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
	public class BHKLRegLine : Indicator
	{
		private double			af;				// Acceleration factor
		private bool			afIncreased;
		private bool			longPosition;
		private int				prevBar;
		private double			prevSAR;
		private int				reverseBar;
		private double			reverseValue;
		private double			todaySAR;		// SAR value
		private double			xp;				// Extreme Price

		private Series<double>	afSeries;
		private Series<bool>	afIncreasedSeries;
		private Series<bool>	longPositionSeries;
		private Series<int>		prevBarSeries;
		private Series<double>	prevSARSeries;
		private Series<int>		reverseBarSeries;
		private Series<double>	reverseValueSeries;
		private Series<double>	todaySARSeries;
		private Series<double>	xpSeries;
		
		private double	avg;
		private double	divisor;
		private	double	intercept;
		private double	myPeriod;
		private double	priorSumXY;
		private	double	priorSumY;
		private double	slope, slopePrev, slopePrev2;
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
		private int seasonInt;  // 0 = spring, 1 = summer, 2 = fall, 3 = winter
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Linear Regression Line with seaaonality";
				Name										= "BHKL Reg Line";
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
				
				Period						= 10;
				Fast						= 10;
				Slow						= 30;
				Smooth						= 5;
				ShowBar = false;
				
				Acceleration				= 0.02;
				AccelerationStep			= 0.02;
				AccelerationMax				= 0.14;
				
				ColorSpring = Brushes.Yellow;
				ColorSummer = Brushes.Green;
				ColorFall = Brushes.Blue;
				ColorWinter = Brushes.Red;
				
				ColorBull = Brushes.Green;
				ColorBear = Brushes.Red;
				Opacity = 50;
				
				AddPlot(Brushes.Goldenrod, "rl10");
				AddPlot(new Stroke(Brushes.Black, 2), PlotStyle.Dot, "psar");
				AddPlot(Brushes.Transparent, "sbar");
			}
			else if (State == State.Configure)
			{
				avg	= divisor = intercept = myPeriod = priorSumXY
					= priorSumY = slope = sumX = sumX2 = sumY = sumXY = slopePrev = slopePrev2 = 0;
				
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
				
				if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
				{
					afSeries			= new Series<double>(this);
					afIncreasedSeries	= new Series<bool>(this);
					longPositionSeries	= new Series<bool>(this);
					prevBarSeries		= new Series<int>(this);
					prevSARSeries		= new Series<double>(this);
					reverseBarSeries	= new Series<int>(this);
					reverseValueSeries	= new Series<double>(this);
					todaySARSeries		= new Series<double>(this);
					xpSeries			= new Series<double>(this);
				}
				
				fastEma = new Series<double>(this);
				slowEma = new Series<double>(this);
				macd = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (IsFirstTickOfBar) 
			{
				slopePrev2 = slopePrev;
				slopePrev = slope;   
			}
			
			#region RL Logic
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				double sumX = (double)Period * (Period - 1) * 0.5;
				double divisor = sumX * sumX - (double)Period * Period * (Period - 1) * (2 * Period - 1) / 6;
				double sumXY = 0;

				for (int count = 0; count < Period && CurrentBar - count >= 0; count++)
					sumXY += count * Input[count];

				double slope = ((double)Period * sumXY - sumX * SUM(Inputs[0], Period)[0]) / divisor;
				double intercept = (SUM(Inputs[0], Period)[0] - slope * sumX) / Period;

				rl10[0] = intercept + slope * (Period - 1);
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
				rl10[0] = CurrentBar == 0 ? input0 : (intercept + slope * (myPeriod - 1));
			}
			#endregion
			
			#region PSAR Logic
			if (CurrentBar < 3)
				return;

			if (CurrentBar == 3)
			{
				// Determine initial position
				longPosition	= rl10[0] > rl10[1];
				xp				= longPosition ? MAX(rl10, CurrentBar)[0] : MIN(rl10, CurrentBar)[0];
				af				= Acceleration;
				psar[0]		= xp + (longPosition ? -1 : 1) * ((MAX(rl10, CurrentBar)[0] - MIN(rl10, CurrentBar)[0]) * af);
				return;
			}
			if (BarsArray[0].BarsType.IsRemoveLastBarSupported && CurrentBar < prevBar)
			{
				af				= afSeries[0];
				afIncreased		= afIncreasedSeries[0];
				longPosition	= longPositionSeries[0];
				prevBar			= prevBarSeries[0];
				prevSAR			= prevSARSeries[0];
				reverseBar		= reverseBarSeries[0];
				reverseValue	= reverseValueSeries[0];
				todaySAR		= todaySARSeries[0];
				xp				= xpSeries[0];
			}

			// Reset accelerator increase limiter on new bars
			if (afIncreased && prevBar != CurrentBar)
				afIncreased = false;

			// Current event is on a bar not marked as a reversal bar yet
			if (reverseBar != CurrentBar)
			{
				// SAR = SAR[1] + af * (xp - SAR[1])
				todaySAR = TodaySAR(psar[1] + af * (xp - psar[1]));
				for (int x = 1; x <= 2; x++)
				{
					if (longPosition)
					{
						if (todaySAR > rl10[x])
							todaySAR = rl10[x];
					}
					else
					{
						if (todaySAR < rl10[x])
							todaySAR = rl10[x];
					}
				}

				// Holding long position
				if (longPosition)
				{
					// Process a new SAR value only on a new bar or if SAR value was penetrated.
					if (prevBar != CurrentBar || rl10[0] < prevSAR)
					{
						psar[0] = todaySAR;
						prevSAR = todaySAR;
					}
					else
						psar[0] = prevSAR;

					if (rl10[0] > xp)
					{
						xp = rl10[0];
						AfIncrease();
					}
				}

				// Holding short position
				else if (!longPosition)
				{
					// Process a new SAR value only on a new bar or if SAR value was penetrated.
					if (prevBar != CurrentBar || rl10[0] > prevSAR)
					{
						psar[0] = todaySAR;
						prevSAR = todaySAR;
					}
					else
						psar[0] = prevSAR;

					if (rl10[0] < xp)
					{
						xp = rl10[0];
						AfIncrease();
					}
				}
			}

			// Current event is on the same bar as the reversal bar
			else
			{
				// Only set new xp values. No increasing af since this is the first bar.
				if (longPosition && rl10[0] > xp)
					xp = rl10[0];
				else if (!longPosition && rl10[0] < xp)
					xp = rl10[0];

				psar[0] = prevSAR;

				// SAR = SAR[1] + af * (xp - SAR[1])
				todaySAR = TodaySAR(longPosition ? Math.Min(reverseValue, rl10[0]) : Math.Max(reverseValue, rl10[0]));
			}

			prevBar = CurrentBar;

			// Reverse position
			if ((longPosition && (rl10[0] < todaySAR || rl10[1] < todaySAR))
				|| (!longPosition && (rl10[0] > todaySAR || rl10[1] > todaySAR)))
				psar[0] = Reverse();

			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				afSeries[0]				= af;
				afIncreasedSeries[0]	= afIncreased;
				longPositionSeries[0]	= longPosition;
				prevBarSeries[0]		= prevBar;
				prevSARSeries[0]		= prevSAR;
				reverseBarSeries[0]		= reverseBar;
				reverseValueSeries[0]	= reverseValue;
				todaySARSeries[0]		= todaySAR;
				xpSeries[0]				= xp;
			}
			#endregion

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
					seasonInt = 1;
				} else if (macd_value >=0 && macd[0] < macd[1]) {
					PlotBrushes[0][0] = ColorFall;
					seasonInt = 2;
				} else if (macd_value < 0 && macd[0] >= macd[1]) {
					PlotBrushes[0][0] = ColorSpring;
					seasonInt = 0;
				} else if (macd_value <0 && macd[0] < macd[1]) {
					PlotBrushes[0][0] = ColorWinter;
					seasonInt = 3;
				}
				
