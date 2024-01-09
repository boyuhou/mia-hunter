#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.BHKL
{
	public class BHKLRangeStats : Indicator
	{
		private double rstats;
		private bool IsCurrencySpot, IsReversion;
		private double exchangeRate;
		private double minimumAmount;
		private string[] REVERSION_CURRENCIES = { "GBP", "AUD", "NZD" };
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "BHKL Range Stats";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				RStatsGC3Ratio = 0.3;
				RStatsRiskRatio = 0.1;
				
				PerTradeRiskAmount = 500;
				
				UpColor	= Brushes.DarkRed;
				DownColor = Brushes.DarkGreen;
			}
			else if (State == State.Configure)
			{
				IsCurrencySpot = !(this.Instrument.MasterInstrument.Currency.ToString() == "UsDollar");
				IsReversion = getIsReversion();
				if (IsCurrencySpot)
				{
					var conversionCurrency = "USD" + this.Instrument.FullName.Substring(3, 3);
					if (IsReversion)
						conversionCurrency = conversionCurrency.Substring(3, 3) + conversionCurrency.Substring(0, 3);
					AddDataSeries(conversionCurrency, Data.BarsPeriodType.Minute, 60, Data.MarketDataType.Last);
				}
			}
			else if (State == State.DataLoaded)
			{
				var masterName = this.Instrument.FullName.Split(' ')[0];
				rstats = BHKLRoot.GetRedisValue(this);
				
				exchangeRate = 1.0;
				
				if (this.Instrument.MasterInstrument.InstrumentType.ToString() == "Forex")
				{
					minimumAmount = 1;
				}
				else if (this.Instrument.MasterInstrument.InstrumentType.ToString() == "Future")
				{
					minimumAmount = getFutureMinimumAmount();
				}
				else 
				{
					minimumAmount = TickSize * 100;
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (IsCurrencySpot && BarsInProgress == 1)
			{
				if (CurrentBar > 0)
					exchangeRate = IsReversion ? (1 / this.Closes[1][0]) : this.Closes[1][0];
			}
			
			if (BarsInProgress == 0) 
			{
				var lotUnits = PerTradeRiskAmount / minimumAmount / (rstats * RStatsRiskRatio) * exchangeRate;
				int roundedLotUnits = (int) (lotUnits / 100 * 100) ;
				var masterName = this.Instrument.FullName.Split(' ')[0];
				
				var displayText = string.Format("\n RStats = {0:F4}", rstats);
				displayText = displayText + string.Format("\n Stop = {0:F4}\n Position Size = {1:n4}", rstats * RStatsRiskRatio, lotUnits);
				Draw.TextFixed(this, "RStatsText", displayText, TextPosition.BottomLeft);
				
				var gc3Amount = rstats * RStatsGC3Ratio;
				
				Draw.Rectangle(this, "upPlot", false, -2, Close[0], -7, Close[0] + gc3Amount, UpColor, UpColor, 50);
				Draw.Rectangle(this, "downPlot", false, -2, Close[0], -7, Close[0] - gc3Amount, DownColor, DownColor, 50);	
			}
			
			
		}
		
		
		private double getFutureMinimumAmount()
		{
			var dictMapping = new Dictionary<string, double>();
			
			// index
			dictMapping.Add("ES", 12.50);
			dictMapping.Add("MES", 1.25);
			dictMapping.Add("NQ", 5);
			dictMapping.Add("MNQ", 0.5);
			dictMapping.Add("YM", 5);
			dictMapping.Add("MYM", 0.5);
			dictMapping.Add("RTY", 5);
			dictMapping.Add("M2K", 0.5);
			
			// energy
			dictMapping.Add("CL", 10);
			dictMapping.Add("MCL", 1);
			dictMapping.Add("QM", 12.50);
			dictMapping.Add("HO", 4.2);
			dictMapping.Add("NG", 10);
			dictMapping.Add("B", 10);
			dictMapping.Add("RB", 4.2);
			dictMapping.Add("G", 25.00);
			
			// metal
			dictMapping.Add("HG", 12.50);
			dictMapping.Add("GC", 10);
			dictMapping.Add("MGC", 1);
			dictMapping.Add("PA", 10);
			dictMapping.Add("PL", 5);
			dictMapping.Add("SI", 25);
			
			// currency
			dictMapping.Add("6A", 20);   // tick size of 6A by system is 0.0001 but it should be 0.00005 so we increase the value here
			dictMapping.Add("6B", 6.25);
			dictMapping.Add("6C", 5);
			dictMapping.Add("DX", 5);
			dictMapping.Add("6E", 6.25);
			dictMapping.Add("6J", 6.25);
			dictMapping.Add("6M", 5);
			dictMapping.Add("6N", 20); // tick size of 6N by system is 0.0001 but itshould be 0.00005
			dictMapping.Add("6S", 12.5);
			dictMapping.Add("BTC", 25);
			dictMapping.Add("MBT", 2.5);
			dictMapping.Add("ETH", 25);
			dictMapping.Add("MET", 2.5);
			
			// soft
			dictMapping.Add("CC", 10);
			dictMapping.Add("KC", 18.75);
			dictMapping.Add("CT", 5);
			dictMapping.Add("SB", 11.2);
			
			// bonds
			dictMapping.Add("ZT", 7.8125);
			dictMapping.Add("ZF", 7.8125);
			dictMapping.Add("ZN", 15.625);
			dictMapping.Add("ZB", 31.25);
			dictMapping.Add("UB", 31.25);
			
			// grain
			dictMapping.Add("ZL", 6);
			dictMapping.Add("ZM", 10);
			dictMapping.Add("ZS", 12.50);
			dictMapping.Add("ZW", 12.50);
			dictMapping.Add("ZC", 12.50);
			
			// meat
			dictMapping.Add("LE", 10);
			dictMapping.Add("GF", 12.50);
			dictMapping.Add("HE", 10);
			
			var masterName = this.Instrument.FullName.Split(' ')[0];
			return dictMapping[masterName] / TickSize;
		}
		
		private bool getIsReversion()
		{
			return IsCurrencySpot && REVERSION_CURRENCIES.Contains(this.Instrument.FullName.Substring(3, 3));
		}
		
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name="RStatsGC3Ratio", Order=1, GroupName="Parameters")]
		public double RStatsGC3Ratio
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="RStatsRiskRatio", Order=2, GroupName="Parameters")]
		public double RStatsRiskRatio
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="PerTradeRiskAmount", Order=3, GroupName="Parameters")]
		public double PerTradeRiskAmount
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="UpColor", Order=8, GroupName="Plots")]
		public Brush UpColor
		{ get; set; }
		
		[Browsable(false)]
		public string UpColorSerializable
		{
			get { return Serialize.BrushToString(UpColor); }
			set { UpColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="DownColor", Order=9, GroupName="Plots")]
		public Brush DownColor
		{ get; set; }
		
		[Browsable(false)]
		public string DownColorSerializable
		{
			get { return Serialize.BrushToString(DownColor); }
			set { DownColor = Serialize.StringToBrush(value); }
		}
		#endregion
	}
	
	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BHKL.BHKLRangeStats[] cacheBHKLRangeStats;
		public BHKL.BHKLRangeStats BHKLRangeStats(double rStatsGC3Ratio, double rStatsRiskRatio, double perTradeRiskAmount, Brush upColor, Brush downColor)
		{
			return BHKLRangeStats(Input, rStatsGC3Ratio, rStatsRiskRatio, perTradeRiskAmount, upColor, downColor);
		}

		public BHKL.BHKLRangeStats BHKLRangeStats(ISeries<double> input, double rStatsGC3Ratio, double rStatsRiskRatio, double perTradeRiskAmount, Brush upColor, Brush downColor)
		{
			if (cacheBHKLRangeStats != null)
				for (int idx = 0; idx < cacheBHKLRangeStats.Length; idx++)
					if (cacheBHKLRangeStats[idx] != null && cacheBHKLRangeStats[idx].RStatsGC3Ratio == rStatsGC3Ratio && cacheBHKLRangeStats[idx].RStatsRiskRatio == rStatsRiskRatio && cacheBHKLRangeStats[idx].PerTradeRiskAmount == perTradeRiskAmount && cacheBHKLRangeStats[idx].UpColor == upColor && cacheBHKLRangeStats[idx].DownColor == downColor && cacheBHKLRangeStats[idx].EqualsInput(input))
						return cacheBHKLRangeStats[idx];
			return CacheIndicator<BHKL.BHKLRangeStats>(new BHKL.BHKLRangeStats(){ RStatsGC3Ratio = rStatsGC3Ratio, RStatsRiskRatio = rStatsRiskRatio, PerTradeRiskAmount = perTradeRiskAmount, UpColor = upColor, DownColor = downColor }, input, ref cacheBHKLRangeStats);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(double rStatsGC3Ratio, double rStatsRiskRatio, double perTradeRiskAmount, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(Input, rStatsGC3Ratio, rStatsRiskRatio, perTradeRiskAmount, upColor, downColor);
		}

		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(ISeries<double> input , double rStatsGC3Ratio, double rStatsRiskRatio, double perTradeRiskAmount, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(input, rStatsGC3Ratio, rStatsRiskRatio, perTradeRiskAmount, upColor, downColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(double rStatsGC3Ratio, double rStatsRiskRatio, double perTradeRiskAmount, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(Input, rStatsGC3Ratio, rStatsRiskRatio, perTradeRiskAmount, upColor, downColor);
		}

		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(ISeries<double> input , double rStatsGC3Ratio, double rStatsRiskRatio, double perTradeRiskAmount, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(input, rStatsGC3Ratio, rStatsRiskRatio, perTradeRiskAmount, upColor, downColor);
		}
	}
}

#endregion
