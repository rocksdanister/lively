using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace livelywpf.Core
{
    public static class Playback
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        readonly static String[] windowsClassDefaults = new string[]
        {
            //startmeu, taskview, action center etc
            "Windows.UI.Core.CoreWindow",
            // alt+tab screen (win10)
            "MultitaskingViewFrame",
            //taskbar
            "Shell_TrayWnd",
            //rainmeter widgets (?)
            "RainmeterMeterWindow"
        };
        static IntPtr workerWOrig, progman;

        public static void Initialize()
        {
            progman = NativeMethods.FindWindow("Progman", null);
            var folderView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (folderView == IntPtr.Zero)
            {
                //If the desktop isn't under Progman, cycle through the WorkerW handles and find the correct one
                do
                {
                    workerWOrig = NativeMethods.FindWindowEx(NativeMethods.GetDesktopWindow(), workerWOrig, "WorkerW", null);
                    folderView = NativeMethods.FindWindowEx(workerWOrig, IntPtr.Zero, "SHELLDLL_DefView", null);
                } while (folderView == IntPtr.Zero && workerWOrig != IntPtr.Zero);
            }
        }

        private static void ForegroundAppMonitor()
        {
            const int maxChars = 256;
            StringBuilder className = new StringBuilder(maxChars);
            var fHandle = NativeMethods.GetForegroundWindow();
            Process fProcess = null;
            if (NativeMethods.GetClassName((int)fHandle, className, maxChars) > 0)
            {
                string cName = className.ToString();
                foreach (var item in windowsClassDefaults)
                {
                    if (String.Equals(item, cName, StringComparison.OrdinalIgnoreCase))
                    {
                        PlayWallpapers();
                    }
                }
            }

            try
            {
                NativeMethods.GetWindowThreadProcessId(fHandle, out int processID);
                fProcess = Process.GetProcessById(processID);
            }
            catch
            {
                Logger.Info("(Foreground) Getting processname failure, skipping!");
                //ignore - admin process etc
                PlayWallpapers();
                return;
            }

            if (String.IsNullOrEmpty(fProcess.ProcessName) || fHandle.Equals(IntPtr.Zero))
            {
                Debug.WriteLine("getting processname failure/handle null, skipping!");
                PlayWallpapers();
                return;
            }

            if (fProcess.ProcessName.Equals("livelywpf", StringComparison.OrdinalIgnoreCase) ||  fProcess.ProcessName.Equals("livelycefsharp", StringComparison.OrdinalIgnoreCase)) 
            {
                PlayWallpapers();
            }

            try
            {
                //looping through custom rules for user defined apps.
                foreach (var item in Program.AppRulesVM.AppRules)
                {
                    if (String.Equals(item.AppName, fProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (item.Rule == AppRulesEnum.ignore)
                        {
                            PlayWallpapers();
                            return;
                        }
                        else if (item.Rule == AppRulesEnum.pause)
                        {
                            PauseWallpapers();
                            return;
                        }
                    }
                }

                if (!(fHandle.Equals(NativeMethods.GetDesktopWindow()) || fHandle.Equals(NativeMethods.GetShellWindow())))
                {
                    if (Program.SettingsVM.Settings.DisplayPauseSettings == DisplayPauseEnum.all) //to-do: if multiscreen true
                    {
                        //win10 and win7 desktop foreground while lively is running.
                        if (IntPtr.Equals(fHandle, workerWOrig) || IntPtr.Equals(fHandle, progman))
                        {
                            PlayWallpapers();
                        }
                        //maximised window or window covering whole screen.
                        else if (NativeMethods.IsZoomed(fHandle) || IsZoomedCustom(fHandle))
                        {
                            PauseWallpapers();
                        }
                        //window is just in focus, not covering screen.
                        else
                        {
                            PlayWallpapers();
                        }
                    }
                }
            }
            catch { }
        }

        private static void PauseWallpapers()
        {
            SetupDesktop.Wallpapers.ForEach(x => 
            {
                x.Pause();
            });
        }

        private static void PlayWallpapers()
        {
            SetupDesktop.Wallpapers.ForEach(x =>
            {
                x.Play();
            });
        }

        /// <summary>
        /// Checks if hWnd window size is >95% for its running screen.
        /// </summary>
        /// <returns>True if window dimensions are greater.</returns>
        private static bool IsZoomedCustom(IntPtr hWnd)
        {
            try
            {
                System.Drawing.Rectangle screenBounds;
                NativeMethods.RECT appBounds;
                NativeMethods.GetWindowRect(hWnd, out appBounds);
                screenBounds = System.Windows.Forms.Screen.FromHandle(hWnd).Bounds;
                //If foreground app 95% working-area( -taskbar of monitor)
                if ((appBounds.Bottom - appBounds.Top) >= screenBounds.Height * .95f && (appBounds.Right - appBounds.Left) >= screenBounds.Width * .95f) 
                    return true;
                else
                    return false;
            }
            catch { 
                return false; 
            }
        }

        /// <summary>
        /// Is foreground live-wallpaper desktop.
        /// </summary>
        /// <returns></returns>
        public static bool IsDesktop()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            if (IntPtr.Equals(hWnd, workerWOrig))
            {
                return true;
            }
            else if (IntPtr.Equals(hWnd, progman))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
