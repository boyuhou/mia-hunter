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
	public class BHKLNDX : Indicator
	{
		private MAX max;
		private MIN min;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "BHKL BHKLNDX";
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
				
				AddPlot(Brushes.DarkCyan,	"SignalValue");
				AddLine(Brushes.DarkGray,					100,			"100Line");
				AddLine(Brushes.DarkGray,					0,				Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.Configure)
			{
				
			}
			
			else if (State == State.DataLoaded)
			{
				max = MAX(High, Period);
				min	= MIN(Low, Period);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period + 1)
				return;
			
			double max0	= max[1];
			double min0	= min[1];
			
			SignalValue[0]	= 100 -100 * (max0 - Close[0]) / (max0 - min0 == 0 ? 1 : max0 - min0);
		}
		
		
		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SignalValue
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
		private BHKL.BHKLNDX[] cacheBHKLNDX;
		public BHKL.BHKLNDX BHKLNDX(int period)
		{
			return BHKLNDX(Input, period);
		}

		public BHKL.BHKLNDX BHKLNDX(ISeries<double> input, int period)
		{
			if (cacheBHKLNDX != null)
				for (int idx = 0; idx < cacheBHKLNDX.Length; idx++)
					if (cacheBHKLNDX[idx] != null && cacheBHKLNDX[idx].Period == period && cacheBHKLNDX[idx].EqualsInput(input))
						return cacheBHKLNDX[idx];
			return CacheIndicator<BHKL.BHKLNDX>(new BHKL.BHKLNDX(){ Period = period }, input, ref cacheBHKLNDX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.BHKLNDX BHKLNDX(int period)
		{
			return indicator.BHKLNDX(Input, period);
		}

		public Indicators.BHKL.BHKLNDX BHKLNDX(ISeries<double> input , int period)
		{
			return indicator.BHKLNDX(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.BHKLNDX BHKLNDX(int period)
		{
			return indicator.BHKLNDX(Input, period);
		}

		public Indicators.BHKL.BHKLNDX BHKLNDX(ISeries<double> input , int period)
		{
			return indicator.BHKLNDX(input, period);
		}
	}
}

#endregion
