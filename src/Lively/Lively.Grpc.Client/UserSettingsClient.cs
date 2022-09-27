using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Grpc.Common.Proto.Settings;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public class UserSettingsClient : IUserSettingsClient
    {
        public ISettingsModel Settings { get; private set; }
        public List<IApplicationRulesModel> AppRules { get; private set; }

        private readonly SettingsService.SettingsServiceClient client;

        public UserSettingsClient()
        {
            client = new SettingsService.SettingsServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));

            Task.Run(async () =>
            {
                await LoadAsync<ISettingsModel>().ConfigureAwait(false);
                await LoadAsync<List<IApplicationRulesModel>>().ConfigureAwait(false);
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

        private ISettingsModel GetSettings()
        {
            var resp = client.GetSettings(new Empty());
            return CreateSettingsFromGrpc(resp);
        }

        private async Task<ISettingsModel> GetSettingsAsync()
        {
            var resp = await client.GetSettingsAsync(new Empty());
            return CreateSettingsFromGrpc(resp);
        }

        private List<IApplicationRulesModel> GetAppRulesSettings()
        {
            var appRules = new List<IApplicationRulesModel>();
            var resp = client.GetAppRulesSettings(new Empty());
            foreach (var item in resp.AppRules)
            {
                appRules.Add(new ApplicationRulesModel(item.AppName, (AppRulesEnum)((int)item.Rule)));
            }
            return appRules;
        }

        private async Task<List<IApplicationRulesModel>> GetAppRulesSettingsAsync()
        {
            var appRules = new List<IApplicationRulesModel>();
            var resp = await client.GetAppRulesSettingsAsync(new Empty());
            foreach (var item in resp.AppRules)
            {
                appRules.Add(new ApplicationRulesModel(item.AppName, (AppRulesEnum)((int)item.Rule)));
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
            if (typeof(T) == typeof(ISettingsModel))
            {
                SetSettings();
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
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
            if (typeof(T) == typeof(ISettingsModel))
            {
                await SetSettingsAsync().ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
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
            if (typeof(T) == typeof(ISettingsModel))
            {
                Settings = GetSettings();
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
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
            if (typeof(T) == typeof(ISettingsModel))
            {
                Settings = await GetSettingsAsync().ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                AppRules = await GetAppRulesSettingsAsync().ConfigureAwait(false);
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        #region helpers

        private SettingsDataModel CreateGrpcSettings(ISettingsModel settings)
        {
            return new SettingsDataModel()
            {
                SavedUrl = settings.SavedURL,
                ProcessMonitorAlogorithm = (ProcessMonitorRule)((int)settings.ProcessMonitorAlgorithm),
                WallpaperArrangement = (WallpaperArrangementRule)settings.WallpaperArrangement,
                SelectedDisplay = new GetScreensResponse()
                {
                    DeviceId = settings.SelectedDisplay.DeviceId,
                    DeviceName = settings.SelectedDisplay.DeviceName,
                    DisplayName = settings.SelectedDisplay.DisplayName,
                    HMonitor = settings.SelectedDisplay.HMonitor.ToInt32(),
                    IsPrimary = settings.SelectedDisplay.IsPrimary,
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
                Startup = settings.Startup,
                IsFirstRun = settings.IsFirstRun,
                ControlPanelOpened = settings.ControlPanelOpened,
                AppFocusPause = (AppRules)((int)settings.AppFocusPause),
                AppFullscreenPause = (AppRules)((int)settings.AppFullscreenPause),
                BatteryPause = (AppRules)((int)settings.BatteryPause),
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
                RemoteDesktopPause = (AppRules)settings.RemoteDesktopPause,
                PowerSaveModePause = (AppRules)settings.PowerSaveModePause,
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
                ApplicationThemeBackground = (Common.Proto.Settings.AppThemeBackground)settings.ApplicationThemeBackground,
                ApplicationThemeBackgroundPath = settings.ApplicationThemeBackgroundPath,
                ThemeBundleVersion = settings.ThemeBundleVersion,
            };
        }

        private ISettingsModel CreateSettingsFromGrpc(SettingsDataModel settings)
        {
            return new SettingsModel()
            {
                SavedURL = settings.SavedUrl,
                ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)((int)settings.ProcessMonitorAlogorithm),
                SelectedDisplay = new DisplayMonitor()
                {
                    DeviceId = settings.SelectedDisplay.DeviceId,
                    DisplayName = settings.SelectedDisplay.DisplayName,
                    DeviceName = settings.SelectedDisplay.DeviceName,
                    HMonitor = new IntPtr(settings.SelectedDisplay.HMonitor),
                    IsPrimary = settings.SelectedDisplay.IsPrimary,
                    Index = settings.SelectedDisplay.Index,
                },
                WallpaperArrangement = (WallpaperArrangement)((int)settings.WallpaperArrangement),
                AppVersion = settings.AppVersion,
                Startup = settings.Startup,
                IsFirstRun = settings.IsFirstRun,
                ControlPanelOpened = settings.ControlPanelOpened,
                AppFocusPause = (AppRulesEnum)((int)settings.AppFocusPause),
                AppFullscreenPause = (AppRulesEnum)((int)settings.AppFullscreenPause),
                BatteryPause = (AppRulesEnum)((int)settings.BatteryPause),
                VideoPlayer = (LivelyMediaPlayer)((int)settings.VideoPlayer),
                VideoPlayerHwAccel = settings.VideoPlayerHwAccel,
                WebBrowser = (LivelyWebBrowser)((int)settings.WebBrowser),
                GifPlayer = (LivelyGifPlayer)((int)settings.GifPlayer),
                PicturePlayer = (LivelyPicturePlayer)((int)settings.PicturePlayer),
                WallpaperWaitTime = settings.WallpaperWaitTime,
                ProcessTimerInterval = settings.ProcessTimerInterval,
                StreamQuality = (Lively.Common.StreamQualitySuggestion)((int)settings.StreamQuality),
                LivelyZipGenerate = settings.LivelyZipGenerate,
                ScalerVideo = (WallpaperScaler)((int)settings.ScalerVideo),
                ScalerGif = (WallpaperScaler)((int)settings.ScalerGif),
                GifCapture = settings.GifCapture,
                MultiFileAutoImport = settings.MultiFileAutoImport,
                SafeShutdown = settings.SafeShutdown,
                IsRestart = settings.IsRestart,
                InputForward = (Lively.Common.InputForwardMode)settings.InputForward,
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
                ApplicationTheme = (Lively.Common.AppTheme)settings.ApplicationTheme,
                RemoteDesktopPause = (AppRulesEnum)settings.RemoteDesktopPause,
                PowerSaveModePause = (AppRulesEnum)settings.PowerSaveModePause,
                LockScreenAutoWallpaper = settings.LockScreenAutoWallpaper,
                DesktopAutoWallpaper = settings.DesktopAutoWallpaper,
                SystemTaskbarTheme = (Lively.Common.TaskbarTheme)settings.SystemTaskbarTheme,
                ScreensaverIdleDelay = (Lively.Common.ScreensaverIdleTime)((int)settings.ScreensaverIdleWait),
                ScreensaverOledWarning = settings.ScreensaverOledWarning,
                ScreensaverEmptyScreenShowBlack = settings.ScreensaverEmptyScreenShowBlack,
                ScreensaverLockOnResume = settings.ScreensaverLockOnResume,
                Language = settings.Language,
                KeepAwakeUI = settings.KeepAwakeUi,
                DisplayPauseSettings = (DisplayPauseEnum)settings.DisplayPauseSettings,
                RememberSelectedScreen = settings.RememberSelectedScreen,
                IsUpdated = settings.Updated,
                ApplicationThemeBackground = (Lively.Common.AppThemeBackground)settings.ApplicationThemeBackground,
                ApplicationThemeBackgroundPath = settings.ApplicationThemeBackgroundPath,
                ThemeBundleVersion = settings.ThemeBundleVersion,
            };
        }

        #endregion //helpers
    }
}