using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf
{
    // TODO:
    // Use generics for all json save and read fn.
    class ApplicationRulesJSON
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Save apprules to disk.
        /// </summary>
        /// <param name="data">Apprules list</param>
        /// <param name="filePath">Save filepath</param>
        public static void SaveAppRules(List<ApplicationRulesModel> data, string filePath)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include
            };

            try
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, data);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        /// <summary>
        /// Load lively wallpaper metadata file from disk.
        /// </summary>
        /// <param name="filePath">livelyinfo.json filepath</param>
        /// <returns>livelyinfo data</returns>
        public static List<ApplicationRulesModel> LoadAppRules(string filePath)
        {
            try
            {
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var tmp = (List<ApplicationRulesModel>)serializer.Deserialize(file, typeof(List<ApplicationRulesModel>));

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
            }

            return null;
        }
    }
}
