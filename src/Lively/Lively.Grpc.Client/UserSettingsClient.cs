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
                await Load<ISettingsModel>();
                await Load<List<IApplicationRulesModel>>();
            }).Wait();
        }

        private async Task SetSettings()
        {
            var settings = new SettingsDataModel()
            {
                SavedUrl = Settings.SavedURL,
                ProcessMonitorAlogorithm = (ProcessMonitorRule)((int)Settings.ProcessMonitorAlgorithm),
                WallpaperArrangement = (WallpaperArrangementRule)Settings.WallpaperArrangement,
                SelectedDisplay = new GetScreensResponse()
                {
                    DeviceId = Settings.SelectedDisplay.DeviceId,
                    DeviceName = Settings.SelectedDisplay.DeviceName,
                    DisplayName = Settings.SelectedDisplay.DisplayName,
                    HMonitor = Settings.SelectedDisplay.HMonitor.ToInt32(),
                    IsPrimary = Settings.SelectedDisplay.IsPrimary,
                    WorkingArea = new Rectangle()
                    {
                        X = Settings.SelectedDisplay.WorkingArea.X,
                        Y = Settings.SelectedDisplay.WorkingArea.Y,
                        Width = Settings.SelectedDisplay.WorkingArea.Width,
                        Height = Settings.SelectedDisplay.WorkingArea.Height
                    },
                    Bounds = new Rectangle()
                    {
                        X = Settings.SelectedDisplay.Bounds.X,
                        Y = Settings.SelectedDisplay.Bounds.Y,
                        Width = Settings.SelectedDisplay.Bounds.Width,
                        Height = Settings.SelectedDisplay.Bounds.Height
                    }
                },
                AppVersion = Settings.AppVersion,
                Startup = Settings.Startup,
                IsFirstRun = Settings.IsFirstRun,
                ControlPanelOpened = Settings.ControlPanelOpened,
                AppFocusPause = (AppRules)((int)Settings.AppFocusPause),
                AppFullscreenPause = (AppRules)((int)Settings.AppFullscreenPause),
                BatteryPause = (AppRules)((int)Settings.BatteryPause),
                VideoPlayer = (MediaPlayer)((int)Settings.VideoPlayer),
                VideoPlayerHwAccel = Settings.VideoPlayerHwAccel,
                WebBrowser = (WebBrowser)((int)Settings.WebBrowser),
                GifPlayer = (GifPlayer)((int)Settings.GifPlayer),
                WallpaperWaitTime = Settings.WallpaperWaitTime,
                ProcessTimerInterval = Settings.ProcessTimerInterval,
                StreamQuality = (Grpc.Common.Proto.Settings.StreamQualitySuggestion)((int)Settings.StreamQuality),
                LivelyZipGenerate = Settings.LivelyZipGenerate,
                ScalerVideo = (WallpaperScalerRule)((int)Settings.ScalerVideo),
                ScalerGif = (WallpaperScalerRule)((int)Settings.ScalerGif),
                GifCapture = Settings.GifCapture,
                MultiFileAutoImport = Settings.MultiFileAutoImport,
                SafeShutdown = Settings.SafeShutdown,
                IsRestart = Settings.IsRestart,
                InputForward = (Grpc.Common.Proto.Settings.InputForwardMode)((int)Settings.InputForward),
                MouseInputMovAlways = Settings.MouseInputMovAlways,
                TileSize = Settings.TileSize,
                LivelyGuiRendering = (GuiMode)Settings.LivelyGUIRendering,
                WallpaperDir = Settings.WallpaperDir,
                WallpaperDirMoveExistingWallpaperNewDir = Settings.WallpaperDirMoveExistingWallpaperNewDir,
                SysTrayIcon = Settings.SysTrayIcon,
                WebDebugPort = Settings.WebDebugPort,
                AutoDetectOnlineStreams = Settings.AutoDetectOnlineStreams,
                ExtractStreamMetaData = Settings.ExtractStreamMetaData,
                WallpaperBundleVersion = Settings.WallpaperBundleVersion,
                AudioVolumeGlobal = Settings.AudioVolumeGlobal,
                AudioOnlyOnDesktop = Settings.AudioOnlyOnDesktop,
                WallpaperScaling = (WallpaperScalerRule)Settings.WallpaperScaling,
                CefDiskCache = Settings.CefDiskCache,
                DebugMenu = Settings.DebugMenu,
                TestBuild = Settings.TestBuild,
                ApplicationTheme = (Grpc.Common.Proto.Settings.AppTheme)Settings.ApplicationTheme,
                RemoteDesktopPause = (AppRules)Settings.RemoteDesktopPause,
                PowerSaveModePause = (AppRules)Settings.PowerSaveModePause,
                LockScreenAutoWallpaper = Settings.LockScreenAutoWallpaper,
                DesktopAutoWallpaper = Settings.DesktopAutoWallpaper,
                SystemTaskbarTheme = (Grpc.Common.Proto.Settings.TaskbarTheme)((int)Settings.SystemTaskbarTheme),
                ScreensaverIdleWait = (Grpc.Common.Proto.Settings.ScreensaverIdleTime)((uint)Settings.WallpaperWaitTime),
                ScreensaverOledWarning = Settings.ScreensaverOledWarning,
                ScreensaverEmptyScreenShowBlack = Settings.ScreensaverEmptyScreenShowBlack,
                ScreensaverLockOnResume = Settings.ScreensaverLockOnResume,
                Language = Settings.Language,
                KeepAwakeUi = Settings.KeepAwakeUI,
            };
            _ = await client.SetSettingsAsync(settings);
        }

        private async Task<ISettingsModel> GetSettings()
        {
            var resp = await client.GetSettingsAsync(new Empty());
            var settings = new SettingsModel()
            {
                SavedURL = resp.SavedUrl,
                ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)((int)resp.ProcessMonitorAlogorithm),
                SelectedDisplay = new DisplayMonitor()
                {
                    DeviceId = resp.SelectedDisplay.DeviceId,
                    DisplayName = resp.SelectedDisplay.DisplayName,
                    DeviceName = resp.SelectedDisplay.DeviceName,
                    HMonitor = new IntPtr(resp.SelectedDisplay.HMonitor),
                    IsPrimary = resp.SelectedDisplay.IsPrimary,
                    Index = resp.SelectedDisplay.Index,
                },
                WallpaperArrangement = (WallpaperArrangement)((int)resp.WallpaperArrangement),
                AppVersion = resp.AppVersion,
                Startup = resp.Startup,
                IsFirstRun = resp.IsFirstRun,
                ControlPanelOpened = resp.ControlPanelOpened,
                AppFocusPause = (AppRulesEnum)((int)resp.AppFocusPause),
                AppFullscreenPause = (AppRulesEnum)((int)resp.AppFullscreenPause),
                BatteryPause = (AppRulesEnum)((int)resp.BatteryPause),
                VideoPlayer = (LivelyMediaPlayer)((int)resp.VideoPlayer),
                VideoPlayerHwAccel = resp.VideoPlayerHwAccel,
                WebBrowser = (LivelyWebBrowser)((int)resp.WebBrowser),
                GifPlayer = (LivelyGifPlayer)((int)resp.GifPlayer),
                WallpaperWaitTime = resp.WallpaperWaitTime,
                ProcessTimerInterval = resp.ProcessTimerInterval,
                StreamQuality = (Lively.Common.StreamQualitySuggestion)((int)resp.StreamQuality),
                LivelyZipGenerate = resp.LivelyZipGenerate,
                ScalerVideo = (WallpaperScaler)((int)resp.ScalerVideo),
                ScalerGif = (WallpaperScaler)((int)resp.ScalerGif),
                GifCapture = resp.GifCapture,
                MultiFileAutoImport = resp.MultiFileAutoImport,
                SafeShutdown = resp.SafeShutdown,
                IsRestart = resp.IsRestart,
                InputForward = (Lively.Common.InputForwardMode)resp.InputForward,
                MouseInputMovAlways = resp.MouseInputMovAlways,
                TileSize = resp.TileSize,
                LivelyGUIRendering = (LivelyGUIState)((int)resp.LivelyGuiRendering),
                WallpaperDir = resp.WallpaperDir,
                WallpaperDirMoveExistingWallpaperNewDir = resp.WallpaperDirMoveExistingWallpaperNewDir,
                SysTrayIcon = resp.SysTrayIcon,
                WebDebugPort = resp.WebDebugPort,
                AutoDetectOnlineStreams = resp.AutoDetectOnlineStreams,
                ExtractStreamMetaData = resp.ExtractStreamMetaData,
                WallpaperBundleVersion = resp.WallpaperBundleVersion,
                AudioVolumeGlobal = resp.AudioVolumeGlobal,
                AudioOnlyOnDesktop = resp.AudioOnlyOnDesktop,
                WallpaperScaling = (WallpaperScaler)resp.WallpaperScaling,
                CefDiskCache = resp.CefDiskCache,
                DebugMenu = resp.DebugMenu,
                TestBuild = resp.TestBuild,
                ApplicationTheme = (Lively.Common.AppTheme)resp.ApplicationTheme,
                RemoteDesktopPause = (AppRulesEnum)resp.RemoteDesktopPause,
                PowerSaveModePause = (AppRulesEnum)resp.PowerSaveModePause,
                LockScreenAutoWallpaper = resp.LockScreenAutoWallpaper,
                DesktopAutoWallpaper = resp.DesktopAutoWallpaper,
                SystemTaskbarTheme = (Lively.Common.TaskbarTheme)resp.SystemTaskbarTheme,
                ScreensaverIdleWait = (Lively.Common.ScreensaverIdleTime)((int)resp.ScreensaverIdleWait),
                ScreensaverOledWarning = resp.ScreensaverOledWarning,
                ScreensaverEmptyScreenShowBlack = resp.ScreensaverEmptyScreenShowBlack,
                ScreensaverLockOnResume = resp.ScreensaverLockOnResume,
                Language = resp.Language,
                KeepAwakeUI = resp.KeepAwakeUi,
            };
            return settings;
        }

        private async Task<List<IApplicationRulesModel>> GetAppRulesSettings()
        {
            var appRules = new List<IApplicationRulesModel>();
            using var call = client.GetAppRulesSettings(new Empty());
            while (await call.ResponseStream.MoveNext())
            {
                var resp = call.ResponseStream.Current;
                appRules.Add(new ApplicationRulesModel(resp.AppName, (AppRulesEnum)((int)resp.Rule)));
            }
            return appRules;
        }

        private async Task SetAppRulesSettings()
        {
            try
            {
                using var call = client.SetAppRulesSettings();
                foreach (var item in AppRules)
                {
                    await call.RequestStream.WriteAsync(new AppRulesDataModel
                    {
                        AppName = item.AppName,
                        Rule = (Common.Proto.Settings.AppRules)((int)item.Rule)
                    });
                }
                await call.RequestStream.CompleteAsync();
                var resp = await call.ResponseAsync;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public async Task Save<T>()
        {
            if (typeof(T) == typeof(ISettingsModel))
            {
                await SetSettings();
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                await SetAppRulesSettings();
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        public async Task Load<T>()
        {
            if (typeof(T) == typeof(ISettingsModel))
            {
                Settings = await GetSettings();
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                AppRules = await GetAppRulesSettings();
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }
    }
}
