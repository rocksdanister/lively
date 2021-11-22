using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf.Helpers.Storage
{
    public class JsonUtil
    {
        public static void Write(string path, JObject rss)
        {
            File.WriteAllText(path, rss.ToString());
        }

        public static JObject Read(string path)
        {
            var json = File.ReadAllText(path);
            return JObject.Parse(json);
        }

    }
}
