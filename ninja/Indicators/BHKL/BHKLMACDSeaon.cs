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
	public class BHKLMACDSeaon : Indicator
	{
		private	Series<double>		fastEma;
		private	Series<double>		slowEma;
		private double				constant1;
		private double				constant2;
		private double				constant3;
		private double				constant4;
		private double				constant5;
		private double				constant6;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"MACD with season concept";
				Name										= "BHKL MACD Seaon";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive	= true;
				Fast						= 10;
				Slow						= 30;
				Smooth						= 5;
				
				ColorSpring = Brushes.Yellow;
				ColorSummer = Brushes.Green;
				ColorFall = Brushes.Blue;
				ColorWinter = Brushes.Red;
				
				AddPlot(Brushes.DarkCyan,									Custom.Resource.NinjaScriptIndicatorNameMACD);
				AddPlot(Brushes.Transparent,									Custom.Resource.NinjaScriptIndicatorAvg);
				AddPlot(new Stroke(Brushes.DodgerBlue, 2),	PlotStyle.Bar,	Custom.Resource.NinjaScriptIndicatorDiff);
				AddLine(Brushes.DarkGray,					0,				Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.Configure)
			{
				constant1	= 2.0 / (1 + Fast);
				constant2	= 1 - (2.0 / (1 + Fast));
				constant3	= 2.0 / (1 + Slow);
				constant4	= 1 - (2.0 / (1 + Slow));
				constant5	= 2.0 / (1 + Smooth);
				constant6	= 1 - (2.0 / (1 + Smooth));
			}
			else if (State == State.DataLoaded)
			{
				fastEma = new Series<double>(this);
				slowEma = new Series<double>(this);
				
				PrintTo = PrintTo.OutputTab2;
			}
		}

		protected override void OnBarUpdate()
		{
			double input0	= Input[0];

			if (CurrentBar == 0)
			{
				fastEma[0]		= input0;
				slowEma[0]		= input0;
				Value[0]		= 0;
				Avg[0]			= 0;
				Diff[0]			= 0;
			}
			else
			{
				double fastEma0	= constant1 * input0 + constant2 * fastEma[1];
				double slowEma0	= constant3 * input0 + constant4 * slowEma[1];
				double macd		= fastEma0 - slowEma0;
				double macdAvg	= constant5 * macd + constant6 * Avg[1];

				fastEma[0]		= fastEma0;
				slowEma[0]		= slowEma0;
				Value[0]		= macd;
				Avg[0]			= macdAvg;
				Diff[0]			= macd - macdAvg;
				
				if (macd >= 0 && Value[0] >= Value[1]) {
					PlotBrushes[0][0] = ColorSummer;
				} else if (macd >=0 && Value[0] < Value[1]) {
					PlotBrushes[0][0] = ColorFall;
				} else if (macd < 0 && Value[0] >= Value[1]) {
					PlotBrushes[0][0] = ColorSpring;
				} else if (macd <0 && Value[0] < Value[1]) {
					PlotBrushes[0][0] = ColorWinter;
				}
				
//				Print(string.Format("{0:yyyy-MM-dd HH:mm:ss}, {1:0.####}, {2:0.####}, {3:0.####}, {4:0.####}, {5:0.####}, {6:0.####}",this.Time[0],macd,input0,fastEma0, constant1, constant2, fastEma[1]));
			}
		}
		
		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Default
		{
			get { return Values[0]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Avg
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Diff
		{
			get { return Values[2]; }
		}

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Slow
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Smooth
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorWinter", Order=8, GroupName="Plots")]
		public Brush ColorWinter
		{ get; set; }

		[Browsable(false)]
		public string ColorWinterSerializable
		{
			get { return Serialize.BrushToString(ColorWinter); }
			set { ColorWinter = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorFall", Order=7, GroupName="Plots")]
		public Brush ColorFall
		{ get; set; }

		[Browsable(false)]
		public string ColorFallSerializable
		{
			get { return Serialize.BrushToString(ColorFall); }
			set { ColorFall = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorSummer", Order=6, GroupName="Plots")]
		public Brush ColorSummer
		{ get; set; }

		[Browsable(false)]
		public string ColorSummerSerializable
		{
			get { return Serialize.BrushToString(ColorSummer); }
			set { ColorSummer = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorSpring", Order=5, GroupName="Plots")]
		public Brush ColorSpring
		{ get; set; }

		[Browsable(false)]
		public string ColorSpringSerializable
		{
			get { return Serialize.BrushToString(ColorSpring); }
			set { ColorSpring = Serialize.StringToBrush(value); }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BHKL.BHKLMACDSeaon[] cacheBHKLMACDSeaon;
		public BHKL.BHKLMACDSeaon BHKLMACDSeaon(int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return BHKLMACDSeaon(Input, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}

		public BHKL.BHKLMACDSeaon BHKLMACDSeaon(ISeries<double> input, int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			if (cacheBHKLMACDSeaon != null)
				for (int idx = 0; idx < cacheBHKLMACDSeaon.Length; idx++)
					if (cacheBHKLMACDSeaon[idx] != null && cacheBHKLMACDSeaon[idx].Fast == fast && cacheBHKLMACDSeaon[idx].Slow == slow && cacheBHKLMACDSeaon[idx].Smooth == smooth && cacheBHKLMACDSeaon[idx].ColorWinter == colorWinter && cacheBHKLMACDSeaon[idx].ColorFall == colorFall && cacheBHKLMACDSeaon[idx].ColorSummer == colorSummer && cacheBHKLMACDSeaon[idx].ColorSpring == colorSpring && cacheBHKLMACDSeaon[idx].EqualsInput(input))
						return cacheBHKLMACDSeaon[idx];
			return CacheIndicator<BHKL.BHKLMACDSeaon>(new BHKL.BHKLMACDSeaon(){ Fast = fast, Slow = slow, Smooth = smooth, ColorWinter = colorWinter, ColorFall = colorFall, ColorSummer = colorSummer, ColorSpring = colorSpring }, input, ref cacheBHKLMACDSeaon);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.BHKLMACDSeaon BHKLMACDSeaon(int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.BHKLMACDSeaon(Input, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}

		public Indicators.BHKL.BHKLMACDSeaon BHKLMACDSeaon(ISeries<double> input , int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.BHKLMACDSeaon(input, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.BHKLMACDSeaon BHKLMACDSeaon(int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.BHKLMACDSeaon(Input, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}

		public Indicators.BHKL.BHKLMACDSeaon BHKLMACDSeaon(ISeries<double> input , int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.BHKLMACDSeaon(input, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}
	}
}

#endregion
