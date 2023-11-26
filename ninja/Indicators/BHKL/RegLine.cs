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
	public class RegLine : Indicator
	{
		private double	avg;
		private double	divisor;
		private	double	intercept;
		private double	myPeriod;
		private double	priorSumXY;
		private	double	priorSumY;
		private double	slope;
		private double	sumX2;
		private	double	sumX;
		private double	sumXY;
		private double	sumY;
		private SUM		sum;
		
		private	Series<double>		fastEma;
		private	Series<double>		slowEma;
		private Series<double> 		macd;
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
				Description									= @"Linear Regression Line with seaaonality";
				Name										= "BHKL Reg Line";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				Period						= 14;
				Fast						= 10;
				Slow						= 30;
				Smooth						= 5;
				
				ColorSpring = Brushes.Yellow;
				ColorSummer = Brushes.Green;
				ColorFall = Brushes.Blue;
				ColorWinter = Brushes.Red;
				
				AddPlot(Brushes.Goldenrod, NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameLinReg);
			}
			else if (State == State.Configure)
			{
				avg	= divisor = intercept = myPeriod = priorSumXY
					= priorSumY = slope = sumX = sumX2 = sumY = sumXY = 0;
				
				constant1	= 2.0 / (1 + Fast);
				constant2	= 1 - (2.0 / (1 + Fast));
				constant3	= 2.0 / (1 + Slow);
				constant4	= 1 - (2.0 / (1 + Slow));
				constant5	= 2.0 / (1 + Smooth);
				constant6	= 1 - (2.0 / (1 + Smooth));
			}
			else if (State == State.DataLoaded)
			{
				sum = SUM(Inputs[0], Period);
				fastEma = new Series<double>(this);
				slowEma = new Series<double>(this);
				macd = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				double sumX = (double)Period * (Period - 1) * 0.5;
				double divisor = sumX * sumX - (double)Period * Period * (Period - 1) * (2 * Period - 1) / 6;
				double sumXY = 0;

				for (int count = 0; count < Period && CurrentBar - count >= 0; count++)
					sumXY += count * Input[count];

				double slope = ((double)Period * sumXY - sumX * SUM(Inputs[0], Period)[0]) / divisor;
				double intercept = (SUM(Inputs[0], Period)[0] - slope * sumX) / Period;

				Value[0] = intercept + slope * (Period - 1);
			}
			else
			{
				if (IsFirstTickOfBar)
				{
					priorSumY = sumY;
					priorSumXY = sumXY;
					myPeriod = Math.Min(CurrentBar + 1, Period);
					sumX = myPeriod * (myPeriod - 1) * 0.5;
					sumX2 = myPeriod * (myPeriod + 1) * 0.5;
					divisor = myPeriod * (myPeriod + 1) * (2 * myPeriod + 1) / 6 - sumX2 * sumX2 / myPeriod;
				}

				double input0 = Input[0];
				sumXY = priorSumXY - (CurrentBar >= Period ? priorSumY : 0) + myPeriod * input0;
				sumY = priorSumY + input0 - (CurrentBar >= Period ? Input[Period] : 0);
				avg = sumY / myPeriod;
				slope = (sumXY - sumX2 * avg) / divisor;
				intercept = (sum[0] - slope * sumX) / myPeriod;
				Value[0] = CurrentBar == 0 ? input0 : (intercept + slope * (myPeriod - 1));
			}

			if (CurrentBar == 0)
			{
				fastEma[0]		= Value[0];
				slowEma[0]		= Value[0];
				macd[0]		= 0;
			}
			else 
			{
				double fastEma0	= constant1 * Value[0] + constant2 * fastEma[1];
				double slowEma0	= constant3 * Value[0] + constant4 * slowEma[1];
				double macd_value = fastEma0 - slowEma0;
				fastEma[0]		= fastEma0;
				slowEma[0]		= slowEma0;
				macd[0] = macd_value;
				
				if (macd_value >= 0 && macd[0] >= macd[1]) {
					PlotBrushes[0][0] = ColorSummer;
				} else if (macd_value >=0 && macd[0] < macd[1]) {
					PlotBrushes[0][0] = ColorFall;
				} else if (macd_value < 0 && macd[0] >= macd[1]) {
					PlotBrushes[0][0] = ColorSpring;
				} else if (macd_value <0 && macd[0] < macd[1]) {
					PlotBrushes[0][0] = ColorWinter;
				}
				
//				Print(string.Format("{0:yyyy-MM-dd HH:mm:ss}, {1:0.####}, {2:0.####}, {3:0.####}, {4:0.####}, {5:0.####}, {6:0.####}",this.Time[0],macd_value,Value[0],fastEma0, constant1, constant2, fastEma[1]));
			}
		}
		
		#region Properties
		[Range(2, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period
		{ get; set; }
		
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Fast
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 2)]
		public int Slow
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 3)]
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
		private BHKL.RegLine[] cacheRegLine;
		public BHKL.RegLine RegLine(int period, int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return RegLine(Input, period, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}

		public BHKL.RegLine RegLine(ISeries<double> input, int period, int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			if (cacheRegLine != null)
				for (int idx = 0; idx < cacheRegLine.Length; idx++)
					if (cacheRegLine[idx] != null && cacheRegLine[idx].Period == period && cacheRegLine[idx].Fast == fast && cacheRegLine[idx].Slow == slow && cacheRegLine[idx].Smooth == smooth && cacheRegLine[idx].ColorWinter == colorWinter && cacheRegLine[idx].ColorFall == colorFall && cacheRegLine[idx].ColorSummer == colorSummer && cacheRegLine[idx].ColorSpring == colorSpring && cacheRegLine[idx].EqualsInput(input))
						return cacheRegLine[idx];
			return CacheIndicator<BHKL.RegLine>(new BHKL.RegLine(){ Period = period, Fast = fast, Slow = slow, Smooth = smooth, ColorWinter = colorWinter, ColorFall = colorFall, ColorSummer = colorSummer, ColorSpring = colorSpring }, input, ref cacheRegLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.RegLine RegLine(int period, int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.RegLine(Input, period, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}

		public Indicators.BHKL.RegLine RegLine(ISeries<double> input , int period, int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.RegLine(input, period, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.RegLine RegLine(int period, int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.RegLine(Input, period, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}

		public Indicators.BHKL.RegLine RegLine(ISeries<double> input , int period, int fast, int slow, int smooth, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring)
		{
			return indicator.RegLine(input, period, fast, slow, smooth, colorWinter, colorFall, colorSummer, colorSpring);
		}
	}
}

#endregion
