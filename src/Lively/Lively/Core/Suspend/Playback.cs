using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Services;
using Lively.Core.Display;
using Lively.Helpers.Hardware;
using Lively.Models;
using Lively.Models.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace Lively.Core.Suspend
{
    /// <summary>
    /// System monitor logic to pause/unpause wallpaper playback.
    /// </summary>
    public partial class Playback : IPlayback
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private PlaybackPolicy _wallpaperPlayback;
        public PlaybackPolicy WallpaperPlaybackPolicy
        {
            get => _wallpaperPlayback;
            set
            {
                _wallpaperPlayback = value;
                PlaybackPolicyChanged?.Invoke(this, _wallpaperPlayback);
            }
        }

        public event EventHandler<PlaybackPolicy> PlaybackPolicyChanged;
        public event EventHandler<WallpaperControlEventArgs> WallpaperControlChanged;

        private readonly DispatcherTimer dispatcherTimer;
        private bool isLockScreen, isRemoteSession;
        private bool disposedValue;

        private readonly IUserSettingsService userSettings;
        private readonly IDisplayManager displayManager;
        private readonly IScreensaverService screenSaver;

        public Playback(IUserSettingsService userSettings,
            IDisplayManager displayManager,
            IScreensaverService screenSaver)
        {
            this.userSettings = userSettings;
            this.displayManager = displayManager;
            this.screenSaver = screenSaver;

            // We are using timer instead of WinEventHook:
            // EVENT_OBJECT_LOCATIONCHANGE has too much noise even with filtering.
            // Not reliable in some systems.
            dispatcherTimer = new();
            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, userSettings.Settings.ProcessTimerInterval);
            WallpaperPlaybackPolicy = PlaybackPolicy.automatic;
            isLockScreen = IsSystemLocked();
            if (isLockScreen)
                Logger.Info("Lockscreen Session already started!");

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    {
                        isLockScreen = true;
                        Logger.Info("Lockscreen Session started!");
                    }
                    break;
                case SessionSwitchReason.SessionUnlock:
                    {
                        isLockScreen = false;
                        Logger.Info("Lockscreen Session ended!");
                    }
                    break;
                case SessionSwitchReason.RemoteConnect:
                    {
                        isRemoteSession = true;
                        Logger.Info("Remote Desktop Session started!");
                    }
                    break;
                case SessionSwitchReason.RemoteDisconnect:
                    {
                        isRemoteSession = false;
                        Logger.Info("Remote Desktop Session ended!");
                    }
                    break;
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.SessionLogoff:
                case SessionSwitchReason.SessionRemoteControl:
                case SessionSwitchReason.ConsoleConnect:
                case SessionSwitchReason.ConsoleDisconnect:
                default:
                    break;
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

        public IDisposable DeferPlayback()
        {
            return new PlaybackDeferrer(this);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (IsPauseDueToSystemState()) {
                    PauseWallpapers();
                }
                else
                {
                    switch (userSettings.Settings.ProcessMonitorAlgorithm)
                    {
                        case ProcessMonitorAlgorithm.foreground:
                            EvaluatePlaybackByForegroundWindow();
                            break;
                        case ProcessMonitorAlgorithm.all:
                            EvaluatePlaybackByVisibleWindow((display, windows) => WindowUtil.IsDisplayCoveredByAnyWindow(windows, display.WorkingArea));
                            break;
                        case ProcessMonitorAlgorithm.grid:
                            EvaluatePlaybackByVisibleWindow((display, windows) => WindowUtil.IsDisplayCoveredByWindowGrid(windows,
                                display.WorkingArea,
                                userSettings.Settings.ProcessMonitorGridTileSize,
                                userSettings.Settings.ProcessMonitorGridTileCoverageThreshold));
                            break;
                        case ProcessMonitorAlgorithm.gamemode:
                            EvaluatePlaybackByGameMode();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void EvaluatePlaybackByForegroundWindow()
        {
            var isCovered = false;
            var hwnd = NativeMethods.GetForegroundWindow();
            var isValidWindow = WindowUtil.IsVisibleTopLevelWindows(hwnd);
            var foregroundDisplay = displayManager.PrimaryDisplayMonitor;
            var isFullScreenPause = userSettings.Settings.AppFullscreenPause == AppRules.pause;
            var isFocusedAppPause = userSettings.Settings.AppFocusPause == AppRules.pause;

            bool isDesktop;
            if (isValidWindow)
            {
                isDesktop = WindowUtil.IsExcludedDesktopWindowClass(hwnd);
                foregroundDisplay = displayManager.GetDisplayMonitorFromHWnd(hwnd);
                isCovered = WindowUtil.IsDisplayCoveredByWindow(hwnd, foregroundDisplay.WorkingArea);
            }
            else
            {
                // We assume its desktop, not enough information otherwise since only foreground window is checked.
                isDesktop = hwnd == NativeMethods.GetDesktopWindow() || hwnd == NativeMethods.GetShellWindow() || WindowUtil.IsExcludedDesktopWindowClass(hwnd);
            }

            if (IsPauseRuleApp(hwnd))
            {
                PauseWallpapers();
            }
            else if (isDesktop || !isValidWindow)
            {
                // Fallback to playback if desktop, uncertain or user exclusion.
                PlayWallpapers();
                SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal);
            }
            else
            {
                var shouldPause = isFullScreenPause && (isFocusedAppPause || isCovered);
                switch (userSettings.Settings.DisplayPauseSettings)
                {
                    case DisplayPause.perdisplay:
                        {
                            foreach (var display in displayManager.DisplayMonitors)
                            {
                                if (foregroundDisplay.Equals(display))
                                {
                                    if (shouldPause)
                                        PauseWallpaper(foregroundDisplay);
                                    else
                                        PlayWallpaper(foregroundDisplay);
                                }
                                else
                                { 
                                    PlayWallpaper(display); 
                                }
                            }
                        }
                        break;
                    case DisplayPause.all:
                        {
                            if (shouldPause)
                                PauseWallpapers();
                            else
                                PlayWallpapers();
                        }
                        break;
                }

                SetWallpaperVolume(userSettings.Settings.AudioOnlyOnDesktop ? 0 : userSettings.Settings.AudioVolumeGlobal);
            }
        }

        private void EvaluatePlaybackByVisibleWindow(Func<DisplayMonitor, List<IntPtr>, bool> coverageCheck)
        {
            var windows = WindowUtil.GetVisibleTopLevelWindows();
            if (windows.Exists(IsPauseRuleApp))
            {
                PauseWallpapers();
                return;
            }

            var monitorWindowsMap = windows
                .GroupBy(hwnd => displayManager.GetDisplayMonitorFromHWnd(hwnd))
                .ToDictionary(g => g.Key, g => g.ToList());
            var effectiveDisplayPauseSetting = userSettings.Settings.WallpaperArrangement switch
            {
                WallpaperArrangement.duplicate => DisplayPause.all,
                _ => userSettings.Settings.DisplayPauseSettings,
            };

            switch (effectiveDisplayPauseSetting)
            {
                case DisplayPause.perdisplay:
                    {
                        switch (userSettings.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                            case WallpaperArrangement.duplicate:
                                {
                                    foreach (var display in displayManager.DisplayMonitors)
                                    {
                                        var windowsOnDisplay = monitorWindowsMap.GetValueOrDefault(display) ?? [];
                                        var shouldPause = ShouldPauseWallpaper(display, windowsOnDisplay, coverageCheck);

                                        if (shouldPause)
                                            PauseWallpaper(display);
                                        else
                                            PlayWallpaper(display);
                                    }
                                }
                                break;
                            case WallpaperArrangement.span:
                                {
                                    var shouldPause = displayManager.DisplayMonitors.All(display =>
                                    {
                                        var windowsOnDisplay = monitorWindowsMap.GetValueOrDefault(display) ?? [];
                                        return ShouldPauseWallpaper(display, windowsOnDisplay, coverageCheck);
                                    });

                                    if (shouldPause)
                                        PauseWallpapers();
                                    else
                                        PlayWallpapers();
                                }
                                break;
                        }
                    }
                    break;
                case DisplayPause.all:
                    {
                        var shouldPauseAll = displayManager.DisplayMonitors.Any(display =>
                        {
                            var windowsOnDisplay = monitorWindowsMap.GetValueOrDefault(display) ?? [];
                            return ShouldPauseWallpaper(display, windowsOnDisplay, coverageCheck);
                        });

                        if (shouldPauseAll)
                            PauseWallpapers();
                        else
                            PlayWallpapers();
                    }
                    break;
            }

            // Audio playback impl.
            var effectiveDisplayAudioOutput = userSettings.Settings.WallpaperArrangement switch
            {
                WallpaperArrangement.per => userSettings.Settings.DisplayAudioOutput,
                _ => DisplayAudioMode.all,
            };
            var effectiveSelectedAudioOutputDisplay = 
                displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(userSettings.Settings.SelectedAudioOutputDisplay))
                ?? displayManager.PrimaryDisplayMonitor;
            foreach (var display in displayManager.DisplayMonitors)
            {
                var windowsOnDisplay = monitorWindowsMap.GetValueOrDefault(display) ?? [];
                var isDesktop = windowsOnDisplay.Count == 0;
                switch (effectiveDisplayAudioOutput)
                {
                    case DisplayAudioMode.selection:
                        {
                            if (!displayManager.IsMultiScreen() || display.Equals(effectiveSelectedAudioOutputDisplay))
                            {
                                if (isDesktop)
                                    SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal, display);
                                else
                                    SetWallpaperVolume(userSettings.Settings.AudioOnlyOnDesktop ? 0 : userSettings.Settings.AudioVolumeGlobal, display);
                            }
                            else {
                                SetWallpaperVolume(0, display);
                            }
                        }
                        break;
                    case DisplayAudioMode.all:
                        {
                            if (isDesktop)
                                SetWallpaperVolume(userSettings.Settings.AudioVolumeGlobal, display);
                            else
                                SetWallpaperVolume(userSettings.Settings.AudioOnlyOnDesktop ? 0 : userSettings.Settings.AudioVolumeGlobal, display);
                        }
                        break;
                }
            }
        }

        private void EvaluatePlaybackByGameMode()
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

        private bool ShouldPauseWallpaper(DisplayMonitor display, List<IntPtr> windowsOnDisplay, Func<DisplayMonitor, List<IntPtr>, bool> coverageCheck)
        {
            var isFullScreenPause = userSettings.Settings.AppFullscreenPause == AppRules.pause;
            var isFocusedAppPause = userSettings.Settings.AppFocusPause == AppRules.pause;
            var isDesktop = windowsOnDisplay.Count == 0;
            var isCovered = coverageCheck(display, windowsOnDisplay);

            // IsFullScreenPause = false, always play wallpaper
            // IsFocusedAppPause = true, only play on desktop.
            return isFullScreenPause && ((isFocusedAppPause && !isDesktop) || isCovered);
        }

        private void PauseWallpapers()
        {
            WallpaperControlChanged?.Invoke(this, new WallpaperControlEventArgs(WallpaperControlAction.Pause));
            MemoryUtil.OptimizeMemory();
        }

        private void PlayWallpapers()
        {
            WallpaperControlChanged?.Invoke(this, new WallpaperControlEventArgs(WallpaperControlAction.Play));
        }

        private void PauseWallpaper(DisplayMonitor display)
        {
            WallpaperControlChanged?.Invoke(this, new WallpaperControlEventArgs(WallpaperControlAction.Pause, display));
            MemoryUtil.OptimizeMemory();
        }

        private void PlayWallpaper(DisplayMonitor display)
        {
            WallpaperControlChanged?.Invoke(this, new WallpaperControlEventArgs(WallpaperControlAction.Play, display));
        }

        private void SetWallpaperVolume(int volume)
        {
            WallpaperControlChanged?.Invoke(this, new WallpaperControlEventArgs(WallpaperControlAction.SetVolume, null, volume));
        }

        private void SetWallpaperVolume(int volume, DisplayMonitor display)
        {
            WallpaperControlChanged?.Invoke(this, new WallpaperControlEventArgs(WallpaperControlAction.SetVolume, display, volume));
        }

        private bool IsPauseDueToSystemState()
        {
            if (screenSaver.IsRunning)
            {
                // Pause running wallpaper if screensaver is using a separate instance of wallpaper.
                return screenSaver.Mode == ScreensaverApplyMode.process;
            }
            else if (WallpaperPlaybackPolicy == PlaybackPolicy.alwaysPaused || isLockScreen ||
                (isRemoteSession && userSettings.Settings.RemoteDesktopPause == AppRules.pause))
            {
                return true;
            }
            else if (userSettings.Settings.BatteryPause == AppRules.pause &&
                PowerUtil.GetACPowerStatus() == PowerUtil.ACLineStatus.Offline)
            {
                return true;
            }
            else if (userSettings.Settings.PowerSaveModePause == AppRules.pause &&
                PowerUtil.GetBatterySaverStatus() == PowerUtil.SystemStatusFlag.On)
            {
                return true;
            }
            return false;
        }

        private bool IsPauseRuleApp(IntPtr hwnd)
        {
            var result = false;
            try
            {
                if (userSettings.AppRules.Count == 0 || NativeMethods.GetWindowThreadProcessId(hwnd, out int pid) == 0)
                    return result;

                using Process process = Process.GetProcessById(pid);
                for (int i = 0; i < userSettings.AppRules.Count; i++)
                {
                    var appRule = userSettings.AppRules[i];
                    if (string.Equals(appRule.AppName, process.ProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        result = appRule.Rule switch
                        {
                            AppRules.pause => true,
                            // Unsupported
                            AppRules.ignore or AppRules.kill => false,
                            _ => false,
                        };
                    }
                }
            }
            catch { }
            return result;
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
