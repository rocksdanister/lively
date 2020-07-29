using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;

namespace livelywpf.Model
{
    [Serializable]
    public class LivelyScreenModel
    {
        public string DeviceName { get; set; }
        public string DeviceNumber { get; set; }
        public int BitsPerPixel { get; set; }
        public Rectangle Bounds { get; set; }
        public Rectangle WorkingArea { get; set; }

        [JsonConstructor]
        public LivelyScreenModel(string DeviceName, int BitsPerPixel, Rectangle Bounds, Rectangle WorkingArea)
        {
            this.DeviceName = DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(DeviceName);
            this.BitsPerPixel = BitsPerPixel;
            this.Bounds = Bounds;
            this.WorkingArea = WorkingArea;
        }

        public LivelyScreenModel(Screen Display)
        {
            this.DeviceName = Display.DeviceName;
            this.DeviceNumber = ScreenHelper.GetScreenNumber(Display);
            this.BitsPerPixel = Display.BitsPerPixel;
            this.Bounds = Display.Bounds;
            this.WorkingArea = Display.WorkingArea;
        }
    }
}
