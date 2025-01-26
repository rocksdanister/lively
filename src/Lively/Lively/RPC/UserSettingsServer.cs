using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Common.Services;
using Lively.Core.Display;
using Lively.Grpc.Common.Proto.Settings;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Lively.RPC
{
    internal class UserSettingsServer : SettingsService.SettingsServiceBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IDisplayManager displayManager;
        private readonly ITransparentTbService ttbService;
        private readonly IUserSettingsService userSettings;
        private readonly IRunnerService runner;
        private readonly IResourceService i18n;
        private readonly ISystray sysTray;

        private readonly object appRulesWriteLock = new object();
        private readonly object settingsWriteLock = new object();

        public UserSettingsServer(IDisplayManager displayManager,
            IUserSettingsService userSettings,
            IRunnerService runner,
            ISystray sysTray,
            IResourceService i18n,
            ITransparentTbService ttbService)
        {
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.ttbService = ttbService;
            this.sysTray = sysTray;
            this.runner = runner;
            this.i18n = i18n;
        }

        public override Task<AppRulesSettings> GetAppRulesSettings(Empty _, ServerCallContext context)
        {
            var resp = new AppRulesSettings();
            foreach (var app in userSettings.AppRules)
            {
                resp.AppRules.Add(new AppRulesDataModel
                {
                    AppName = app.AppName,
                    Rule = (Grpc.Common.Proto.Settings.AppRules)((int)app.Rule)
                });
            }
            return Task.FromResult(resp);
        }

        public override Task<Empty> SetAppRulesSettings(AppRulesSettings req, ServerCallContext context)
        {
            userSettings.AppRules.Clear();
            foreach (var item in req.AppRules)
            {
                userSettings.AppRules.Add(new ApplicationRulesModel(item.AppName, (Models.Enums.AppRules)(int)item.Rule));
            }

            try
            {
                return Task.FromResult(new Empty());
            }
            finally
            {
                lock (appRulesWriteLock)
                {
                    userSettings.Save<List<ApplicationRulesModel>>();
                }
            }
        }

        public override Task<Empty> SetSettings(SettingsDataModel req, ServerCallContext context)
        {
            bool restartRequired = (Models.Enums.AppTheme)req.ApplicationTheme != userSettings.Settings.ApplicationTheme;// || req.Language != userSettings.Settings.Language;
            if (req.Startup != userSettings.Settings.Startup)
            {
                userSettings.Settings.Startup = req.Startup;
                try
                {
                    _ = WindowsStartup.SetStartup(userSettings.Settings.Startup);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            if (req.Language != userSettings.Settings.Language)
            {
                userSettings.Settings.Language = req.Language;
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    i18n.SetCulture(userSettings.Settings.Language);
                }));
            }

            if (req.SysTrayIcon != userSettings.Settings.SysTrayIcon)
            {
                userSettings.Settings.SysTrayIcon = req.SysTrayIcon;
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    sysTray.Visibility(userSettings.Settings.SysTrayIcon);
                }));
            }

            if ((Models.Enums.TaskbarTheme)req.SystemTaskbarTheme != userSettings.Settings.SystemTaskbarTheme)
            {
                userSettings.Settings.SystemTaskbarTheme = (Models.Enums.TaskbarTheme)req.SystemTaskbarTheme;
                ttbService.Start(userSettings.Settings.SystemTaskbarTheme);
            }

            if ((Models.Enums.AppTheme)req.ApplicationTheme != userSettings.Settings.ApplicationTheme)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    App.ChangeTheme((Models.Enums.AppTheme)req.ApplicationTheme);
                }));
            }

            userSettings.Settings.SavedURL = req.SavedUrl;
            userSettings.Settings.ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)((int)req.ProcessMonitorAlogorithm);
            userSettings.Settings.SelectedDisplay = displayManager.DisplayMonitors.FirstOrDefault(x => req.SelectedDisplay.DeviceId == x.DeviceId) ?? displayManager.PrimaryDisplayMonitor;
            userSettings.Settings.WallpaperArrangement = (WallpaperArrangement)((int)req.WallpaperArrangement);
            userSettings.Settings.AppVersion = req.AppVersion;
            userSettings.Settings.AppPreviousVersion = req.AppPreviousVersion;
            userSettings.Settings.ScreensaverType = (ScreensaverType)req.ScreensaverType;
            userSettings.Settings.ScreensaverArragement = (WallpaperArrangement)req.ScreensaverArrangement;
            //userSettings.Settings.Startup = req.Startup;
            userSettings.Settings.IsFirstRun = req.IsFirstRun;
            userSettings.Settings.ControlPanelOpened = req.ControlPanelOpened;
            userSettings.Settings.AppFocusPause = (Models.Enums.AppRules)((int)req.AppFocusPause);
            userSettings.Settings.AppFullscreenPause = (Models.Enums.AppRules)((int)req.AppFullscreenPause);
            userSettings.Settings.BatteryPause = (Models.Enums.AppRules)((int)req.BatteryPause);
            userSettings.Settings.VideoPlayer = (LivelyMediaPlayer)((int)req.VideoPlayer);
            userSettings.Settings.VideoPlayerHwAccel = req.VideoPlayerHwAccel;
            userSettings.Settings.WebBrowser = (LivelyWebBrowser)((int)req.WebBrowser);
            userSettings.Settings.GifPlayer = (LivelyGifPlayer)((int)req.GifPlayer);
            userSettings.Settings.PicturePlayer = (LivelyPicturePlayer)((int)req.PicturePlayer);
            userSettings.Settings.WallpaperWaitTime = req.WallpaperWaitTime;
            userSettings.Settings.ProcessTimerInterval = req.ProcessTimerInterval;
            userSettings.Settings.StreamQuality = (Models.Enums.StreamQualitySuggestion)((int)req.StreamQuality);
            userSettings.Settings.LivelyZipGenerate = req.LivelyZipGenerate;
            userSettings.Settings.ScalerVideo = (WallpaperScaler)((int)req.ScalerVideo);
            userSettings.Settings.ScalerGif = (WallpaperScaler)((int)req.ScalerGif);
            userSettings.Settings.GifCapture = req.GifCapture;
            userSettings.Settings.MultiFileAutoImport = req.MultiFileAutoImport;
            userSettings.Settings.SafeShutdown = req.SafeShutdown;
            userSettings.Settings.IsRestart = req.IsRestart;
            userSettings.Settings.InputForward = (Models.Enums.InputForwardMode)req.InputForward;
            userSettings.Settings.MouseInputMovAlways = req.MouseInputMovAlways;
            userSettings.Settings.TileSize = req.TileSize;
            userSettings.Settings.UIMode = (LivelyGUIState)((int)req.LivelyGuiRendering);
            userSettings.Settings.WallpaperDir = req.WallpaperDir;
            userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir = req.WallpaperDirMoveExistingWallpaperNewDir;
            //userSettings.Settings.SysTrayIcon = req.SysTrayIcon;
            userSettings.Settings.WebDebugPort = req.WebDebugPort;
            userSettings.Settings.AutoDetectOnlineStreams = req.AutoDetectOnlineStreams;
            userSettings.Settings.ExtractStreamMetaData = req.ExtractStreamMetaData;
            userSettings.Settings.WallpaperBundleVersion = req.WallpaperBundleVersion;
            userSettings.Settings.AudioVolumeGlobal = req.AudioVolumeGlobal;
            userSettings.Settings.AudioOnlyOnDesktop = req.AudioOnlyOnDesktop;
            userSettings.Settings.WallpaperScaling = (WallpaperScaler)req.WallpaperScaling;
            userSettings.Settings.CefDiskCache = req.CefDiskCache;
            userSettings.Settings.DebugMenu = req.DebugMenu;
            userSettings.Settings.TestBuild = req.TestBuild;
            userSettings.Settings.ApplicationTheme = (Models.Enums.AppTheme)req.ApplicationTheme;
            userSettings.Settings.RemoteDesktopPause = (Models.Enums.AppRules)req.RemoteDesktopPause;
            userSettings.Settings.PowerSaveModePause = (Models.Enums.AppRules)req.PowerSaveModePause;
            userSettings.Settings.LockScreenAutoWallpaper = req.LockScreenAutoWallpaper;
            userSettings.Settings.DesktopAutoWallpaper = req.DesktopAutoWallpaper;
            //userSettings.Settings.SystemTaskbarTheme = (Common.TaskbarTheme)req.SystemTaskbarTheme;
            userSettings.Settings.ScreensaverIdleDelay = (Models.Enums.ScreensaverIdleTime)((int)req.ScreensaverIdleWait);
            userSettings.Settings.ScreensaverOledWarning = req.ScreensaverOledWarning;
            userSettings.Settings.ScreensaverEmptyScreenShowBlack = req.ScreensaverEmptyScreenShowBlack;
            userSettings.Settings.ScreensaverLockOnResume = req.ScreensaverLockOnResume;
            userSettings.Settings.KeepAwakeUI = req.KeepAwakeUi;
            userSettings.Settings.DisplayPauseSettings = (DisplayPause)req.DisplayPauseSettings;
            userSettings.Settings.RememberSelectedScreen = req.RememberSelectedScreen;
            userSettings.Settings.IsUpdated = req.Updated;
            userSettings.Settings.IsUpdatedNotify = req.UpdatedNotify;
            userSettings.Settings.ApplicationThemeBackground = (Models.Enums.AppThemeBackground)req.ApplicationThemeBackground;
            userSettings.Settings.ApplicationThemeBackgroundPath = req.ApplicationThemeBackgroundPath;
            userSettings.Settings.ThemeBundleVersion = req.ThemeBundleVersion;
            userSettings.Settings.IsScreensaverPluginNotify = req.ScreensaverPluginNotify;

            try
            {
                return Task.FromResult(new Empty());
            }
            finally
            {
                lock (settingsWriteLock)
                {
                    userSettings.Save<SettingsModel>();
                    if (restartRequired)
                    {
                        runner.RestartUI();
                    }
                }
            }
        }

        public override Task<SettingsDataModel> GetSettings(Empty _, ServerCallContext context)
        {
            var settings = userSettings.Settings;
            var resp = new SettingsDataModel()
            {
                SavedUrl = settings.SavedURL,
                ProcessMonitorAlogorithm = (ProcessMonitorRule)((int)settings.ProcessMonitorAlgorithm),
                WallpaperArrangement = (WallpaperArrangementRule)settings.WallpaperArrangement,
                SelectedDisplay = new GetScreensResponse()
                {
                    DeviceId = settings.SelectedDisplay.DeviceId ?? string.Empty,
                    DeviceName = settings.SelectedDisplay.DeviceName ?? string.Empty,
                    DisplayName = settings.SelectedDisplay.DisplayName ?? string.Empty,
                    HMonitor = settings.SelectedDisplay.HMonitor.ToInt32(),
                    IsPrimary = settings.SelectedDisplay.IsPrimary,
                    Index = settings.SelectedDisplay.Index,
                    WorkingArea = new Rectangle()
                    {
                        X = settings.SelectedDisplay.WorkingArea.X,
                        Y = settings.SelectedDisplay.WorkingArea.Y,
                        Width = settings.SelectedDisplay.WorkingArea.Width,
                        Height = settings.SelectedDisplay.WorkingArea.Height
                    },
                    Bounds = new Rectangle()
                    {
                        X = settings.SelectedDisplay.Bounds.X,
                        Y = settings.SelectedDisplay.Bounds.Y,
                        Width = settings.SelectedDisplay.Bounds.Width,
                        Height = settings.SelectedDisplay.Bounds.Height
                    }
                },
                AppVersion = settings.AppVersion,
                AppPreviousVersion = settings.AppPreviousVersion,
                ScreensaverArrangement = (WallpaperArrangementRule)settings.ScreensaverArragement,
                ScreensaverType = (ScreensaverTypeRule)settings.ScreensaverType,
                Startup = settings.Startup,
                IsFirstRun = settings.IsFirstRun,
                ControlPanelOpened = settings.ControlPanelOpened,
                AppFocusPause = (Grpc.Common.Proto.Settings.AppRules)((int)settings.AppFocusPause),
                AppFullscreenPause = (Grpc.Common.Proto.Settings.AppRules)((int)settings.AppFullscreenPause),
                BatteryPause = (Grpc.Common.Proto.Settings.AppRules)((int)settings.BatteryPause),
                VideoPlayer = (MediaPlayer)((int)settings.VideoPlayer),
                VideoPlayerHwAccel = settings.VideoPlayerHwAccel,
                WebBrowser = (WebBrowser)((int)settings.WebBrowser),
                GifPlayer = (GifPlayer)((int)settings.GifPlayer),
                PicturePlayer = (PicturePlayer)(((int)settings.PicturePlayer)),
                WallpaperWaitTime = settings.WallpaperWaitTime,
                ProcessTimerInterval = settings.ProcessTimerInterval,
                StreamQuality = (Grpc.Common.Proto.Settings.StreamQualitySuggestion)((int)settings.StreamQuality),
                LivelyZipGenerate = settings.LivelyZipGenerate,
                ScalerVideo = (WallpaperScalerRule)((int)settings.ScalerVideo),
                ScalerGif = (WallpaperScalerRule)((int)settings.ScalerGif),
                GifCapture = settings.GifCapture,
                MultiFileAutoImport = settings.MultiFileAutoImport,
                SafeShutdown = settings.SafeShutdown,
                IsRestart = settings.IsRestart,
                InputForward = (Grpc.Common.Proto.Settings.InputForwardMode)((int)settings.InputForward),
                MouseInputMovAlways = settings.MouseInputMovAlways,
                TileSize = settings.TileSize,
                LivelyGuiRendering = (GuiMode)settings.UIMode,
                WallpaperDir = settings.WallpaperDir,
                WallpaperDirMoveExistingWallpaperNewDir = settings.WallpaperDirMoveExistingWallpaperNewDir,
                SysTrayIcon = settings.SysTrayIcon,
                WebDebugPort = settings.WebDebugPort,
                AutoDetectOnlineStreams = settings.AutoDetectOnlineStreams,
                ExtractStreamMetaData = settings.ExtractStreamMetaData,
                WallpaperBundleVersion = settings.WallpaperBundleVersion,
                AudioVolumeGlobal = settings.AudioVolumeGlobal,
                AudioOnlyOnDesktop = settings.AudioOnlyOnDesktop,
                WallpaperScaling = (WallpaperScalerRule)settings.WallpaperScaling,
                CefDiskCache = settings.CefDiskCache,
                DebugMenu = settings.DebugMenu,
                TestBuild = settings.TestBuild,
                ApplicationTheme = (Grpc.Common.Proto.Settings.AppTheme)settings.ApplicationTheme,
                RemoteDesktopPause = (Grpc.Common.Proto.Settings.AppRules)settings.RemoteDesktopPause,
                PowerSaveModePause = (Grpc.Common.Proto.Settings.AppRules)settings.PowerSaveModePause,
                LockScreenAutoWallpaper = settings.LockScreenAutoWallpaper,
                DesktopAutoWallpaper = settings.DesktopAutoWallpaper,
                SystemTaskbarTheme = (Grpc.Common.Proto.Settings.TaskbarTheme)((int)settings.SystemTaskbarTheme),
                ScreensaverIdleWait = (Grpc.Common.Proto.Settings.ScreensaverIdleTime)((uint)settings.ScreensaverIdleDelay),
                ScreensaverOledWarning = settings.ScreensaverOledWarning,
                ScreensaverEmptyScreenShowBlack = settings.ScreensaverEmptyScreenShowBlack,
                ScreensaverLockOnResume = settings.ScreensaverLockOnResume,
                Language = settings.Language,
                KeepAwakeUi = settings.KeepAwakeUI,
                DisplayPauseSettings = (DisplayPauseRule)settings.DisplayPauseSettings,
                RememberSelectedScreen = settings.RememberSelectedScreen,
                Updated = settings.IsUpdated,
                UpdatedNotify = settings.IsUpdatedNotify,
                ApplicationThemeBackground = (Grpc.Common.Proto.Settings.AppThemeBackground)settings.ApplicationThemeBackground,
                ApplicationThemeBackgroundPath = settings.ApplicationThemeBackgroundPath ?? string.Empty,
                ThemeBundleVersion = settings.ThemeBundleVersion,
                ScreensaverPluginNotify = settings.IsScreensaverPluginNotify,
            };
            return Task.FromResult(resp);
        }
    }
}
