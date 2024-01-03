#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Globalization;
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
	public class BHKLTradePlot : Indicator
	{
		private List<MyOrder> myOrders = new List<MyOrder>();
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "BHKL Trade Plot";
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
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				LoadFile();
				int counter = 1;
				foreach (MyOrder myOrder in myOrders)
				{
					if (myOrder.Action.Equals("BUY", StringComparison.InvariantCultureIgnoreCase))
					{
						Draw.Dot(this, "BHKLTradePlot"+counter.ToString(), true, myOrder.Time, myOrder.AvgPrice, Brushes.Green);
					}
					else if (myOrder.Action.Equals("SELL", StringComparison.InvariantCultureIgnoreCase))
					{
						Draw.Dot(this, "BHKLTradePlot"+counter.ToString(), true, myOrder.Time, myOrder.AvgPrice, Brushes.Red);
					}
					else 
					{
						Draw.Dot(this, "BHKLTradePlot"+counter.ToString(), true, myOrder.Time, myOrder.AvgPrice, Brushes.Purple);
					}
					counter++;
				}
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
		
		private void LoadFile()
		{
			if (!File.Exists(FilePath))
			{
				return;
			}
			string[] formats = {"yyyy-MM-dd HH:mm:ss", "M/d/yyyy H:mm","yyyy-MM-dd HH:mm","yyyy-MM-dd H:mm"};
			
			using(var fs = File.OpenRead(FilePath))
			using(var reader = new StreamReader(fs))
			{
				while(!reader.EndOfStream)
				{
					DateTime entryTime;
					DateTime exitTime;
					
					var line = reader.ReadLine();
                    var values = line.Split(',');
					string ticker = values[0];
					if (!ticker.Equals(this.Instrument.FullName, StringComparison.InvariantCultureIgnoreCase))
						continue;
					
					var amount = values[1];
					
					if (!DateTime.TryParseExact(values[2], formats, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out entryTime))
					{
						throw new Exception(line);
					}
					
					var entryPrice = Double.Parse(values[3]);
					var stopPrice = Double.Parse(values[4]);
					
					if (!DateTime.TryParseExact(values[5], formats, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out exitTime))
					{
						throw new Exception(line);
					}
					
					var exitPrice = Double.Parse(values[6]);
					var direction = values[7];
					
					
					if (direction.Equals("Long", StringComparison.InvariantCultureIgnoreCase))
					{
						this.myOrders.Add(new MyOrder("BUY", entryPrice, amount, entryTime));
						this.myOrders.Add(new MyOrder("SELL", exitPrice, amount, exitTime));
						this.myOrders.Add(new MyOrder("STOP", stopPrice, amount, entryTime));
					}
					else
					{
						this.myOrders.Add(new MyOrder("SELL", entryPrice, amount, entryTime));
						this.myOrders.Add(new MyOrder("BUY", exitPrice, amount, exitTime));
						this.myOrders.Add(new MyOrder("STOP", stopPrice, amount, entryTime));
					}
				}
			}
		}
		
		public class MyOrder
		{
			public string Action {get; set;}
			public string Quantity {get; set;}
			public double AvgPrice {get; set;}
			public DateTime Time {get; set;}
			
			public MyOrder(string Action, double AvgPrice, string Quantity, DateTime Time)
			{
				this.Action = Action;
				this.AvgPrice = AvgPrice;
				this.Quantity = Quantity;
				this.Time = Time;
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name="FilePath", Order=0, GroupName="Parameters")]
		public string FilePath
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BHKL.BHKLTradePlot[] cacheBHKLTradePlot;
		public BHKL.BHKLTradePlot BHKLTradePlot(string filePath)
		{
			return BHKLTradePlot(Input, filePath);
		}

		public BHKL.BHKLTradePlot BHKLTradePlot(ISeries<double> input, string filePath)
		{
			if (cacheBHKLTradePlot != null)
				for (int idx = 0; idx < cacheBHKLTradePlot.Length; idx++)
					if (cacheBHKLTradePlot[idx] != null && cacheBHKLTradePlot[idx].FilePath == filePath && cacheBHKLTradePlot[idx].EqualsInput(input))
						return cacheBHKLTradePlot[idx];
			return CacheIndicator<BHKL.BHKLTradePlot>(new BHKL.BHKLTradePlot(){ FilePath = filePath }, input, ref cacheBHKLTradePlot);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BHKL.BHKLTradePlot BHKLTradePlot(string filePath)
		{
			return indicator.BHKLTradePlot(Input, filePath);
		}

		public Indicators.BHKL.BHKLTradePlot BHKLTradePlot(ISeries<double> input , string filePath)
		{
			return indicator.BHKLTradePlot(input, filePath);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BHKL.BHKLTradePlot BHKLTradePlot(string filePath)
		{
			return indicator.BHKLTradePlot(Input, filePath);
		}

		public Indicators.BHKL.BHKLTradePlot BHKLTradePlot(ISeries<double> input , string filePath)
		{
			return indicator.BHKLTradePlot(input, filePath);
		}
	}
}

#endregion
