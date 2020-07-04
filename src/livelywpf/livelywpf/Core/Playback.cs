using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;

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
        private static PlaybackState wallpaperPlaybackState = PlaybackState.play;
        private static DispatcherTimer dispatcherTimer = new DispatcherTimer();

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
            InitializeTimer();
            dispatcherTimer.Start();
        }

        private static void InitializeTimer()
        {
            dispatcherTimer.Tick += new EventHandler(ProcessMonitor);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, Program.SettingsVM.Settings.ProcessTimerInterval);
        }

        private static void ProcessMonitor(object sender, EventArgs e)
        {
            if (wallpaperPlaybackState == PlaybackState.paused)
            {
                PauseWallpapers();
                return;
            }

            if (Program.SettingsVM.Settings.ProcessMonitorAlgorithm == ProcessMonitorAlgorithm.foreground)
            {
                ForegroundAppMonitor();
            }
            else
            {
                throw new NotImplementedException();
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
                        return;
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
                return;
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
                    if (!ScreenHelper.IsMultiScreen() || Program.SettingsVM.Settings.DisplayPauseSettings == DisplayPauseEnum.all)
                    {
                        if (IntPtr.Equals(fHandle, workerWOrig) || IntPtr.Equals(fHandle, progman))
                        {
                            //win10 and win7 desktop foreground while lively is running.
                            PlayWallpapers();
                        }
                        else if (NativeMethods.IsZoomed(fHandle) || IsZoomedCustom(fHandle))
                        {
                            //maximised window or window covering whole screen.
                            if (Program.SettingsVM.Settings.AppFullscreenPause == AppRulesEnum.ignore)
                                PlayWallpapers();
                            else
                                PauseWallpapers();
                        }
                        else
                        {
                            //window is just in focus, not covering screen.
                            if (Program.SettingsVM.Settings.AppFocusPause == AppRulesEnum.pause)
                                PauseWallpapers();
                            else
                                PlayWallpapers();
                        }
                    }
                    else 
                    {
                        //multiscreen wp pause algorithm, for per-monitor pause rule.
                        Screen focusedScreen;
                        if ((focusedScreen = MapWindowToMonitor(fHandle)) != null)
                        {
                            //unpausing the rest of wallpapers.
                            //this is a limitation of this algorithm since only one window can be foreground!
                            foreach (var item in ScreenHelper.GetScreen())
                            {
                                if (item != focusedScreen)
                                    PlayWallpaper(item);
                            }
                        }
                        else
                        {
                            //no display connected?
                            return;
                        }

                        if (IntPtr.Equals(fHandle, workerWOrig) || IntPtr.Equals(fHandle, progman))
                        {
                            //win10 and win7 desktop foreground while lively is running.
                            PlayWallpaper(focusedScreen);
                        }
                        else if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                        {
                            if (IsZoomedSpan(fHandle))
                            {
                                PauseWallpaper(Screen.PrimaryScreen);
                            }
                            else //window is not greater >90%
                            {
                                if (Program.SettingsVM.Settings.AppFocusPause == AppRulesEnum.pause)
                                {
                                    PauseWallpaper(Screen.PrimaryScreen);
                                }
                                else
                                {
                                    PlayWallpaper(Screen.PrimaryScreen);
                                }
                            }
                        }
                        else if (NativeMethods.IsZoomed(fHandle) || IsZoomedCustom(fHandle))
                        {
                            //maximised window or window covering whole screen.
                            if (Program.SettingsVM.Settings.AppFullscreenPause == AppRulesEnum.ignore)
                                PlayWallpaper(focusedScreen);
                            else
                                PauseWallpaper(focusedScreen);
                        }
                        else
                        {
                            //window is just in focus, not covering screen.
                            if (Program.SettingsVM.Settings.AppFocusPause == AppRulesEnum.pause)
                                PauseWallpaper(focusedScreen);
                            else
                                PlayWallpaper(focusedScreen);
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

        private static void PauseWallpaper(Screen display)
        {
            SetupDesktop.Wallpapers.ForEach(x =>
            {
                if(x.GetScreen() == display)
                    x.Pause();
            });
        }

        private static void PlayWallpaper(Screen display)
        {
            SetupDesktop.Wallpapers.ForEach(x =>
            {
                if (x.GetScreen() == display)
                    x.Pause();
            });
        }

        /// <summary>
        /// Checks if hWnd window size is >95% for its running screen.
        /// </summary>
        /// <returns>True if window dimensions are greater.</returns>
        private static bool IsZoomedCustom(IntPtr hWnd)
        {
            /*
            try
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out processID);
                currProcess = Process.GetProcessById(processID);
            }
            catch
            {

                Debug.WriteLine("getting processname failure, skipping isZoomedCustom()");
                //ignore, admin process etc
                return false;
            }
            */

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
        /// Finds out which displaydevice the given application is residing.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static Screen MapWindowToMonitor(IntPtr handle)
        {
            try
            {
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                return screen;
            }
            catch
            {
                //what if there is no display connected? idk.
                return null;
            }
        }

        /// <summary>
        /// Checks if the hWnd dimension is spanned across all displays.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        private static bool IsZoomedSpan(IntPtr hWnd)
        {
            NativeMethods.RECT appBounds;
            NativeMethods.GetWindowRect(hWnd, out appBounds);
            // If foreground app 95% working-area( - taskbar of monitor)
            if ((appBounds.Bottom - appBounds.Top) >= SystemInformation.VirtualScreen.Height * .95f &&
                (appBounds.Right - appBounds.Left) >= SystemInformation.VirtualScreen.Width * .95f) 
                return true;
            else
                return false;
        }

        /// <summary>
        /// Sets a wallpaper temporary play/pause behavior that does not persist when application restart.
        /// </summary>
        /// <param name="state"></param>
        public static void SetWallpaperPlaybackState(PlaybackState state)
        {
            wallpaperPlaybackState = state;
        }

        public static PlaybackState GetWallpaperPlaybackState()
        {
            return wallpaperPlaybackState;
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
