using System;
using System.IO;
using Newtonsoft.Json;

namespace Lively.Common.Helpers.Storage
{
    public static class JsonStorage<T>
    {
        public static T LoadData(string filePath)
        {
            // deserialize JSON directly from a file
            using StreamReader file = File.OpenText(filePath);
            var serializer = new JsonSerializer
            {
                //TypeNameHandling = TypeNameHandling.Auto
            };
            var tmp = (T)serializer.Deserialize(file, typeof(T));

            //if file is corrupted, json can return null.
            return (tmp != null ? tmp : throw new ArgumentNullException("json null/corrupt"));
        }

        public static void StoreData(string filePath, T data)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                //serializer.Converters.Add(new JavaScriptDateTimeConverter());
                NullValueHandling = NullValueHandling.Include,
                //TypeNameHandling = TypeNameHandling.Auto,
            };

            using StreamWriter sw = new StreamWriter(filePath);
            using JsonWriter writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, data, typeof(T));
        }
    }
}
