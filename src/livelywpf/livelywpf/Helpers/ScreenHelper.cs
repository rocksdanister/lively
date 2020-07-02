using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace livelywpf
{
    public static class ScreenHelper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool IsMultiScreen()
        {
            return Screen.AllScreens.Count() > 1;
        }

        public static int ScreenCount()
        {
            return Screen.AllScreens.Count();
        }

        public static Screen[] GetScreen()
        {
            return Screen.AllScreens;
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
                //ignoring DeviceName which can change during driver update, windows restart etc..
                case DisplayIdentificationMode.screenLayout:
                    foreach (var item in Screen.AllScreens)
                    {
                        if(item.WorkingArea == screen.WorkingArea && item.Bounds == screen.Bounds)
                        {
                            screenStatus = true;
                            break;
                        }
                    }
                    break;
            }
            return screenStatus;
        }

        public static bool ScreenCompare(Screen screen1, Screen screen2, DisplayIdentificationMode mode)
        {
            bool screenStatus = false;
            switch (mode)
            {
                case DisplayIdentificationMode.screenClass:
                    if (screen1 == screen2)
                    {
                        screenStatus = true;
                    }
                    break;
                //ignoring DeviceName which can change during driver update, windows restart etc..
                case DisplayIdentificationMode.screenLayout:
                    if (screen1.WorkingArea == screen2.WorkingArea && screen1.Bounds == screen2.Bounds)
                    {
                        screenStatus = true;
                    }
                    break;
            }
            return screenStatus;
        }
    }
}
