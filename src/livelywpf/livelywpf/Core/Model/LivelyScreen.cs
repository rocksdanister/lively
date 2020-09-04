using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;

namespace livelywpf.Core
{
    /// <summary>
    /// Currently very similar to winform screen class, the idea is to abstract it...
    /// So that in the future when I remove/change winform library only this and ScreenHelper.cs file require modification.
    /// </summary>
    [Serializable]
    public class LivelyScreen
    {
        public string DeviceName { get; set; }
        public string DeviceNumber { get; set; }
        public int BitsPerPixel { get; set; }
        public Rectangle Bounds { get; set; }
        public Rectangle WorkingArea { get; set; }

        [JsonConstructor]
        public LivelyScreen(string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea)
        {
            this.DeviceName = DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(DeviceName);
            this.BitsPerPixel = BitsPerPixel;
            this.Bounds = Bounds;
            this.WorkingArea = WorkingArea;
        }

        public LivelyScreen(Screen Display)
        {
            this.DeviceName = Display.DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(Display.DeviceName);
            this.BitsPerPixel = Display.BitsPerPixel;
            this.Bounds = Display.Bounds;
            this.WorkingArea = Display.WorkingArea;
        }
    }
}
