using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lively.Common.Helpers.Storage
{
    public class JsonUtil
    {
        public static void Write(string path, JObject rss)
        {
            File.WriteAllText(path, rss.ToString());
        }

        public static JObject ReadJObject(string path)
        {
            var json = File.ReadAllText(path);
            return JObject.Parse(json);
        }

        public static JToken ReadJToken(string path)
        {
            var json = File.ReadAllText(path);
            return JToken.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
