using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf
{
    class WallpaperLayoutJSON
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Save wallpaper arrangement on display to disk.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePath"></param>
        public static void SaveWallpaperLayout(List<WallpaperLayoutModel> data, string filePath)
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
        /// Load wallpaper arrangement on display from disk.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<WallpaperLayoutModel> LoadWallpaperLayout(string filePath)
        {
            try
            {
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var tmp = (List<WallpaperLayoutModel>)serializer.Deserialize(file, typeof(List<WallpaperLayoutModel>));

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
                //System.Windows.MessageBox.Show(e.ToString());
                Logger.Error(e.ToString());
            }

            return null;
        }
    }
}
