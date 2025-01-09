using Google.Protobuf.WellKnownTypes;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Grpc.Common.Proto.Settings;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public class UserSettingsClient : IUserSettingsClient
    {
        public SettingsModel Settings { get; private set; }
        public List<ApplicationRulesModel> AppRules { get; private set; }

        private readonly SettingsService.SettingsServiceClient client;

        public UserSettingsClient()
        {
            client = new SettingsService.SettingsServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));

            Task.Run(async () =>
            {
                await LoadAsync<SettingsModel>().ConfigureAwait(false);
                await LoadAsync<List<ApplicationRulesModel>>().ConfigureAwait(false);
            }).Wait();
        }

        private void SetSettings()
        {
            _ = client.SetSettings(CreateGrpcSettings(Settings));
        }

        private async Task SetSettingsAsync()
        {
            _ = await client.SetSettingsAsync(CreateGrpcSettings(Settings));
        }

        private SettingsModel GetSettings()
        {
            var resp = client.GetSettings(new Empty());
            return CreateSettingsFromGrpc(resp);
        }

        private async Task<SettingsModel> GetSettingsAsync()
        {
            var resp = await client.GetSettingsAsync(new Empty());
            return CreateSettingsFromGrpc(resp);
        }

        private List<ApplicationRulesModel> GetAppRulesSettings()
        {
            var appRules = new List<ApplicationRulesModel>();
            var resp = client.GetAppRulesSettings(new Empty());
            foreach (var item in resp.AppRules)
            {
                appRules.Add(new ApplicationRulesModel(item.AppName, (Models.Enums.AppRules)((int)item.Rule)));
            }
            return appRules;
        }

        private async Task<List<ApplicationRulesModel>> GetAppRulesSettingsAsync()
        {
            var appRules = new List<ApplicationRulesModel>();
            var resp = await client.GetAppRulesSettingsAsync(new Empty());
            foreach (var item in resp.AppRules)
            {
                appRules.Add(new ApplicationRulesModel(item.AppName, (Models.Enums.AppRules)((int)item.Rule)));
            }
            return appRules;
        }

        private void SetAppRulesSettings()
        {
            var tmp = new AppRulesSettings();
            foreach (var item in AppRules)
            {
                tmp.AppRules.Add(new AppRulesDataModel
                {
                    AppName = item.AppName,
                    Rule = (Common.Proto.Settings.AppRules)((int)item.Rule)
                });
            }
            _ = client.SetAppRulesSettings(tmp);
        }

        private async Task SetAppRulesSettingsAsync()
        {
            var tmp = new AppRulesSettings();
            foreach (var item in AppRules)
            {
                tmp.AppRules.Add(new AppRulesDataModel
                {
                    AppName = item.AppName,
                    Rule = (Common.Proto.Settings.AppRules)((int)item.Rule)
                });
            }
            _ = await client.SetAppRulesSettingsAsync(tmp);
        }

        public void Save<T>()
        {
            if (typeof(T) == typeof(SettingsModel))
            {
                SetSettings();
            }
            else if (typeof(T) == typeof(List<ApplicationRulesModel>))
            {
                SetAppRulesSettings();
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        public async Task SaveAsync<T>()
        {
            if (typeof(T) == typeof(SettingsModel))
            {
                await SetSettingsAsync().ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(List<ApplicationRulesModel>))
            {
                await SetAppRulesSettingsAsync().ConfigureAwait(false);
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        public void Load<T>()
        {
            if (typeof(T) == typeof(SettingsModel))
            {
                Settings = GetSettings();
            }
            else if (typeof(T) == typeof(List<ApplicationRulesModel>))
            {
                AppRules = GetAppRulesSettings();
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        public async Task LoadAsync<T>()
        {
            if (typeof(T) == typeof(SettingsModel))
            {
                Settings = await GetSettingsAsync().ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(List<ApplicationRulesModel>))
            {
                AppRules = await GetAppRulesSettingsAsync().ConfigureAwait(false);
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        #region helpers

        private SettingsDataModel CreateGrpcSettings(SettingsModel settings)
        {
            return new SettingsDataModel()
            {
                SavedUrl = settings.SavedURL,
                ProcessMonitorAlogorithm = (ProcessMonitorRule)((int)settings.ProcessMonitorAlgorithm),
                WallpaperArrangement = (WallpaperArrangementRule)settings.WallpaperArrangement,
                ScreensaverArrangement = (WallpaperArrangementRule)settings.ScreensaverArragement,
                ScreensaverType = (ScreensaverTypeRule)settings.ScreensaverType,
                SelectedDisplay = new GetScreensResponse()
                {
                    DeviceId = settings.SelectedDisplay.DeviceId,
                    DeviceName = settings.SelectedDisplay.DeviceName,
                    DisplayName = settings.SelectedDisplay.DisplayName,
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
                Startup = settings.Startup,
                IsFirstRun = settings.IsFirstRun,
                ControlPanelOpened = settings.ControlPanelOpened,
                AppFocusPause = (Common.Proto.Settings.AppRules)((int)settings.AppFocusPause),
                AppFullscreenPause = (Common.Proto.Settings.AppRules)((int)settings.AppFullscreenPause),
                BatteryPause = (Common.Proto.Settings.AppRules)((int)settings.BatteryPause),
                VideoPlayer = (MediaPlayer)((int)settings.VideoPlayer),
                VideoPlayerHwAccel = settings.VideoPlayerHwAccel,
                WebBrowser = (WebBrowser)((int)settings.WebBrowser),
                GifPlayer = (GifPlayer)((int)settings.GifPlayer),
                PicturePlayer = (PicturePlayer)((int)settings.PicturePlayer),
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
                RemoteDesktopPause = (Common.Proto.Settings.AppRules)settings.RemoteDesktopPause,
                PowerSaveModePause = (Common.Proto.Settings.AppRules)settings.PowerSaveModePause,
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
                ApplicationThemeBackground = (Common.Proto.Settings.AppThemeBackground)settings.ApplicationThemeBackground,
                ApplicationThemeBackgroundPath = settings.ApplicationThemeBackgroundPath,
                ThemeBundleVersion = settings.ThemeBundleVersion,
                ScreensaverPluginNotify = settings.IsScreensaverPluginNotify,
            };
        }

        private SettingsModel CreateSettingsFromGrpc(SettingsDataModel settings)
        {
            return new SettingsModel()
            {
                SavedURL = settings.SavedUrl,
                ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)((int)settings.ProcessMonitorAlogorithm),
                ScreensaverArragement = (WallpaperArrangement)settings.ScreensaverArrangement,
                ScreensaverType = (ScreensaverType)settings.ScreensaverType,
                SelectedDisplay = new DisplayMonitor()
                {
                    DeviceId = settings.SelectedDisplay.DeviceId,
                    DisplayName = settings.SelectedDisplay.DisplayName,
                    DeviceName = settings.SelectedDisplay.DeviceName,
                    HMonitor = new IntPtr(settings.SelectedDisplay.HMonitor),
                    IsPrimary = settings.SelectedDisplay.IsPrimary,
                    Index = settings.SelectedDisplay.Index,                  
                    Bounds = new System.Drawing.Rectangle(
                        settings.SelectedDisplay.Bounds.X,
                        settings.SelectedDisplay.Bounds.Y,
                        settings.SelectedDisplay.Bounds.Width,
                        settings.SelectedDisplay.Bounds.Height),
                    WorkingArea = new System.Drawing.Rectangle(
                        settings.SelectedDisplay.WorkingArea.X,
                        settings.SelectedDisplay.WorkingArea.Y,
                        settings.SelectedDisplay.WorkingArea.Width,
                        settings.SelectedDisplay.WorkingArea.Height),
                },
                WallpaperArrangement = (WallpaperArrangement)((int)settings.WallpaperArrangement),
                AppVersion = settings.AppVersion,
                AppPreviousVersion = settings.AppPreviousVersion,
                Startup = settings.Startup,
                IsFirstRun = settings.IsFirstRun,
                ControlPanelOpened = settings.ControlPanelOpened,
                AppFocusPause = (Models.Enums.AppRules)((int)settings.AppFocusPause),
                AppFullscreenPause = (Models.Enums.AppRules)((int)settings.AppFullscreenPause),
                BatteryPause = (Models.Enums.AppRules)((int)settings.BatteryPause),
                VideoPlayer = (LivelyMediaPlayer)((int)settings.VideoPlayer),
                VideoPlayerHwAccel = settings.VideoPlayerHwAccel,
                WebBrowser = (LivelyWebBrowser)((int)settings.WebBrowser),
                GifPlayer = (LivelyGifPlayer)((int)settings.GifPlayer),
                PicturePlayer = (LivelyPicturePlayer)((int)settings.PicturePlayer),
                WallpaperWaitTime = settings.WallpaperWaitTime,
                ProcessTimerInterval = settings.ProcessTimerInterval,
                StreamQuality = (Models.Enums.StreamQualitySuggestion)((int)settings.StreamQuality),
                LivelyZipGenerate = settings.LivelyZipGenerate,
                ScalerVideo = (WallpaperScaler)((int)settings.ScalerVideo),
                ScalerGif = (WallpaperScaler)((int)settings.ScalerGif),
                GifCapture = settings.GifCapture,
                MultiFileAutoImport = settings.MultiFileAutoImport,
                SafeShutdown = settings.SafeShutdown,
                IsRestart = settings.IsRestart,
                InputForward = (Models.Enums.InputForwardMode)settings.InputForward,
                MouseInputMovAlways = settings.MouseInputMovAlways,
                TileSize = settings.TileSize,
                UIMode = (LivelyGUIState)((int)settings.LivelyGuiRendering),
                WallpaperDir = settings.WallpaperDir,
                WallpaperDirMoveExistingWallpaperNewDir = settings.WallpaperDirMoveExistingWallpaperNewDir,
                SysTrayIcon = settings.SysTrayIcon,
                WebDebugPort = settings.WebDebugPort,
                AutoDetectOnlineStreams = settings.AutoDetectOnlineStreams,
                ExtractStreamMetaData = settings.ExtractStreamMetaData,
                WallpaperBundleVersion = settings.WallpaperBundleVersion,
                AudioVolumeGlobal = settings.AudioVolumeGlobal,
                AudioOnlyOnDesktop = settings.AudioOnlyOnDesktop,
                WallpaperScaling = (WallpaperScaler)settings.WallpaperScaling,
                CefDiskCache = settings.CefDiskCache,
                DebugMenu = settings.DebugMenu,
                TestBuild = settings.TestBuild,
                ApplicationTheme = (Models.Enums.AppTheme)settings.ApplicationTheme,
                RemoteDesktopPause = (Models.Enums.AppRules)settings.RemoteDesktopPause,
                PowerSaveModePause = (Models.Enums.AppRules)settings.PowerSaveModePause,
                LockScreenAutoWallpaper = settings.LockScreenAutoWallpaper,
                DesktopAutoWallpaper = settings.DesktopAutoWallpaper,
                SystemTaskbarTheme = (Models.Enums.TaskbarTheme)settings.SystemTaskbarTheme,
                ScreensaverIdleDelay = (Models.Enums.ScreensaverIdleTime)((int)settings.ScreensaverIdleWait),
                ScreensaverOledWarning = settings.ScreensaverOledWarning,
                ScreensaverEmptyScreenShowBlack = settings.ScreensaverEmptyScreenShowBlack,
                ScreensaverLockOnResume = settings.ScreensaverLockOnResume,
                Language = settings.Language,
                KeepAwakeUI = settings.KeepAwakeUi,
                DisplayPauseSettings = (DisplayPause)settings.DisplayPauseSettings,
                RememberSelectedScreen = settings.RememberSelectedScreen,
                IsUpdated = settings.Updated,
                IsUpdatedNotify = settings.UpdatedNotify,
                ApplicationThemeBackground = (Models.Enums.AppThemeBackground)settings.ApplicationThemeBackground,
                ApplicationThemeBackgroundPath = settings.ApplicationThemeBackgroundPath,
                ThemeBundleVersion = settings.ThemeBundleVersion,
                IsScreensaverPluginNotify = settings.ScreensaverPluginNotify,
            };
        }

        #endregion //helpers
    }
}