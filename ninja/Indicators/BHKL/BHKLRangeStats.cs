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
				
				UpColor	= Brushes.DarkRed;
				DownColor = Brushes.DarkGreen;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				var masterName = this.Instrument.FullName.Split(' ')[0];
				rstats = BHKLRoot.GetRedisValue(this);
			}
		}

		protected override void OnBarUpdate()
		{
			var displayText = string.Format("\n RStats = {0:F4}", rstats);
			displayText = displayText + string.Format("\n Stop = {0:F4}", rstats * RStatsRiskRatio);
			Draw.TextFixed(this, "RStatsText", displayText, TextPosition.BottomLeft);
			
			var gc3Amount = rstats * RStatsGC3Ratio;
			
			Draw.Rectangle(this, "upPlot", false, -2, Close[0], -7, Close[0] + gc3Amount, UpColor, UpColor, 50);
			Draw.Rectangle(this, "downPlot", false, -2, Close[0], -7, Close[0] - gc3Amount, DownColor, DownColor, 50);
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
		public BHKL.BHKLRangeStats BHKLRangeStats(double rStatsGC3Ratio, double rStatsRiskRatio, Brush upColor, Brush downColor)
		{
			return BHKLRangeStats(Input, rStatsGC3Ratio, rStatsRiskRatio, upColor, downColor);
		}

		public BHKL.BHKLRangeStats BHKLRangeStats(ISeries<double> input, double rStatsGC3Ratio, double rStatsRiskRatio, Brush upColor, Brush downColor)
		{
			if (cacheBHKLRangeStats != null)
				for (int idx = 0; idx < cacheBHKLRangeStats.Length; idx++)
					if (cacheBHKLRangeStats[idx] != null && cacheBHKLRangeStats[idx].RStatsGC3Ratio == rStatsGC3Ratio && cacheBHKLRangeStats[idx].RStatsRiskRatio == rStatsRiskRatio && cacheBHKLRangeStats[idx].UpColor == upColor && cacheBHKLRangeStats[idx].DownColor == downColor && cacheBHKLRangeStats[idx].EqualsInput(input))
						return cacheBHKLRangeStats[idx];
			return CacheIndicator<BHKL.BHKLRangeStats>(new BHKL.BHKLRangeStats(){ RStatsGC3Ratio = rStatsGC3Ratio, RStatsRiskRatio = rStatsRiskRatio, UpColor = upColor, DownColor = downColor }, input, ref cacheBHKLRangeStats);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(double rStatsGC3Ratio, double rStatsRiskRatio, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(Input, rStatsGC3Ratio, rStatsRiskRatio, upColor, downColor);
		}

		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(ISeries<double> input , double rStatsGC3Ratio, double rStatsRiskRatio, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(input, rStatsGC3Ratio, rStatsRiskRatio, upColor, downColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(double rStatsGC3Ratio, double rStatsRiskRatio, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(Input, rStatsGC3Ratio, rStatsRiskRatio, upColor, downColor);
		}

		public Indicators.BHKL.BHKLRangeStats BHKLRangeStats(ISeries<double> input , double rStatsGC3Ratio, double rStatsRiskRatio, Brush upColor, Brush downColor)
		{
			return indicator.BHKLRangeStats(input, rStatsGC3Ratio, rStatsRiskRatio, upColor, downColor);
		}
	}
}

#endregion
