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

		public static double GetRedisKey(IRedisClientsPool pool, string key, NinjaScriptBase nsb) 
		{
			double result = 0;
			try {
				IRedisClient client;
				using (client = pool.CreateClientAsync(REDIS_HOST, REDIS_PORT, REDIS_DB).Result)
	            {
					string s = client.GetAsync(key).Result.As<string>();
					result = double.Parse(s);
//	                client.HSetAsync(string.Format("{0}{1}DMMBODown", estDateTime.ToString("yyyyMMdd"), TimeFrameName), this.Instrument.FullName + "|" + SignalTime.ToString("yyyy-MM-dd HH:mm:ss"), CurrentValue);
	            }
			}
			catch (Exception e)
			{
				nsb.Print(key + e);
			}
			return result;
		}
	}
}






