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
        public static bool IsMultiScreen()
        {
            return Screen.AllScreens.Count() > 1;
        }

        public static int ScreenCount()
        {
            return Screen.AllScreens.Count();
        }

        public static List<LivelyScreen> GetScreen()
        {
            //todo optimise
            var result = new List<LivelyScreen>();
            foreach (var item in Screen.AllScreens)
            {
                result.Add(new LivelyScreen(item));
            }
            return result;
        }

        public static Screen[] GetScreenWinform()
        {
            return Screen.AllScreens;
        }

        public static LivelyScreen GetPrimaryScreen()
        {
            return new LivelyScreen(Screen.PrimaryScreen);
        }

        public static bool ScreenExists(Screen screen, DisplayIdentificationMode mode)
        {
            bool screenStatus = false;
            switch (mode)
            {
                case DisplayIdentificationMode.screenClass:
                    foreach (var item in Screen.AllScreens)
                    {
                        if(screen.Equals(item))
                        {
                            screenStatus = true;
                            break;
                        }
                    }
                    break;
                case DisplayIdentificationMode.screenLayout:
                    //ignoring DeviceName which can change during driver update, windows restart etc..
                    foreach (var item in Screen.AllScreens)
                    {
                        if(item.Bounds == screen.Bounds)
                        {
                            screenStatus = true;
                            break;
                        }
                    }
                    break;
            }
            return screenStatus;
        }

        public static bool ScreenExists(LivelyScreen screen, DisplayIdentificationMode mode)
        {
            bool screenStatus = false;
            switch (mode)
            {
                case DisplayIdentificationMode.screenClass:
                    foreach (var item in Screen.AllScreens)
                    {
                        if (item.DeviceName == screen.DeviceName)
                        {
                            screenStatus = true;
                            break;
                        }
                    }
                    break;
                case DisplayIdentificationMode.screenLayout:
                    //ignoring DeviceName which can change during driver update, windows restart etc..
                    foreach (var item in Screen.AllScreens)
                    {
                        if (item.Bounds == screen.Bounds)
                        {
                            screenStatus = true;
                            break;
                        }
                    }
                    break;
            }
            return screenStatus;
        }

        public static bool ScreenCompare(Screen screen1, LivelyScreen screen2, DisplayIdentificationMode mode)
        {
            bool screenStatus = false;
            switch (mode)
            {
                case DisplayIdentificationMode.screenClass:
                    if (screen1.DeviceName == screen2.DeviceName)
                    {
                        screenStatus = true;
                    }
                    break;
                case DisplayIdentificationMode.screenLayout:
                    //ignoring DeviceName which can change during driver update, windows restart etc..
                    if (screen1.Bounds == screen2.Bounds)
                    {
                        screenStatus = true;
                    }
                    break;
            }
            return screenStatus;
        }

        public static bool ScreenCompare(LivelyScreen screen1, LivelyScreen screen2, DisplayIdentificationMode mode)
        {
            bool screenStatus = false;
            switch (mode)
            {
                case DisplayIdentificationMode.screenClass:
                    if (screen1.DeviceName == screen2.DeviceName)
                    {
                        screenStatus = true;
                    }
                    break;
                case DisplayIdentificationMode.screenLayout:
                    //ignoring DeviceName which can change during driver update, windows restart etc..
                    if (screen1.Bounds == screen2.Bounds)
                    {
                        screenStatus = true;
                    }
                    break;
            }
            return screenStatus;
        }

        public static Screen GetScreenWinform(string DeviceName, Rectangle Bounds, Rectangle WorkingArea, DisplayIdentificationMode mode)
        {
            foreach (var item in GetScreenWinform())
            {
                switch (mode)
                {
                    case DisplayIdentificationMode.screenClass:
                        if (item.DeviceName.Equals(DeviceName))
                        {
                            return item;
                        }
                        break;
                    case DisplayIdentificationMode.screenLayout:
                        //ignoring DeviceName which can change during driver update, windows restart etc..
                        if (item.Bounds == Bounds)
                        {
                            return item;
                        }
                        break;
                }
            }
            return null;
        }

        public static LivelyScreen GetScreen(string DeviceName, Rectangle Bounds, Rectangle WorkingArea, DisplayIdentificationMode mode)
        {
            foreach (var item in GetScreen())
            {
                switch (mode)
                {
                    case DisplayIdentificationMode.screenClass:
                        if (item.DeviceName.Equals(DeviceName))
                        {
                            return item;
                        }
                        break;
                    case DisplayIdentificationMode.screenLayout:
                        //ignoring DeviceName which can change during driver update, windows restart etc..
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
