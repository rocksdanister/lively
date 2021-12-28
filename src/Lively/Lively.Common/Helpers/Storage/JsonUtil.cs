using System.IO;
using Newtonsoft.Json.Linq;

namespace Lively.Common.Helpers.Storage
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
