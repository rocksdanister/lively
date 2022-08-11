using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Threading;
using System.Linq;
using Lively.Common;
using Lively.Services;
using Lively.Common.Helpers.Pinvoke;
using Lively.Helpers.Hardware;
using Lively.Core.Display;
using Lively.Models;

namespace Lively.Core.Suspend
{
    /// <summary>
    /// System monitor logic to pause/unpause wallpaper playback.
    /// </summary>
    public class Playback : IPlayback
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string[] classWhiteList = new string[]
        {
            //startmeu, taskview (win10), action center etc
            "Windows.UI.Core.CoreWindow",
            //alt+tab screen (win10)
            "MultitaskingViewFrame",
            //taskview (win11)
            "XamlExplorerHostIslandWindow",
            //widget window (win11)
            "WindowsDashboard",
            //taskbar(s)
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            //systray notifyicon expanded popup
            "NotifyIconOverflowWindow",
            //rainmeter widgets
            "RainmeterMeterWindow",
            //Coodesker, ref: https://github.com/rocksdanister/lively/issues/760
            "_cls_desk_"
        };
        private IntPtr workerWOrig, progman;
        private PlaybackState _wallpaperPlayback;
        public PlaybackState WallpaperPlayback
        {
            get => _wallpaperPlayback;
            set
            {
                _wallpaperPlayback = value;
                PlaybackStateChanged?.Invoke(this, _wallpaperPlayback);
            }
        }
        public event EventHandler<PlaybackState> PlaybackStateChanged;

        private readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private bool _isLockScreen, _isRemoteSession;
        private bool disposedValue;
        private int livelyPid = 0;

        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;
        private readonly IScreensaverService screenSaver;

        public Playback(IUserSettingsService userSettings, IDesktopCore desktopCore,  IDisplayManager displayManager,IScreensaverService screenSaver)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.screenSaver = screenSaver;

