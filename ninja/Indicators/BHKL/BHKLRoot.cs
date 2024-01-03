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
using RedisBoost;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.BHKL
{
	#region KL bar
	public class BHKLBar
	{
		public double rl10 {get;set;}
		public double rl10Prior {get;set;}
		public double rl10Prior2 {get;set;}
		public double rl10sumY {get;set;}
		public double rl10sumYPrior {get;set;}
		public double rl10sumXY {get;set;}
		public double rl10sumXYPrior {get;set;}
		public int rl10Period {get;set;}
		public double rl10sumX {get;set;}
		public double rl10sumX2 {get;set;}
		public double rl10divisor {get;set;}
		public double rl10avg {get;set;}
		public double rl10intercept {get;set;}
		
		public double rl30 {get;set;}
		public double rl30Prior {get;set;}
		public double rl30Prior2 {get;set;}
		public double rl30sumY {get;set;}
		public double rl30sumYPrior {get;set;}
		public double rl30sumXY {get;set;}
		public double rl30sumXYPrior {get;set;}
		public int rl30Period {get;set;}
		public double rl30sumX {get;set;}
		public double rl30sumX2 {get;set;}
		public double rl30divisor {get;set;}
		public double rl30avg {get;set;}
		public double rl30intercept {get;set;}
		
		public double psar {get;set;}
		public double psarPrior {get;set;}
		public double psarPrior2 {get;set;}
		
		public double rl10slope {get;set;}
		public double rl10slopePrior {get;set;}
		public double rl10slopePrior2 {get;set;}
		
		public double rl30slope {get;set;}
		public double rl30slopePrior {get;set;}
		public double rl30slopePrior2 {get;set;}
		
		public double macdValue {get;set;}
		public double macdValuePrior {get;set;}
		public double fastEma {get;set;}
		public double fastEmaPrior {get;set;}
		public double slowEma {get;set;}
		public double slowEmaPrior {get;set;}
		public int macdSeason {get;set;} // 0 = spring, 1 = summer, 2 = fall, 3 = winter
		
		
		public void RollTime()
		{
			this.rl10Prior2 = this.rl10Prior;
			this.rl10Prior = this.rl10;
			this.rl10sumYPrior = this.rl10sumY;
			this.rl10sumXYPrior = this.rl10sumXY;
			this.rl10sumX = this.rl10Period * (this.rl10Period - 1) * 0.5;
			this.rl10sumX2 = this.rl10Period * (this.rl10Period + 1) * 0.5;
			this.rl10divisor = this.rl10Period * (this.rl10Period + 1) * (2 * this.rl10Period + 1) / 6 - this.rl10sumX2 * this.rl10sumX2 / this.rl10Period;
			
			this.rl30Prior2 = this.rl30Prior;
			this.rl30Prior = this.rl30;
			this.rl30sumYPrior = this.rl30sumY;
			this.rl30sumXYPrior = this.rl30sumXY;
			this.rl30sumX = this.rl30Period * (this.rl30Period - 1) * 0.5;
			this.rl30sumX2 = this.rl30Period * (this.rl30Period + 1) * 0.5;
			this.rl30divisor = this.rl30Period * (this.rl30Period + 1) * (2 * this.rl30Period + 1) / 6 - this.rl30sumX2 * this.rl30sumX2 / this.rl30Period;
			
			this.psarPrior2 = this.psarPrior;
			this.psarPrior = this.psar;
			
			this.rl10slopePrior2 = this.rl10slopePrior;
			this.rl10slopePrior = this.rl10slope;
			
			this.rl30slopePrior2 = this.rl30slopePrior;
			this.rl30slopePrior = this.rl30slope;
			
			this.macdValuePrior = this.macdValue;
			this.fastEmaPrior = this.fastEma;
			this.slowEmaPrior = this.slowEma;
		}
	}
	#endregion
	
	public class BHKLRoot
	{
		public static string REDIS_HOST = "127.0.0.1";
		public static int REDIS_PORT = 6379;
		public static int REDIS_DB = 0;
		public static IRedisClientsPool POOL = RedisClient.CreateClientsPool();
		public static int MACD_FAST = 10;
		public static int MACD_SLOW = 30;
		public static int MACD_SMOOTH = 5;
		
		public static void UpdateBar(NinjaScriptBase nsb, ref BHKLBar bar, ISeries<double> Close, SUM sum10, SUM sum30, int rl10Period = 10, int rl30Period = 10)
		{
			if (nsb.IsFirstTickOfBar)
			{
				bar.rl10Period = Math.Min(nsb.CurrentBar + 1, rl10Period);
				bar.rl30Period = Math.Min(nsb.CurrentBar + 1, rl30Period);
				bar.RollTime();
			}
			
			#region rl10 calc
			double inputRl10 = Close[0];
			bar.rl10sumXY = bar.rl10sumXYPrior - (nsb.CurrentBar >= rl10Period ? bar.rl10sumYPrior : 0) + bar.rl10Period * inputRl10;
			bar.rl10sumY = bar.rl10sumYPrior + inputRl10 - (nsb.CurrentBar >= rl10Period ? Close[rl10Period] : 0);
			bar.rl10avg = bar.rl10sumY / bar.rl10Period;
			bar.rl10slope = (bar.rl10sumXY - bar.rl10sumX2 * bar.rl10avg) / bar.rl10divisor;
			bar.rl10intercept = (sum10[0] - bar.rl10slope * bar.rl10sumX) / bar.rl10Period;
			bar.rl10 = nsb.CurrentBar == 0 ? inputRl10 : (bar.rl10intercept + bar.rl10slope * (bar.rl10Period - 1));
			#endregion
			
			#region rl30 calc
			double inputRl30 = Close[0];
			bar.rl30sumXY = bar.rl30sumXYPrior - (nsb.CurrentBar >= rl30Period ? bar.rl30sumYPrior : 0) + bar.rl30Period * inputRl30;
			bar.rl30sumY = bar.rl30sumYPrior + inputRl30 - (nsb.CurrentBar >= rl30Period ? Close[rl30Period] : 0);
			bar.rl30avg = bar.rl30sumY / bar.rl30Period;
			bar.rl30slope = (bar.rl30sumXY - bar.rl30sumX2 * bar.rl30avg) / bar.rl30divisor;
			bar.rl30intercept = (sum30[0] - bar.rl30slope * bar.rl30sumX) / bar.rl30Period;
			bar.rl30 = nsb.CurrentBar == 0 ? inputRl30 : (bar.rl30intercept + bar.rl30slope * (bar.rl30Period - 1));
			#endregion
			
			#region macd calc
			var constant1 = 2.0 / (1 + MACD_FAST);
			var constant2 = 1 - (2.0 / (1 + MACD_FAST));
			var constant3 = 2.0 / (1 + MACD_SLOW);
			var constant4 = 1 - (2.0 / (1 + MACD_SLOW));
			var constant5 = 2.0 / (1 + MACD_SMOOTH);
			var constant6 = 1 - (2.0 / (1 + MACD_SMOOTH));
			if (nsb.CurrentBar == 0) {
				bar.fastEma = Close[0];
				bar.slowEma = Close[0];
				bar.macdValue = 0;
			}
			else {
				bar.fastEma = constant1 * bar.rl10 + constant2 * bar.fastEmaPrior;
				bar.slowEma = constant3 * bar.rl10 + constant4 * bar.slowEmaPrior;
				bar.macdValue = bar.fastEma - bar.slowEma;
			}
			
			if (bar.macdValue >= 0 && bar.macdValue >= bar.macdValuePrior) {
				bar.macdSeason = 1;
			} else if (bar.macdValue >=0 && bar.macdValue < bar.macdValuePrior) {
				bar.macdSeason = 2;
			} else if (bar.macdValue < 0 && bar.macdValue >= bar.macdValuePrior) {
				bar.macdSeason = 0;
			} else if (bar.macdValue <0 && bar.macdValue < bar.macdValuePrior) {
				bar.macdSeason = 3;
			}
			#endregion
		}
		
		public static double GetRedisValue(NinjaScriptBase nsb, string postKey = "")
		{
			var masterName = nsb.Instrument.FullName.Split(' ')[0];
			
			var redisName = masterName;
			
			if (nsb.Instrument.MasterInstrument.InstrumentType.ToString() == "Forex")
			{
				redisName = redisName + ".FXCM";
			}
			else if (nsb.Instrument.MasterInstrument.InstrumentType.ToString() == "Future")
			{
				redisName = GetRedisFutureName(nsb);
			}
			
			if (string.IsNullOrWhiteSpace(postKey))
			{
				redisName = redisName + postKey;
			}
			double multiplier = GetMultiplier(nsb);
			double result = 0;
			try {
				IRedisClient client;
				using (client = POOL.CreateClientAsync(REDIS_HOST, REDIS_PORT, REDIS_DB).Result)
	            {
					string s = client.GetAsync(redisName).Result.As<string>();
					result = double.Parse(s);
//	                client.HSetAsync(string.Format("{0}{1}DMMBODown", estDateTime.ToString("yyyyMMdd"), TimeFrameName), this.Instrument.FullName + "|" + SignalTime.ToString("yyyy-MM-dd HH:mm:ss"), CurrentValue);
	            }
			}
			catch (Exception e)
			{
				nsb.Print(redisName + e);
			}
			return result * multiplier;
			
		}
		
		public static double GetMultiplier(NinjaScriptBase nsb){
			double multiplier = 1;
			if (nsb.Instrument.MasterInstrument.InstrumentType.ToString() == "Future")
			{
				var list = new List<string>();
				list.Add("MES");
				list.Add("MNQ");
				list.Add("M2K");
				list.Add("MYM");
				list.Add("MGC");
				list.Add("MCL");
				list.Add("MBT");
				list.Add("MET");
				
				if (list.Contains(nsb.Instrument.FullName.Split(' ')[0]))
					multiplier = 0.1;
			}
			return multiplier;
		}
		
		public static string GetRedisFutureName(NinjaScriptBase nsb) 
		{
			var dictMapping = new Dictionary<string, string>();
			
			// index
			dictMapping.Add("ES", "@ES#");
			dictMapping.Add("MES", "@ES#");
			dictMapping.Add("NQ", "@NQ#");
			dictMapping.Add("MNQ", "@NQ#");
			dictMapping.Add("YM", "@YM#");
			dictMapping.Add("MYM", "@YM#");
			dictMapping.Add("RTY", "@RTY#");
			dictMapping.Add("M2K", "@RTY#");
			
			// energy
			dictMapping.Add("CL", "QCL#");
			dictMapping.Add("MCL", "QCL#");
			dictMapping.Add("HO", "IHO#");
			dictMapping.Add("NG", "QNG#");
			dictMapping.Add("B", "EB#");
			dictMapping.Add("RB", "QRB#");
			
			// metal
			dictMapping.Add("HG", "QHG#");
			dictMapping.Add("GC", "QGC#");
			dictMapping.Add("MGC", "QGC#");
			dictMapping.Add("PL", "QPL#");
			dictMapping.Add("SI", "QSI#");
			
			// currency
			dictMapping.Add("6A", "@AD#");  
			dictMapping.Add("6B", "@BP#");
			dictMapping.Add("6C", "@CD#");
			dictMapping.Add("DX", "@DX#");
			dictMapping.Add("6E", "@EU#");
			dictMapping.Add("6J", "@JY#");
			dictMapping.Add("6M", "@PX#");
			dictMapping.Add("6N", "@NE#"); 
			dictMapping.Add("6S", "@SF#");
			dictMapping.Add("BTC", "@BTC#");
			dictMapping.Add("MBT", "@BTC#");
			dictMapping.Add("ETH", "@ETH#");
			dictMapping.Add("MET", "@ETH#");
			
			// soft
			dictMapping.Add("CC", "@CC#");
			dictMapping.Add("KC", "@KC#");
			dictMapping.Add("CT", "@CT#");
			dictMapping.Add("SB", "@SB#");
			
			// bonds
			dictMapping.Add("ZT", "@TU#");
			dictMapping.Add("ZF", "@FV#");
			dictMapping.Add("ZN", "@TY#");
			dictMapping.Add("ZB", "@US#");
			dictMapping.Add("UB", "@UB#");
			
			// grain
			dictMapping.Add("ZL", "@BO#");
			dictMapping.Add("ZM", "@SM#");
			dictMapping.Add("ZS", "@S#");
			dictMapping.Add("ZW", "@W#");
			dictMapping.Add("ZC", "@C#");
			
			// meat
			dictMapping.Add("LE", "@LE#");
			dictMapping.Add("GF", "@GF#");
			dictMapping.Add("HE", "@HE#");
			
			return dictMapping[nsb.Instrument.FullName.Split(' ')[0]];
		}
	}
}
































