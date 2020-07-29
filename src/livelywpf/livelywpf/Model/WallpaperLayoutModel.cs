using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows;
using Newtonsoft.Json;
using livelywpf.Model;

namespace livelywpf
{
    /// <summary>
    /// Wallpaper arragement on display.
    /// </summary>
    [Serializable]
    public class WallpaperLayoutModel
    {
        public LivelyScreenModel LivelyScreen { get; set; }
        public string LivelyInfoPath { get; set; }

        [JsonConstructor]
        public WallpaperLayoutModel(string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea, string livelyInfoPath) 
        {
            LivelyScreen = new LivelyScreenModel(DeviceName, BitsPerPixel, Bounds, WorkingArea);
            this.LivelyInfoPath = livelyInfoPath;
        }

        public WallpaperLayoutModel(Screen Display, string livelyInfoPath)
        {
            LivelyScreen = new LivelyScreenModel(Display);
            this.LivelyInfoPath = livelyInfoPath;
        }
    }
}
