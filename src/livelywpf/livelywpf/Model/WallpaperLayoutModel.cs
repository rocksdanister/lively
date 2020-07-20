using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows;
using Newtonsoft.Json;

namespace livelywpf
{
    /// <summary>
    /// Wallpaper arragement on display.
    /// </summary>
    [Serializable]
    public class WallpaperLayoutModel
    {
        public string DeviceName { get; set; }
        public int BitsPerPixel { get; set; }
        public Rectangle Bounds {get; set;}
        public Rectangle WorkingArea { get; set; }
        public string LivelyInfoPath { get; set; }

        [JsonConstructor]
        public WallpaperLayoutModel(string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea, string livelyInfoPath)
        {
            this.DeviceName = DeviceName;
            this.BitsPerPixel = BitsPerPixel;
            this.Bounds = Bounds;
            this.WorkingArea = WorkingArea;
            this.LivelyInfoPath = livelyInfoPath;
        }

        public WallpaperLayoutModel(Screen Display, string livelyInfoPath)
        {
            this.DeviceName = Display.DeviceName;
            this.BitsPerPixel = Display.BitsPerPixel;
            this.Bounds = Display.Bounds;
            this.WorkingArea = Display.WorkingArea;
            this.LivelyInfoPath = livelyInfoPath;
        }

    }
}
