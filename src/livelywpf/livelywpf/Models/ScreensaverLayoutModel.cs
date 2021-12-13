using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows;
using Newtonsoft.Json;
using livelywpf.Core;

namespace livelywpf.Models
{
    [Serializable]
    public class ScreensaverLayoutModel : IScreensaverLayoutModel
    {
        public ScreensaverMode Mode { get; set; }
        public WallpaperArrangement Arrangement { get; set; }
        public LivelyScreen LivelyScreen { get; set; }
        public string LivelyInfoPath { get; set; }

        [JsonConstructor]
        public ScreensaverLayoutModel(string DeviceId, string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea, string livelyInfoPath, WallpaperArrangement arrangement, ScreensaverMode mode)
        {
            LivelyScreen = new LivelyScreen(DeviceId, DeviceName, BitsPerPixel, Bounds, WorkingArea);
            this.LivelyInfoPath = livelyInfoPath;
            this.Arrangement = arrangement;
            this.Mode = mode;
        }

        public ScreensaverLayoutModel(ScreensaverMode mode, WallpaperArrangement arrangement, LivelyScreen livelyScreen, string livelyInfoPath)
        {
            this.Mode = mode;
            this.Arrangement = arrangement;
            this.LivelyScreen = livelyScreen;
            this.LivelyInfoPath = livelyInfoPath;
        }
    }
}
