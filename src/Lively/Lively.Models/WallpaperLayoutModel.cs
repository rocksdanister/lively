using Newtonsoft.Json;

namespace Lively.Models
{
    /// <summary>
    /// Wallpaper arragement on display.
    /// </summary>
    public class WallpaperLayoutModel
    {
        [JsonProperty(PropertyName = "LivelyScreen")] //backward compatibility < v1.9
        public DisplayMonitor Display { get; set; }
        public string LivelyInfoPath { get; set; }

        /*
        [JsonConstructor]
        public WallpaperLayoutModel(string DeviceId, string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea, string livelyInfoPath)
        {
            Display = new DisplayMonitor(DeviceId, DeviceName, BitsPerPixel, Bounds, WorkingArea);
            this.LivelyInfoPath = livelyInfoPath;
        }
        */

        public WallpaperLayoutModel(DisplayMonitor Display, string livelyInfoPath)
        {
            this.Display = Display;
            this.LivelyInfoPath = livelyInfoPath;
        }
    }
}
