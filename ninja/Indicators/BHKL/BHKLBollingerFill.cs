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
	public class BHKLBollingerFill : Indicator
	{
		private SMA		sma;
		private StdDev	stdDev;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "BHKL Bollinger Fill";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				NumStdDev					= 1;
				Period						= 30;
				
				OpacityFill = 60;
				ColorFill = Brushes.Red;
				
				AddPlot(Brushes.Goldenrod, "Upper");
				AddPlot(Brushes.Goldenrod, "Middle");
				AddPlot(Brushes.Goldenrod, "Lower");
				
			}
			else if (State == State.Configure)
			{
				sma		= SMA(Period);
				stdDev	= StdDev(Period);
			}
		}

		protected override void OnBarUpdate()
		{
			double sma0		= sma[0];
			double stdDev0	= stdDev[0];

			Upper[0]		= sma0 + NumStdDev * stdDev0;
			Middle[0]		= sma0;
			Lower[0]		= sma0 - NumStdDev * stdDev0;
			
			Draw.Region(this, "bbFill", this.CurrentBar, 0, Upper, Lower, null, ColorFill, OpacityFill);
		}

		#region Properties
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Lower
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Middle
		{
			get { return Values[1]; }
		}

		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NumStdDev", GroupName = "NinjaScriptParameters", Order = 0)]
		public double NumStdDev
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
		public int Period
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Upper
		{
			get { return Values[0]; }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="ColorFill", Order=5, GroupName="Plots")]
		public Brush ColorFill
		{ get; set; }

		[Browsable(false)]
		public string ColorFillSerializable
		{
			get { return Serialize.BrushToString(ColorFill); }
			set { ColorFill = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name="OpacityFill", Order=4, GroupName="Plots")]
		public int OpacityFill
		{ get; set; }
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BHKL.BHKLBollingerFill[] cacheBHKLBollingerFill;
		public BHKL.BHKLBollingerFill BHKLBollingerFill(double numStdDev, int period, Brush colorFill, int opacityFill)
		{
			return BHKLBollingerFill(Input, numStdDev, period, colorFill, opacityFill);
		}

		public BHKL.BHKLBollingerFill BHKLBollingerFill(ISeries<double> input, double numStdDev, int period, Brush colorFill, int opacityFill)
		{
			if (cacheBHKLBollingerFill != null)
				for (int idx = 0; idx < cacheBHKLBollingerFill.Length; idx++)
					if (cacheBHKLBollingerFill[idx] != null && cacheBHKLBollingerFill[idx].NumStdDev == numStdDev && cacheBHKLBollingerFill[idx].Period == period && cacheBHKLBollingerFill[idx].ColorFill == colorFill && cacheBHKLBollingerFill[idx].OpacityFill == opacityFill && cacheBHKLBollingerFill[idx].EqualsInput(input))
						return cacheBHKLBollingerFill[idx];
			return CacheIndicator<BHKL.BHKLBollingerFill>(new BHKL.BHKLBollingerFill(){ NumStdDev = numStdDev, Period = period, ColorFill = colorFill, OpacityFill = opacityFill }, input, ref cacheBHKLBollingerFill);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.BHKLBollingerFill BHKLBollingerFill(double numStdDev, int period, Brush colorFill, int opacityFill)
		{
			return indicator.BHKLBollingerFill(Input, numStdDev, period, colorFill, opacityFill);
		}

		public Indicators.BHKL.BHKLBollingerFill BHKLBollingerFill(ISeries<double> input , double numStdDev, int period, Brush colorFill, int opacityFill)
		{
			return indicator.BHKLBollingerFill(input, numStdDev, period, colorFill, opacityFill);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.BHKLBollingerFill BHKLBollingerFill(double numStdDev, int period, Brush colorFill, int opacityFill)
		{
			return indicator.BHKLBollingerFill(Input, numStdDev, period, colorFill, opacityFill);
		}

		public Indicators.BHKL.BHKLBollingerFill BHKLBollingerFill(ISeries<double> input , double numStdDev, int period, Brush colorFill, int opacityFill)
		{
			return indicator.BHKLBollingerFill(input, numStdDev, period, colorFill, opacityFill);
		}
	}
}

#endregion
