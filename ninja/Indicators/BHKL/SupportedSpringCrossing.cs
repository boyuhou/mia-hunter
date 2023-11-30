#region Using declarations
using System;
using System.Globalization;
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
using NinjaTrader.NinjaScript.Indicators.Sharpe;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.BHKL
{
	public class SupportedSpringCrossing : Indicator
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
		private double	slope;
		private double	sumX2;
		private	double	sumX;
		private double	sumXY;
		private double	sumY;
		private SUM		sum;
		
		private Series<double>	rl10;
		private Series<double>	psar;
		private Series<double>	upper;
		private Series<double>	lower;
		
		private SMA		sma;
		private StdDev	stdDev;
		
		private double minRangeValue, maxRangeValue;
		
		private int previousDirection, currentDirection;
		
		private double rstats;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Identify Possible SSC condition";
				Name										= "BHKL Supported Spring Crossing";
				Calculate									= Calculate.OnPriceChange;
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
				
				Acceleration				= 0.02;
				AccelerationStep			= 0.02;
				AccelerationMax				= 0.2;
				
				Period						= 10;
				
				BBPeriod = 10;
				BBNumStdDev					= 0.5;
				
				RStatsGC3Ratio = 0.2;
				
				AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Dot, "SignalDot");
			}
			else if (State == State.Configure)
			{
				
				avg	= divisor = intercept = myPeriod = priorSumXY
					= priorSumY = slope = sumX = sumX2 = sumY = sumXY = 0;
				
				xp				= 0.0;
				af				= 0;
				todaySAR		= 0;
				prevSAR			= 0;
				reverseBar		= 0;
				reverseValue	= 0;
				prevBar			= 0;
				afIncreased		= false;
				
				previousDirection = 0;
				currentDirection = 0;
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

				rl10 = new Series<double>(this);
				psar = new Series<double>(this);
				upper = new Series<double>(this);
				lower = new Series<double>(this);
				
				sma		= SMA(BBPeriod);
				stdDev	= StdDev(BBPeriod);
				
				rstats = BHKLRoot.GetRedisValue(this);
			}
		}

		protected override void OnBarUpdate()
		{
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
			
			#region Bollinger Logic
			double sma0		= sma[0];
			double stdDev0	= stdDev[0];

			upper[0]		= sma0 + BBNumStdDev * stdDev0;
			lower[0]		= sma0 - BBNumStdDev * stdDev0;
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
			
			//Main Logic
			// current direction == 1
			//
			
			if (IsFirstTickOfBar) 
			{
				previousDirection = currentDirection;
			}
			
			if (rl10[0] >= psar[0]) {
				currentDirection = 1;
			}
			else {
				currentDirection = -1;
			}
			
			if (currentDirection == previousDirection) {
				minRangeValue = Math.Min(minRangeValue, rl10[0]);
				maxRangeValue = Math.Max(maxRangeValue, rl10[0]);
				
				// SSC to the down side
				if (rl10[0] < rl10[1] 
					&& rl10[0] < upper[0] 
					&& currentDirection == 1
					&& (maxRangeValue - minRangeValue) >= RStatsGC3Ratio * rstats
				)  
				{
					SignalDot[0] = 0.75;
				}
				if (rl10[0] > rl10[1] 
					&& rl10[0] > lower[0] 
					&& currentDirection == -1
					&& (maxRangeValue - minRangeValue) >= RStatsGC3Ratio * rstats
				)  
				{
					SignalDot[0] = 0.25;
				}
			}
			
			// PSAR flipped
			if (currentDirection != previousDirection) {
				minRangeValue = psar[0];
				maxRangeValue = psar[0];
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
		
		[Range(0.00, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Acceleration", GroupName = "NinjaScriptParameters", Order = 1)]
		public double Acceleration
		{ get; set; }

		[Range(0.001, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AccelerationMax", GroupName = "NinjaScriptParameters", Order = 2)]
		public double AccelerationMax
		{ get; set; }

		[Range(0.001, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AccelerationStep", GroupName = "NinjaScriptParameters", Order = 3)]
		public double AccelerationStep
		{ get; set; }
		
		
		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BollingerNumStdDev", GroupName = "NinjaScriptParameters", Order = 4)]
		public double BBNumStdDev
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BollingerPeriod", GroupName = "NinjaScriptParameters", Order = 5)]
		public int BBPeriod
		{ get; set; }
		
		[Range(0, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "RStatsGC3Ratio", GroupName = "NinjaScriptParameters", Order = 6)]
		public double RStatsGC3Ratio
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> SignalDot
		{
			get { return Values[0]; }
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BHKL.SupportedSpringCrossing[] cacheSupportedSpringCrossing;
		public BHKL.SupportedSpringCrossing SupportedSpringCrossing(int period, double acceleration, double accelerationMax, double accelerationStep, double bBNumStdDev, int bBPeriod, double rStatsGC3Ratio)
		{
			return SupportedSpringCrossing(Input, period, acceleration, accelerationMax, accelerationStep, bBNumStdDev, bBPeriod, rStatsGC3Ratio);
		}

		public BHKL.SupportedSpringCrossing SupportedSpringCrossing(ISeries<double> input, int period, double acceleration, double accelerationMax, double accelerationStep, double bBNumStdDev, int bBPeriod, double rStatsGC3Ratio)
		{
			if (cacheSupportedSpringCrossing != null)
				for (int idx = 0; idx < cacheSupportedSpringCrossing.Length; idx++)
					if (cacheSupportedSpringCrossing[idx] != null && cacheSupportedSpringCrossing[idx].Period == period && cacheSupportedSpringCrossing[idx].Acceleration == acceleration && cacheSupportedSpringCrossing[idx].AccelerationMax == accelerationMax && cacheSupportedSpringCrossing[idx].AccelerationStep == accelerationStep && cacheSupportedSpringCrossing[idx].BBNumStdDev == bBNumStdDev && cacheSupportedSpringCrossing[idx].BBPeriod == bBPeriod && cacheSupportedSpringCrossing[idx].RStatsGC3Ratio == rStatsGC3Ratio && cacheSupportedSpringCrossing[idx].EqualsInput(input))
						return cacheSupportedSpringCrossing[idx];
			return CacheIndicator<BHKL.SupportedSpringCrossing>(new BHKL.SupportedSpringCrossing(){ Period = period, Acceleration = acceleration, AccelerationMax = accelerationMax, AccelerationStep = accelerationStep, BBNumStdDev = bBNumStdDev, BBPeriod = bBPeriod, RStatsGC3Ratio = rStatsGC3Ratio }, input, ref cacheSupportedSpringCrossing);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.SupportedSpringCrossing SupportedSpringCrossing(int period, double acceleration, double accelerationMax, double accelerationStep, double bBNumStdDev, int bBPeriod, double rStatsGC3Ratio)
		{
			return indicator.SupportedSpringCrossing(Input, period, acceleration, accelerationMax, accelerationStep, bBNumStdDev, bBPeriod, rStatsGC3Ratio);
		}

		public Indicators.BHKL.SupportedSpringCrossing SupportedSpringCrossing(ISeries<double> input , int period, double acceleration, double accelerationMax, double accelerationStep, double bBNumStdDev, int bBPeriod, double rStatsGC3Ratio)
		{
			return indicator.SupportedSpringCrossing(input, period, acceleration, accelerationMax, accelerationStep, bBNumStdDev, bBPeriod, rStatsGC3Ratio);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.SupportedSpringCrossing SupportedSpringCrossing(int period, double acceleration, double accelerationMax, double accelerationStep, double bBNumStdDev, int bBPeriod, double rStatsGC3Ratio)
		{
			return indicator.SupportedSpringCrossing(Input, period, acceleration, accelerationMax, accelerationStep, bBNumStdDev, bBPeriod, rStatsGC3Ratio);
		}

		public Indicators.BHKL.SupportedSpringCrossing SupportedSpringCrossing(ISeries<double> input , int period, double acceleration, double accelerationMax, double accelerationStep, double bBNumStdDev, int bBPeriod, double rStatsGC3Ratio)
		{
			return indicator.SupportedSpringCrossing(input, period, acceleration, accelerationMax, accelerationStep, bBNumStdDev, bBPeriod, rStatsGC3Ratio);
		}
	}
}

#endregion
