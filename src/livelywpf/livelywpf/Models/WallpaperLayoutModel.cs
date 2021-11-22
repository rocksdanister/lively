using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows;
using Newtonsoft.Json;
using livelywpf.Core;

namespace livelywpf.Models
{
    /// <summary>
    /// Wallpaper arragement on display.
    /// </summary>
    [Serializable]
    public class WallpaperLayoutModel : IWallpaperLayoutModel
    {
        public LivelyScreen LivelyScreen { get; set; }
        public string LivelyInfoPath { get; set; }

        [JsonConstructor]
        public WallpaperLayoutModel(string DeviceId, string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea, string livelyInfoPath)
        {
            LivelyScreen = new LivelyScreen(DeviceId, DeviceName, BitsPerPixel, Bounds, WorkingArea);
            this.LivelyInfoPath = livelyInfoPath;
        }

        public WallpaperLayoutModel(LivelyScreen Display, string livelyInfoPath)
        {
            LivelyScreen = Display;
            this.LivelyInfoPath = livelyInfoPath;
        }
    }
}
