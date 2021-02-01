using System;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;
using System.Windows;
using System.Linq;

namespace livelywpf.Core
{
    /// <summary>
    /// Currently very similar to winform screen class, the idea is to abstract it...
    /// So that in the future when I remove/change winform library only this and ScreenHelper.cs file require modification.
    /// </summary>
    [Serializable]
    public class LivelyScreen
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
            this.DeviceId = DeviceId ?? (ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceName == DeviceName)?.DeviceId);
            this.DeviceName = DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(DeviceName);
            this.BitsPerPixel = BitsPerPixel;
            this.Bounds = Bounds;
            this.WorkingArea = WorkingArea;
        }

        public LivelyScreen(DisplayMonitor Display)
        {
            this.DeviceId = Display.DeviceId;
            this.DeviceName = Display.DeviceName;
            this.DeviceNumber = Display.Index.ToString();
            this.BitsPerPixel = 0;
            this.Bounds = RectToRectangle(Display.Bounds);
            this.WorkingArea = RectToRectangle(Display.WorkingArea);
        }

        public LivelyScreen(Screen Display)
        {
            //Screen class does not have DeviceId.
            this.DeviceId = ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceName == Display.DeviceName)?.DeviceId;
            this.DeviceName = Display.DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(Display.DeviceName);
            this.BitsPerPixel = Display.BitsPerPixel;
            this.Bounds = Display.Bounds;
            this.WorkingArea = Display.WorkingArea;
        }

        Rectangle RectToRectangle(Rect rect)
        {
            return new Rectangle() { Width = (int)rect.Width, Height = (int)rect.Height, X = (int)rect.X, Y = (int)rect.Y };
        }

    }
}
