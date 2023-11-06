using Lively.Common.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lively.Common.Com
{
    //Ref:
    //https://bitbucket.org/ciniml/desktopwallpaper/
    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

        /// <summary>
        /// Gets the monitor device path.
        /// </summary>
        /// <param name="monitorIndex">Index of the monitor device in the monitor device list.</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);
        /// <summary>
        /// Gets number of monitor device paths.
        /// </summary>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetMonitorDevicePathCount();

        [return: MarshalAs(UnmanagedType.Struct)]
        NativeMethods.RECT GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

        void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetBackgroundColor();

        void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);
        [return: MarshalAs(UnmanagedType.I4)]
        DesktopWallpaperPosition GetPosition();

        void SetSlideshow(IntPtr items);
        IntPtr GetSlideshow();

        void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick);
        [PreserveSig]
        uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick);

        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);

        DesktopSlideshowDirection GetStatus();

        bool Enable();
    }

    /// <summary>
    /// This enumeration is used to set and get slideshow options.
    /// </summary> 
    public enum DesktopSlideshowOptions
    {
        ShuffleImages = 0x01,     // When set, indicates that the order in which images in the slideshow are displayed can be randomized.
    }


    /// <summary>
    /// This enumeration is used by GetStatus to indicate the current status of the slideshow.
    /// </summary>
    public enum DesktopSlideshowState
    {
        Enabled = 0x01,
        Slideshow = 0x02,
        DisabledByRemoteSession = 0x04,
    }


    /// <summary>
    /// This enumeration is used by the AdvanceSlideshow method to indicate whether to advance the slideshow forward or backward.
    /// </summary>
    public enum DesktopSlideshowDirection
    {
        Forward = 0,
        Backward = 1,
    }

    /// <summary>
    /// This enumeration indicates the wallpaper position for all monitors. (This includes when slideshows are running.)
    /// The wallpaper position specifies how the image that is assigned to a monitor should be displayed.
    /// </summary>
    public enum DesktopWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    /// <summary>
    /// CoClass DesktopWallpaper
    /// </summary>
    [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    public class DesktopWallpaperClass
    {
    }
}
