using System;
using System.Drawing;
using Newtonsoft.Json;
using System.Linq;

namespace livelywpf.Core
{
    /// <summary>
    /// Currently very similar to winform screen class, the idea is to abstract it...
    /// So that in the future when I remove/change winform library only this and ScreenHelper.cs file require modification.
    /// </summary>
    [Serializable]
    public class LivelyScreen : IEquatable<LivelyScreen>
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceNumber { get; set; }
        public int BitsPerPixel { get; set; }
        public Rectangle Bounds { get; set; }
        public Rectangle WorkingArea { get; set; }

        [JsonConstructor]
        public LivelyScreen(string DeviceId, string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea)
        {
            //Backward compatibility: lively < v1.1.9 does not have DeviceId.
            this.DeviceId = DeviceId ?? (ScreenHelper.GetScreen().FirstOrDefault(x => x.Bounds == Bounds)?.DeviceId); 
            this.DeviceName = DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(DeviceName);
            this.BitsPerPixel = BitsPerPixel;
            this.Bounds = Bounds;
            this.WorkingArea = WorkingArea;
        }

        public LivelyScreen(DisplayMonitor display)
        {
            this.DeviceId = display.DeviceId;
            this.DeviceName = display.DeviceName;
            this.DeviceNumber = display.Index.ToString();
            this.BitsPerPixel = 0;
            this.Bounds = ScreenHelper.RectToRectangle(display.Bounds);
            this.WorkingArea = ScreenHelper.RectToRectangle(display.WorkingArea);
        }

        public LivelyScreen(LivelyScreen display)
        {
            this.DeviceId = display.DeviceId;
            this.DeviceName = display.DeviceName;
            this.DeviceNumber = display.DeviceNumber;
            this.BitsPerPixel = 0;
            this.Bounds = display.Bounds;
            this.WorkingArea = display.WorkingArea;
        }

        public LivelyScreen(System.Windows.Forms.Screen display)
        {
            //Screen class does not have DeviceId.
            this.DeviceId = ScreenHelper.GetScreen().FirstOrDefault(x => x.Bounds == Bounds)?.DeviceId;
            this.DeviceName = display.DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(display.DeviceName);
            this.BitsPerPixel = display.BitsPerPixel;
            this.Bounds = display.Bounds;
            this.WorkingArea = display.WorkingArea;
        }

        public bool Equals(LivelyScreen other)
        {
            return other.DeviceId == this.DeviceId;
        }
    }
}