//				Print(string.Format("{0:yyyy-MM-dd HH:mm:ss}, {1:0.####}, {2:0.####}, {3:0.####}, {4:0.####}, {5:0.####}, {6:0.####}",this.Time[0],macd_value,Value[0],fastEma0, constant1, constant2, fastEma[1]));
			}
			
			if (ShowBar && CurrentBar > 3)
			{
				sbar[0] = 0;
				if (seasonInt != 3 && Close[0] > Value[0] && Close[1] > Value[1] && slope > slopePrev && slopePrev > slopePrev2 && psar[0] < rl10[0])
				{
					Draw.Rectangle(this, String.Format("BullUp{0}", CurrentBar), false, Time[1], Low[0], Time[0], High[0], Brushes.Transparent, ColorBull, Opacity, true);
					sbar[0] = 1;
				}
				
				if (seasonInt != 1 && Close[0] < Value[0] && Close[1] < Value[1] && slope < slopePrev && slopePrev < slopePrev2 && psar[0] > rl10[0])
				{
					Draw.Rectangle(this, String.Format("BearDown{0}", CurrentBar), false, Time[1], Low[0], Time[0], High[0], Brushes.Transparent, ColorBear, Opacity, true);
					sbar[0] = -1;
				}
			}
		}
		
		#region Miscellaneous
		// Only raise accelerator if not raised for current bar yet
		private void AfIncrease()
		{
			if (!afIncreased)
			{
				af			= Math.Min(AccelerationMax, af + AccelerationStep);
				afIncreased	= true;
			}
		}

		// Additional rule. SAR for today can't be placed inside the bar of day - 1 or day - 2.
		private double TodaySAR(double tSAR)
		{
			if (longPosition)
			{
				double lowestSAR = Math.Min(Math.Min(tSAR, rl10[0]), rl10[1]);
				if (rl10[0] > lowestSAR)
					tSAR = lowestSAR;
			}
			else
			{
				double highestSAR = Math.Max(Math.Max(tSAR, rl10[0]), rl10[1]);
				if (rl10[0] < highestSAR)
					tSAR = highestSAR;
			}
			return tSAR;
		}

		private double Reverse()
		{
			double tSAR = xp;

			if ((longPosition && prevSAR > rl10[0]) || (!longPosition && prevSAR < rl10[0]) || prevBar != CurrentBar)
			{
				longPosition	= !longPosition;
				reverseBar		= CurrentBar;
				reverseValue	= xp;
				af				= Acceleration;
				xp				= longPosition ? rl10[0] : rl10[0];
				prevSAR			= tSAR;
			}
			else
				tSAR = prevSAR;
			return tSAR;
		}
		#endregion
		
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
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowBar", GroupName = "NinjaScriptParameters", Order = 4)]
		public bool ShowBar
		{ get; set; }
		
		[Range(0.00, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Acceleration", GroupName = "NinjaScriptParameters", Order = 5)]
		public double Acceleration
		{ get; set; }

		[Range(0.001, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AccelerationMax", GroupName = "NinjaScriptParameters", Order = 6)]
		public double AccelerationMax
		{ get; set; }

		[Range(0.001, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AccelerationStep", GroupName = "NinjaScriptParameters", Order = 7)]
		public double AccelerationStep
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorWinter", Order=4, GroupName="Plots")]
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
		[Display(Name="ColorFall", Order=3, GroupName="Plots")]
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
		[Display(Name="ColorSummer", Order=2, GroupName="Plots")]
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
		[Display(Name="ColorSpring", Order=1, GroupName="Plots")]
		public Brush ColorSpring
		{ get; set; }

		[Browsable(false)]
		public string ColorSpringSerializable
		{
			get { return Serialize.BrushToString(ColorSpring); }
			set { ColorSpring = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorBull", Order=6, GroupName="Plots")]
		public Brush ColorBull
		{ get; set; }

		[Browsable(false)]
		public string ColorBullSerializable
		{
			get { return Serialize.BrushToString(ColorBull); }
			set { ColorBull = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorBear", Order=7, GroupName="Plots")]
		public Brush ColorBear
		{ get; set; }

		[Browsable(false)]
		public string ColorBearSerializable
		{
			get { return Serialize.BrushToString(ColorBear); }
			set { ColorBear = Serialize.StringToBrush(value); }
		}
		
		[Range(0, 100), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Opacity", GroupName = "Plots", Order = 8)]
		public int Opacity
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> rl10
		{
			get { return Values[0]; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> psar
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> sbar
		{
			get { return Values[2]; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BHKL.BHKLRegLine[] cacheBHKLRegLine;
		public BHKL.BHKLRegLine BHKLRegLine(int period, int fast, int slow, int smooth, double acceleration, double accelerationMax, double accelerationStep, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring, Brush colorBull, Brush colorBear, int opacity)
		{
			return BHKLRegLine(Input, period, fast, slow, smooth, acceleration, accelerationMax, accelerationStep, colorWinter, colorFall, colorSummer, colorSpring, colorBull, colorBear, opacity);
		}

		public BHKL.BHKLRegLine BHKLRegLine(ISeries<double> input, int period, int fast, int slow, int smooth, double acceleration, double accelerationMax, double accelerationStep, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring, Brush colorBull, Brush colorBear, int opacity)
		{
			if (cacheBHKLRegLine != null)
				for (int idx = 0; idx < cacheBHKLRegLine.Length; idx++)
					if (cacheBHKLRegLine[idx] != null && cacheBHKLRegLine[idx].Period == period && cacheBHKLRegLine[idx].Fast == fast && cacheBHKLRegLine[idx].Slow == slow && cacheBHKLRegLine[idx].Smooth == smooth && cacheBHKLRegLine[idx].Acceleration == acceleration && cacheBHKLRegLine[idx].AccelerationMax == accelerationMax && cacheBHKLRegLine[idx].AccelerationStep == accelerationStep && cacheBHKLRegLine[idx].ColorWinter == colorWinter && cacheBHKLRegLine[idx].ColorFall == colorFall && cacheBHKLRegLine[idx].ColorSummer == colorSummer && cacheBHKLRegLine[idx].ColorSpring == colorSpring && cacheBHKLRegLine[idx].ColorBull == colorBull && cacheBHKLRegLine[idx].ColorBear == colorBear && cacheBHKLRegLine[idx].Opacity == opacity && cacheBHKLRegLine[idx].EqualsInput(input))
						return cacheBHKLRegLine[idx];
			return CacheIndicator<BHKL.BHKLRegLine>(new BHKL.BHKLRegLine(){ Period = period, Fast = fast, Slow = slow, Smooth = smooth, Acceleration = acceleration, AccelerationMax = accelerationMax, AccelerationStep = accelerationStep, ColorWinter = colorWinter, ColorFall = colorFall, ColorSummer = colorSummer, ColorSpring = colorSpring, ColorBull = colorBull, ColorBear = colorBear, Opacity = opacity }, input, ref cacheBHKLRegLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.BHKLRegLine BHKLRegLine(int period, int fast, int slow, int smooth, double acceleration, double accelerationMax, double accelerationStep, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring, Brush colorBull, Brush colorBear, int opacity)
		{
			return indicator.BHKLRegLine(Input, period, fast, slow, smooth, acceleration, accelerationMax, accelerationStep, colorWinter, colorFall, colorSummer, colorSpring, colorBull, colorBear, opacity);
		}

		public Indicators.BHKL.BHKLRegLine BHKLRegLine(ISeries<double> input , int period, int fast, int slow, int smooth, double acceleration, double accelerationMax, double accelerationStep, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring, Brush colorBull, Brush colorBear, int opacity)
		{
			return indicator.BHKLRegLine(input, period, fast, slow, smooth, acceleration, accelerationMax, accelerationStep, colorWinter, colorFall, colorSummer, colorSpring, colorBull, colorBear, opacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.BHKLRegLine BHKLRegLine(int period, int fast, int slow, int smooth, double acceleration, double accelerationMax, double accelerationStep, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring, Brush colorBull, Brush colorBear, int opacity)
		{
			return indicator.BHKLRegLine(Input, period, fast, slow, smooth, acceleration, accelerationMax, accelerationStep, colorWinter, colorFall, colorSummer, colorSpring, colorBull, colorBear, opacity);
		}

		public Indicators.BHKL.BHKLRegLine BHKLRegLine(ISeries<double> input , int period, int fast, int slow, int smooth, double acceleration, double accelerationMax, double accelerationStep, Brush colorWinter, Brush colorFall, Brush colorSummer, Brush colorSpring, Brush colorBull, Brush colorBear, int opacity)
		{
			return indicator.BHKLRegLine(input, period, fast, slow, smooth, acceleration, accelerationMax, accelerationStep, colorWinter, colorFall, colorSummer, colorSpring, colorBull, colorBear, opacity);
		}
	}
}

#endregion