            Initialize();
            desktopCore.WallpaperReset += (s, e) => FindDesktopHandles();
        }

        private void FindDesktopHandles()
        {
            //resetting
            workerWOrig = IntPtr.Zero;
            progman = IntPtr.Zero;

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

        private void Initialize()
        {
            InitializeTimer();
            WallpaperPlayback = PlaybackState.play;

            try
            {
                using Process process = Process.GetCurrentProcess();
                livelyPid = process.Id;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to retrieve Lively Pid:" + e.Message);
            }

            /*
            // Check if already in remote/lockscreen session.
            _isRemoteSession = System.Windows.Forms.SystemInformation.TerminalServerSession;
            if (_isRemoteSession)
            {
                Logger.Info("Remote Desktop Session already started!");
            }
            */
            _isLockScreen = IsSystemLocked();
            if (_isLockScreen)
            {
                Logger.Info("Lockscreen Session already started!");
            }
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void InitializeTimer()
        {
            dispatcherTimer.Tick += new EventHandler(ProcessMonitor);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, userSettings.Settings.ProcessTimerInterval);
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.RemoteConnect)
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

        public void Start()
        {
            dispatcherTimer.Start();
        }

        public void Stop()
        {
            dispatcherTimer.Stop();
        }

        private void ProcessMonitor(object sender, EventArgs e)
        {
            if (screenSaver.IsRunning)
            {
                PlayWallpapers();
                SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal);
            }
            else if (WallpaperPlayback == PlaybackState.paused || _isLockScreen ||
                (_isRemoteSession && userSettings.Settings.RemoteDesktopPause == AppRulesEnum.pause))
            {
                PauseWallpapers();
            }
            else if (userSettings.Settings.BatteryPause == AppRulesEnum.pause &&
                PowerUtil.GetACPowerStatus() == PowerUtil.ACLineStatus.Offline)
            {
                PauseWallpapers();
            }
            else if (userSettings.Settings.PowerSaveModePause == AppRulesEnum.pause &&
                PowerUtil.GetBatterySaverStatus() == PowerUtil.SystemStatusFlag.On)
            {
                PauseWallpapers();
            }
            else
            {
                switch (userSettings.Settings.ProcessMonitorAlgorithm)
                {
                    case ProcessMonitorAlgorithm.foreground:
                        ForegroundAppMonitor();
                        break;
                    case ProcessMonitorAlgorithm.all:
                        ForegroundAppMonitor(); //fallback
                        break;
                    case ProcessMonitorAlgorithm.gamemode:
                        GameModeAppMonitor();
                        break;
                }
            }
        }

        private void GameModeAppMonitor()
        {
            if (NativeMethods.SHQueryUserNotificationState(out NativeMethods.QUERY_USER_NOTIFICATION_STATE state) == 0)
            {
                switch (state)
                {
                    case NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_NOT_PRESENT:
                    case NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY:
                    case NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE:
                    case NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_ACCEPTS_NOTIFICATIONS:
                    case NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME:
                        break;
                    case NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN:
                        PauseWallpapers();
                        return;
                }
            }
            PlayWallpapers();
            SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal);
        }

        private void UIAutomationMonitor()
        {
            throw new NotImplementedException();
            /*
            // track windows open
            Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Subtree, (s, e) =>
            {
                var element = (AutomationElement)s;
                if (element.Current.ProcessId != Process.GetCurrentProcess().Id)
                {
                    Debug.WriteLine("Added window '" + element.Current.Name + "'");
                    // track window close
                    Automation.AddAutomationEventHandler(WindowPattern.WindowClosedEvent, element, TreeScope.Element, (s2, e2) =>
                    {
                        Debug.WriteLine("Removed windows '" + element + "'");
                    });
                }
            });
            */
        }

        private void ForegroundAppMonitor()
        {
            var isDesktop = false;
            var fHandle = NativeMethods.GetForegroundWindow();

            if (IsWhitelistedClass(fHandle))
            {
                PlayWallpapers();
                SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal);
                return;
            }

            try
            {
                NativeMethods.GetWindowThreadProcessId(fHandle, out int processID);
                using Process fProcess = Process.GetProcessById(processID);

                //process with no name, possibly overlay or some other service pgm; resume playback.
                if (string.IsNullOrEmpty(fProcess.ProcessName) || fHandle.Equals(IntPtr.Zero))
                {
                    PlayWallpapers();
                    return;
                }

                //is it Lively or its plugins..
                if (fProcess.Id == livelyPid || IsLivelyPlugin(fProcess.Id))
                {
                    PlayWallpapers();
                    SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal);
                    return;
                }

                //looping through custom rules for user defined apps..
                for (int i = 0; i < userSettings.AppRules.Count; i++)
                {
                    var item = userSettings.AppRules[i];
                    if (string.Equals(item.AppName, fProcess.ProcessName, StringComparison.Ordinal))
                    {
                        switch (item.Rule)
                        {
                            case AppRulesEnum.pause:
                                PauseWallpapers();
                                break;
                            case AppRulesEnum.ignore:
                                PlayWallpapers();
                                SetWallpaperVolume(userSettings.Settings.AudioOnlyOnDesktop ? 0 : userSettings.Settings.AudioVolumeGlobal);
                                break;
                            case AppRulesEnum.kill:
                                //todo
                                break;
                        }
                        return;
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
                    if (!displayManager.IsMultiScreen() ||
                        userSettings.Settings.DisplayPauseSettings == DisplayPauseEnum.all)
                    //userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                    {
                        if (IntPtr.Equals(fHandle, workerWOrig) || IntPtr.Equals(fHandle, progman))
                        {
                            //win10 and win7 desktop foreground while lively is running.
                            isDesktop = true;
                            PlayWallpapers();
                        }
                        else if (NativeMethods.IsZoomed(fHandle) || IsZoomedCustom(fHandle))
                        {
                            //maximised window or window covering whole screen.
                            if (userSettings.Settings.AppFullscreenPause == AppRulesEnum.ignore)
                            {
                                PlayWallpapers();
                            }
                            else
                            {
                                PauseWallpapers();
                            }
                        }
                        else
                        {
                            //window is just in focus, not covering screen.
                            if (userSettings.Settings.AppFocusPause == AppRulesEnum.pause)
                            {
                                PauseWallpapers();
                            }
                            else
                            {
                                PlayWallpapers();
                            }
                        }
                    }
                    else
                    {
                        //multiscreen wp pause algorithm, for per-monitor pause rule.
                        IDisplayMonitor focusedScreen;
                        if ((focusedScreen = MapWindowToMonitor(fHandle)) != null)
                        {
                            //unpausing the rest of wallpapers.
                            //this is a limitation of this algorithm since only one window can be foreground!
                            foreach (var item in displayManager.DisplayMonitors)
                            {
                                if (userSettings.Settings.WallpaperArrangement != WallpaperArrangement.span &&
                                    !focusedScreen.Equals(item))
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
                            isDesktop = true;
                            PlayWallpaper(focusedScreen);
                        }
                        else if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span)
                        {
                            if (IsZoomedSpan(fHandle))
                            {
                                PauseWallpapers();
                            }
                            else //window is not greater >90%
                            {
                                if (userSettings.Settings.AppFocusPause == AppRulesEnum.pause)
                                {
                                    PauseWallpapers();
                                }
                                else
                                {
                                    PlayWallpapers();
                                }
                            }
                        }
                        else if (NativeMethods.IsZoomed(fHandle) || IsZoomedCustom(fHandle))
                        {
                            //maximised window or window covering whole screen.
                            if (userSettings.Settings.AppFullscreenPause == AppRulesEnum.ignore)
                            {
                                PlayWallpaper(focusedScreen);
                            }
                            else
                            {
                                PauseWallpaper(focusedScreen);
                            }
                        }
                        else
                        {
                            //window is just in focus, not covering screen.
                            if (userSettings.Settings.AppFocusPause == AppRulesEnum.pause)
                            {
                                PauseWallpaper(focusedScreen);
                            }
                            else
                            {
                                PlayWallpaper(focusedScreen);
                            }
                        }
                    }

                    if (isDesktop)
                    {
                        SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal);
                    }
                    else
                    {
                        SetWallpaperVolume(userSettings.Settings.AudioOnlyOnDesktop ? 0 : userSettings.Settings.AudioVolumeGlobal);
                    }
                }
            }
            catch { }
        }

        private string GetClassName(IntPtr hwnd)
        {
            const int maxChars = 256;
            StringBuilder className = new StringBuilder(maxChars);
            return NativeMethods.GetClassName((int)hwnd, className, maxChars) > 0 ? className.ToString() : string.Empty;
        }

        private bool IsWhitelistedClass(IntPtr hwnd)
        {
            const int maxChars = 256;
            StringBuilder className = new StringBuilder(maxChars);
            return NativeMethods.GetClassName((int)hwnd, className, maxChars) > 0 && classWhiteList.Any(x => x.Equals(className.ToString(), StringComparison.Ordinal));
        }

        private void PauseWallpapers()
        {
            foreach (var x in desktopCore.Wallpapers)
            {
                x.Pause();
            }
        }

        private void PlayWallpapers()
        {
            foreach (var x in desktopCore.Wallpapers)
            {
                x.Play();
            }
        }

        private void PauseWallpaper(IDisplayMonitor display)
        {
            foreach (var x in desktopCore.Wallpapers)
            {
                if (x.Screen.Equals(display))
                {
                    x.Pause();
                }
            }
        }

        private void PlayWallpaper(IDisplayMonitor display)
        {
            foreach (var x in desktopCore.Wallpapers)
            {
                if (x.Screen.Equals(display))
                {
                    x.Play();
                }
            }
        }

        private void SetWallpaperVolume(int volume)
        {
            foreach (var x in desktopCore.Wallpapers)
            {
                x.SetVolume(volume);
            }
        }

        private void SetWallpaperVolume(int volume, IDisplayMonitor display)
        {
            foreach (var x in desktopCore.Wallpapers)
            {
                if (x.Screen.Equals(display))
                {
                    x.SetVolume(volume);
                }
            }
        }

        private bool IsLivelyPlugin(int pid)
        {
            return desktopCore.Wallpapers.Any(x => x.Proc != null && x.Proc.Id == pid);
        }

        /// <summary>
        /// Checks if hWnd window size is >95% for its running screen.
        /// </summary>
        /// <returns>True if window dimensions are greater.</returns>
        private bool IsZoomedCustom(IntPtr hWnd)
        {
            try
            {
                System.Drawing.Rectangle screenBounds;
                NativeMethods.RECT appBounds;
                NativeMethods.GetWindowRect(hWnd, out appBounds);
                screenBounds = displayManager.GetDisplayMonitorFromHWnd(hWnd).Bounds;
                //If foreground app 95% working-area( -taskbar of monitor)
                if ((appBounds.Bottom - appBounds.Top) >= screenBounds.Height * .95f && (appBounds.Right - appBounds.Left) >= screenBounds.Width * .95f)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Finds out which displaydevice the given application is residing.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private IDisplayMonitor MapWindowToMonitor(IntPtr handle)
        {
            try
            {
                return displayManager.GetDisplayMonitorFromHWnd(handle);
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
            NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT appBounds);
            var screenArea = displayManager.VirtualScreenBounds;
            // If foreground app 95% working-area( - taskbar of monitor)
            return ((appBounds.Bottom - appBounds.Top) >= screenArea.Height * .95f &&
               (appBounds.Right - appBounds.Left) >= screenArea.Width * .95f);
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
                using (Process fProcess = Process.GetProcessById(processID))
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
        private bool IsDesktop()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            return (IntPtr.Equals(hWnd, workerWOrig) || IntPtr.Equals(hWnd, progman));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    dispatcherTimer.Stop();
                    SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Playback()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
