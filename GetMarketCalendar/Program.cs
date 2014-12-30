using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace GetMarketCalendar
{
    class Program
    {
        static void Main(string[] args)
        {
            YahooMarketCalendar ymc = new YahooMarketCalendar();
            ymc.SaveJson();
        }
    }

    class LoggerClass
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void NLogInfo(string message)
        {
            logger.Info(message);
        }
    }
}
