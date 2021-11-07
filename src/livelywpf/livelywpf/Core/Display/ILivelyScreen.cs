using System;
using System.Drawing;

namespace livelywpf.Core
{
    public interface ILivelyScreen : IEquatable<ILivelyScreen>
    {
        int BitsPerPixel { get; set; }
        Rectangle Bounds { get; set; }
        string DeviceId { get; set; }
        string DeviceName { get; set; }
        string DeviceNumber { get; set; }
        Rectangle WorkingArea { get; set; }
    }
}