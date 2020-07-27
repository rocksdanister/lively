using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf.Cef
{
    class LivelyPropertiesJSON
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void SaveLivelyProperties(string path, JObject rss)
        {
            try
            {
                File.WriteAllText(path, rss.ToString());
            }
            catch(Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        public static JObject LoadLivelyProperties(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var data = JObject.Parse(json);
                return data;
            }
            catch(Exception e)
            {
                Logger.Error(e.ToString());
                return null;
            }
        }

    }
}
