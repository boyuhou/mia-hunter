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
	public class BHKLRoot
	{
		public static string REDIS_HOST = "127.0.0.1";
		public static int REDIS_PORT = 6379;
		public static int REDIS_DB = 0;
		public static IRedisClientsPool POOL = RedisClient.CreateClientsPool();		
		
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












