using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf
{
    class LivelyInfoJSON
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Save lively wallpaper metadata file on disk.
        /// </summary>
        /// <param name="data">livelyinfo data</param>
        /// <param name="filePath">Save filepath</param>
        public static void SaveWallpaperMetaData(LivelyInfoModel data, string filePath) 
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
        public static LivelyInfoModel LoadWallpaperMetaData(string filePath)
        {
            try
            {
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var tmp = (LivelyInfoModel)serializer.Deserialize(file, typeof(LivelyInfoModel));

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
