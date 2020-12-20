using livelywpf.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Threading;

namespace livelywpf.Core
{
    /// <summary>
    /// System monitor logic to pause/unpause wallpaper playback.
    /// </summary>
    public class Playback
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        readonly string[] windowsClassDefaults = new string[]
        {
            //startmeu, taskview, action center etc
            "Windows.UI.Core.CoreWindow",
            // alt+tab screen (win10)
            "MultitaskingViewFrame",
            //taskbar
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            //systray notifyicon expanded popup
            "NotifyIconOverflowWindow",
            //rainmeter widgets
            "RainmeterMeterWindow"
        };
        private static IntPtr workerWOrig, progman;
        public static PlaybackState PlaybackState { get; set; }
        //public event EventHandler<PlaybackState> PlaybackStateChanged;
        private readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private bool _isLockScreen, _isRemoteSession;

        public Playback()
        {
            Initialize();
        }

        public void Start()
        {
            // Check if already in remote/lockscreen session.
            _isRemoteSession = System.Windows.Forms.SystemInformation.TerminalServerSession;
            if (_isRemoteSession)
            {
                Logger.Info("Remote Desktop Session already started!");
            }
            _isLockScreen = IsSystemLocked();
            if (_isLockScreen)
            {
                Logger.Info("Lockscreen Session already started!");
            }
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            dispatcherTimer.Start();
        }

        public void Stop()
        {
            dispatcherTimer.Stop();
            _isLockScreen = _isRemoteSession = false;
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
        }

