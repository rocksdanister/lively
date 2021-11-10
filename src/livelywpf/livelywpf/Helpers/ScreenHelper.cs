using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using livelywpf.Core;

namespace livelywpf.Helpers
{
    public static class ScreenHelper
    {
        public static event EventHandler DisplayUpdated;
        private static readonly List<ILivelyScreen> displayMonitors = new List<ILivelyScreen>();

        static ScreenHelper()
        {
            //DisplayManager.Initialize();
        }

        public static void Initialize()
        {
            DisplayManager.Initialize();
            UpdateDisplayList();
            DisplayManager.Instance.DisplayUpdated += Instance_DisplayUpdated;
        }

        private static void Instance_DisplayUpdated(object sender, System.EventArgs e)
        {
            UpdateDisplayList();
            DisplayUpdated?.Invoke(null, EventArgs.Empty);
        }

        public static List<ILivelyScreen> GetScreen()
        {
            return displayMonitors;
        }

        public static bool IsMultiScreen()
        {
            return DisplayManager.Instance.DisplayMonitors.Count > 1;
        }

        public static int ScreenCount()
        {
            return DisplayManager.Instance.DisplayMonitors.Count;
        }

        public static ILivelyScreen GetPrimaryScreen()
        {
            return new LivelyScreen(DisplayManager.Instance.PrimaryDisplayMonitor);
        }

        public static bool ScreenExists(ILivelyScreen screen, DisplayIdentificationMode mode)
        {
            bool screenStatus = false;
            switch (mode)
            {
                case DisplayIdentificationMode.deviceName:
                    foreach (var item in Screen.AllScreens)
                    {
                        if (item.DeviceName == screen.DeviceName)
                        {
                            screenStatus = true;
                            break;
                        }
                    }
                    break;
                case DisplayIdentificationMode.deviceId:
                    //ignoring DeviceName which can change during driver update, windows restart etc..
                    screenStatus = GetScreen().FirstOrDefault(x => x.DeviceId == screen.DeviceId) != null;
                    break;
                case DisplayIdentificationMode.screenLayout:
                    screenStatus = GetScreen().FirstOrDefault(x => x.Bounds == screen.Bounds) != null;
                    break;
            }
            return screenStatus;
        }

        public static bool ScreenCompare(ILivelyScreen screen1, ILivelyScreen screen2, DisplayIdentificationMode mode)
        {
            bool screenStatus = false;
            switch (mode)
            {
                case DisplayIdentificationMode.deviceName:
                    screenStatus = (screen1.DeviceName == screen2.DeviceName);
                    break;
                case DisplayIdentificationMode.deviceId:
                    screenStatus = (screen1.DeviceId == screen2.DeviceId);
                    break;
                case DisplayIdentificationMode.screenLayout:
                    screenStatus = (screen1.Bounds == screen2.Bounds);
                    break;
            }
            return screenStatus;
        }

        public static ILivelyScreen GetScreen(string DeviceId, string DeviceName, Rectangle Bounds, Rectangle WorkingArea, DisplayIdentificationMode mode)
        {
            foreach (var item in GetScreen())
            {
                switch (mode)
                {
                    case DisplayIdentificationMode.deviceName:
                        if (item.DeviceName == DeviceName)
                        {
                            return item;
                        }
                        break;
                    case DisplayIdentificationMode.deviceId:
                        //ignoring DeviceName which can change during driver update, windows restart etc..
                        if (item.DeviceId == DeviceId)
                        {
                            return item;
                        }
                        break;
                    case DisplayIdentificationMode.screenLayout:
                        if (item.Bounds == Bounds)
                        {
                            return item;
                        }
                        break;
                }
            }
            return null;
        }

        public static Rectangle GetVirtualScreenBounds()
        {
            return RectToRectangle(DisplayManager.Instance.VirtualScreenBounds);
        }

        public static Rectangle RectToRectangle(System.Windows.Rect rect)
        {
            return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }

        private static void UpdateDisplayList()
        {
            displayMonitors.Clear();
            DisplayManager.Instance.DisplayMonitors.ToList().ForEach(
                screen => displayMonitors.Add(new LivelyScreen(screen)));
        }

        public static ILivelyScreen GetScreenFromPoint(Point pt)
        {
            return new LivelyScreen(
                DisplayManager.Instance.GetDisplayMonitorFromPoint(
                    new System.Windows.Point(pt.X, pt.Y)));
        }

        /// <summary>
        /// Extract last digits of the Screen class DeviceName(WinForm Screen class DeviceName only.), eg: \\.\DISPLAY4 -> 4
        /// </summary>
        /// <param name="DeviceName">devicename string</param>
        /// <returns>-1 if fail</returns>
        public static string GetScreenNumber(string DeviceName)
        {
            if (DeviceName == null)
                return "-1";

            var result = Regex.Match(DeviceName, @"\d+$", RegexOptions.RightToLeft);
            return result.Success ? result.Value : "-1";
        }
    }
}
