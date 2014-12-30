using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace GetMarketCalendar
{
    class CalendarEvent
    {
        [JsonProperty("time")]
        public DateTime? Time { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("event")]
        public string Event { get; set; }
        [JsonProperty("priority")]
        public int Priority { get; set; }
        [JsonProperty("last")]
        public string Last { get; set; }
        [JsonProperty("expectation")]
        public string Expectation { get; set; }
        [JsonProperty("result")]
        public string Result { get; set; }
    }

    class YahooMarketCalendar
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        [JsonProperty("events")]
        public List<CalendarEvent> Events { get; set; }

        private const string YahooMarketUrl = "http://info.finance.yahoo.co.jp/fx/marketcalendar/";
        private const string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko";
        private const string SettingFile = "Settings.json";

        public YahooMarketCalendar()
        {
            if (File.Exists(SettingFile))
            {
                try
                {
                    using (StreamReader reader = File.OpenText(SettingFile))
                    {
                        JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                        this.Date = DateTime.Parse((string)o["settings"]["date"]);
                    }
                }
                catch (Exception)
                {
                    this.Date = DateTime.Now;
                }
            }
            else
                this.Date = DateTime.Now;
            GetYahooMarketCalendar();
        }
        
        public YahooMarketCalendar(DateTime date)
        {
            this.Date = date;
            GetYahooMarketCalendar();
        }

        public void SaveJson()
        {
            string dataDirectry = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Json");
            if (!Directory.Exists(dataDirectry))
                Directory.CreateDirectory(dataDirectry);
            string path = Path.Combine(dataDirectry, "Eco" + Date.ToString("yyyyMmdd") + ".json");
            JsonSerializer serializer = new JsonSerializer();
            //serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(path))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this);
            }
        }

        private void GetYahooMarketCalendar()
        {
            try
            {
                Events = new List<CalendarEvent>();
                string html = GetYahooMarketHtml();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                YahooMarketInfo(doc);
            }
            catch(Exception e1)
            {
                LoggerClass.NLogInfo("error:" + e1.Message);   
            }
        }

        private string GetYahooMarketHtml()
        {
            string url = YahooMarketUrl + String.Format("/?d={0:yyyyMMdd}&c=ALL&i=0", Date);
            using (var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
                                         | DecompressionMethods.Deflate
            }))
            {
                client.BaseAddress = new Uri("http://info.finance.yahoo.co.jp/fx/marketcalendar/");
                client.DefaultRequestHeaders.Referrer = new Uri("http://info.finance.yahoo.co.jp/fx/marketcalendar/");
                client.DefaultRequestHeaders.Add("Acceptt", "text/html, application/xhtml+xml, */*");
                client.DefaultRequestHeaders.Add("Accept-Language", "ja,en-US;q=0.7,en;q=0.3");
                client.DefaultRequestHeaders.Add("user-agent", UserAgent);

                return client.GetStringAsync(url).Result;
            }
        }

        private void YahooMarketInfo(HtmlDocument doc)
        {
            var eco = doc.DocumentNode.Descendants("div")
                .FirstOrDefault(
                    x => x.Attributes["class"] != null && x.Attributes["class"].Value == "ecoEventTbl02 marB20");
            if (eco == null)
                return;
            bool dateFlag = false;
            foreach (var economicEvent in eco.Descendants("tr"))
            {
                var daterow = economicEvent.Descendants("th").FirstOrDefault();
                if (daterow != null)
                {
                    if (daterow.Attributes["class"] != null && daterow.Attributes["class"].Value == "date")
                    {
                        string publish = daterow.InnerText.Trim();
                        publish = publish.Substring(0, publish.IndexOf(' '));
                     
                        if(publish == Date.ToString("M/d"))
                            dateFlag = true;
                        else
                        {
                            if(dateFlag)
                                break;
                        }
                    }
                }
                else
                {
                    if (dateFlag)
                    {
                        try
                        {
                            var item = economicEvent.Descendants("td");
                            if (item != null && item.Count() >= 6)
                            {
                                this.Events.Add(new CalendarEvent
                                {
                                    Time = GetTime(item.ElementAt(0).InnerText),
                                    Country = GetCountry(item.ElementAt(1).ChildNodes[0]),
                                    Event = item.ElementAt(1).InnerText,
                                    Priority = GetPriory(item.ElementAt(2)),
                                    Last = item.ElementAt(3).InnerText,
                                    Expectation = item.ElementAt(4).InnerText,
                                    Result = item.ElementAt(5).InnerText,
                                });
                            }
                        }
                        catch (Exception e1)
                        {
                            LoggerClass.NLogInfo("error:" + e1.Message);
                        }
                    }
                }
            }
        }

        private string GetCountry(HtmlNode doc)
        {
            string s = doc.Attributes["class"].Value;
            s = s.Substring(s.IndexOf(' ') + 4);
            if (Regex.IsMatch(s, "^[A-Z][a-z][a-z][0-9]"))
                return s.Substring(0, 3);
            else if(Regex.IsMatch(s, "^[A-Z][a-z][0-9]"))
                return s.Substring(0, 2);
            return "-";
        }

        private int GetPriory(HtmlNode doc)
        {
            try
            {
                string icoRating = doc.Descendants("span").FirstOrDefault().Attributes["class"].Value;
                switch (icoRating)
                {
                    case "icoRating1":
                        return 1;
                    case "icoRating2":
                        return 2;
                    case "icoRating3":
                        return 3;
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private DateTime? GetTime(string s)
        {
            string[] stime = s.Trim().Split(':');
            try
            {
                return new DateTime(this.Date.Year, this.Date.Month, this.Date.Day,
                    int.Parse(stime[0]), int.Parse(stime[1]), 0);        
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
