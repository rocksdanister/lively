using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf
{
    class SettingsJSON
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static SettingsModel LoadConfig(string filePath)
        {
            try
            {
                // deserialize JSON directly from a file
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var tmp = (SettingsModel)serializer.Deserialize(file, typeof(SettingsModel));

                    //if file is corrupted, json can return null.
                    if (tmp == null)
                    {
                        throw new ArgumentNullException("json null/corrupt");
                    }
                    else
                    {
                        return tmp;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                return null;
            }
        }

        public static void SaveConfig(string filePath, SettingsModel configFile)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                //serializer.Converters.Add(new JavaScriptDateTimeConverter());
                NullValueHandling = NullValueHandling.Include
            };

            try
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, configFile);
                }

            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

    }
}
