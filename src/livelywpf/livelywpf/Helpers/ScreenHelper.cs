using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using livelywpf.Core;

namespace livelywpf
{
    public static class ScreenHelper
    {
        static ScreenHelper()
        {
            //DisplayManager.Initialize();
        }

        public static void Initialize()
        {
            DisplayManager.Initialize();
        }

        public static bool IsMultiScreen()
        {
            return DisplayManager.Instance.DisplayMonitors.Count > 1;
        }

        public static int ScreenCount()
        {
            return DisplayManager.Instance.DisplayMonitors.Count;
        }

        public static List<LivelyScreen> GetScreen()
        {
            var result = new List<LivelyScreen>();
            foreach (var item in DisplayManager.Instance.DisplayMonitors)
            {
                result.Add(new LivelyScreen(item));
            }
            return result;
        }

        public static LivelyScreen GetPrimaryScreen()
        {
            return new LivelyScreen(DisplayManager.Instance.PrimaryDisplayMonitor);
        }

        public static bool ScreenExists(LivelyScreen screen, DisplayIdentificationMode mode)
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

        public static bool ScreenCompare(LivelyScreen screen1, LivelyScreen screen2, DisplayIdentificationMode mode)
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

        public static LivelyScreen GetScreen(string DeviceId, string DeviceName, Rectangle Bounds, Rectangle WorkingArea, DisplayIdentificationMode mode)
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