        private void Initialize()
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
            PlaybackState = PlaybackState.play;
        }

        private void InitializeTimer()
        {
            dispatcherTimer.Tick += new EventHandler(ProcessMonitor);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, Program.SettingsVM.Settings.ProcessTimerInterval);
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.RemoteConnect )
            {
                _isRemoteSession = true;
                Logger.Info("Remote Desktop Session started!");
            }
            else if (e.Reason == SessionSwitchReason.RemoteDisconnect)
            {
                _isRemoteSession = false;
                Logger.Info("Remote Desktop Session ended!");
            }
            else if (e.Reason == SessionSwitchReason.SessionLock)
            {
                _isLockScreen = true;
                Logger.Info("Lockscreen Session started!");
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                _isLockScreen = false;
                Logger.Info("Lockscreen Session ended!");
            }
        }

        private void ProcessMonitor(object sender, EventArgs e)
        {
            if (PlaybackState == PlaybackState.paused)
            {
                PauseWallpapers();
                return;
            }
            else if (_isRemoteSession || _isLockScreen)
            {
                PauseWallpapers();
                return;
            }
            else if (Program.SettingsVM.Settings.BatteryPause == AppRulesEnum.pause)
            {
                if (System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline)
                {
                    PauseWallpapers();
                    return;
                }
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

        private void ForegroundAppMonitor()
        {
            const int maxChars = 256;
            StringBuilder className = new StringBuilder(maxChars);
            var fHandle = NativeMethods.GetForegroundWindow();
            //Process fProcess;
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
                using (Process fProcess = Process.GetProcessById(processID))
                {
                    if (String.IsNullOrEmpty(fProcess.ProcessName) || fHandle.Equals(IntPtr.Zero))
                    {
                        //process with no name, possibly overlay or some other service pgm; resume playback.
                        PlayWallpapers();
                        return;
                    }

                    if (fProcess.ProcessName.Equals("livelywpf", StringComparison.OrdinalIgnoreCase) ||
                        fProcess.ProcessName.Equals("livelycefsharp", StringComparison.OrdinalIgnoreCase) ||
                        fProcess.ProcessName.Equals("libvlcplayer", StringComparison.OrdinalIgnoreCase) ||
                        fProcess.ProcessName.Equals("libmpvplayer", StringComparison.OrdinalIgnoreCase))
                    {
                        PlayWallpapers();
                        SetWallpaperVolume(Program.SettingsVM.Settings.AudioVolumeGlobal);
                        return;
                    }

                    //looping through custom rules for user defined apps.
                    for (int i = 0; i < Program.AppRulesVM.AppRules.Count; i++)
                    {
                        var item = Program.AppRulesVM.AppRules[i];
                        if (String.Equals(item.AppName, fProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (item.Rule == AppRulesEnum.ignore)
                            {
                                PlayWallpapers();
                                SetWallpaperVolume(Program.SettingsVM.Settings.AudioVolumeGlobal);
                                return;
                            }
                            else if (item.Rule == AppRulesEnum.pause)
                            {
                                PauseWallpapers();
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                //failed to get process info.. maybe remote process; resume playback.
                PlayWallpapers();
                return;
            }

            try
            {

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
                        LivelyScreen focusedScreen;
                        if ((focusedScreen = MapWindowToMonitor(fHandle)) != null)
                        {
                            //unpausing the rest of wallpapers.
                            //this is a limitation of this algorithm since only one window can be foreground!
                            foreach (var item in ScreenHelper.GetScreen())
                            {
                                if (!ScreenHelper.ScreenCompare(item, focusedScreen, DisplayIdentificationMode.screenLayout))
                                {
                                    PlayWallpaper(item);
                                    //SetWallpaperVoume(0, item);
                                }
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
                            SetWallpaperVolume(Program.SettingsVM.Settings.AudioVolumeGlobal, focusedScreen);
                        }
                        else if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                        {
                            if (IsZoomedSpan(fHandle))
                            {
                                PauseWallpaper(ScreenHelper.GetPrimaryScreen());
                            }
                            else //window is not greater >90%
                            {
                                if (Program.SettingsVM.Settings.AppFocusPause == AppRulesEnum.pause)
                                {
                                    PauseWallpaper(ScreenHelper.GetPrimaryScreen());
                                }
                                else
                                {
                                    PlayWallpaper(ScreenHelper.GetPrimaryScreen());
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
                    SetWallpaperVolume(Program.SettingsVM.Settings.AudioVolumeGlobal);
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

        private static void PauseWallpaper(LivelyScreen display)
        {
            SetupDesktop.Wallpapers.ForEach(x =>
            {
                if(ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                    x.Pause();
            });
        }

        private static void PlayWallpaper(LivelyScreen display)
        {
            SetupDesktop.Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                    x.Play();
            });
        }

        private static void SetWallpaperVolume(int volume)
        {
            SetupDesktop.Wallpapers.ForEach(x =>
            {
                x.SetVolume(volume);
            });
        }

        private static void SetWallpaperVolume(int volume, LivelyScreen display)
        {
            SetupDesktop.Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                {
                    x.SetVolume(volume);
                }
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
        /// Finds out which displaydevice the given application is residing.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private LivelyScreen MapWindowToMonitor(IntPtr handle)
        {
            try
            {
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                return new LivelyScreen(screen);
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
        private bool IsZoomedSpan(IntPtr hWnd)
        {
            NativeMethods.RECT appBounds;
            NativeMethods.GetWindowRect(hWnd, out appBounds);
            // If foreground app 95% working-area( - taskbar of monitor)
            if ((appBounds.Bottom - appBounds.Top) >= System.Windows.Forms.SystemInformation.VirtualScreen.Height * .95f &&
               (appBounds.Right - appBounds.Left) >= System.Windows.Forms.SystemInformation.VirtualScreen.Width * .95f)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Checks if LockApp is foreground program.
        /// <para>Could not find a better way to do this quickly,
        /// Lockscreen class is "Windows.UI.Core.CoreWindow" which is used by other windows UI elements.</para>
        /// This should be enough for just checking before subscribing to the Lock/Unlocked windows event.
        /// </summary>
        /// <returns>True if lockscreen is active.</returns>
        private bool IsSystemLocked()
        {
            bool result = false;
            var fHandle = NativeMethods.GetForegroundWindow();
            try
            {
                NativeMethods.GetWindowThreadProcessId(fHandle, out int processID);
                using(Process fProcess = Process.GetProcessById(processID))
                {
                    result = fProcess.ProcessName.Equals("LockApp", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Is foreground live-wallpaper desktop.
        /// </summary>
        /// <returns></returns>
        public static bool IsDesktop()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            return (IntPtr.Equals(hWnd, workerWOrig) || IntPtr.Equals(hWnd, progman));
        }
    }
}
