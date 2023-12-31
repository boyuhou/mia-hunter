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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Core.FloatingPoint;

#endregion

//This namespace holds MarketAnalyzerColumns in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns.BHKL
{
	public class PSARCountAnalyzer : MarketAnalyzerColumn
	{
		private double			af, afPrior;				// Acceleration factor
		private bool			afIncreased, afIncreasedPrior;
		private bool			longPosition, longPositionPrior;
		private int				prevBar, prevBarPrior;
		private double			prevSAR, prevSARPrior;
		private int				reverseBar, reverseBarPrior;
		private double			reverseValue, reverseValuePrior;
		private double			todaySAR, todaySARPrior;		// SAR value
		private double			xp, xpPrior;				// Extreme Price
		
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
		
		private int previousDirection, currentDirection;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "PSAR Count";
				Calculate									= Calculate.OnPriceChange;
				
				Period						= 10;
				Acceleration				= 0.02;
				AccelerationStep			= 0.02;
				AccelerationMax				= 0.14;
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

				rl10 = new Series<double>(this);
				psar = new Series<double>(this);
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
			
			#region PSAR Logic
			if (CurrentBar < 3){
				return;
			}
				

			if (CurrentBar == 3)
			{
				// Determine initial position
				longPosition	= rl10[0] > rl10[1];
				xp				= longPosition ? MAX(rl10, CurrentBar)[0] : MIN(rl10, CurrentBar)[0];
				af				= Acceleration;
				psar[0]		= xp + (longPosition ? -1 : 1) * ((MAX(rl10, CurrentBar)[0] - MIN(rl10, CurrentBar)[0]) * af);
				
				return;
			}
		
			if (IsFirstTickOfBar) {
				afPrior				= af;
				afIncreasedPrior	= afIncreased;
				longPositionPrior	= longPosition;
				prevBarPrior		= prevBar;
				prevSARPrior		= prevSAR;
				reverseBarPrior		= reverseBar;
				reverseValuePrior	= reverseValue;
				todaySARPrior		= todaySAR;
				xpPrior				= xp;
			}
			af				= afPrior;
			afIncreased		= afIncreasedPrior;
			longPosition	= longPositionPrior;
			prevBar			= prevBarPrior;
			prevSAR			= prevSARPrior;
			reverseBar		= reverseBarPrior;
			reverseValue	= reverseValuePrior;
			todaySAR		= todaySARPrior;
			xp				= xpPrior;
		

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
			#endregion
			
			
			// Main Logic
			if (IsFirstTickOfBar) 
			{
				previousDirection = currentDirection;
			}
			
			if (rl10[0] >= psar[0]) 
			{
				if (previousDirection < 0)
					currentDirection = 1;
				else
					currentDirection = previousDirection + 1;
			}
			else 
			{
				if (previousDirection > 0)
					currentDirection = -1;
				else
					currentDirection = previousDirection - 1;
			}
			
			CurrentValue = currentDirection;
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
		
		#endregion
	}
}
