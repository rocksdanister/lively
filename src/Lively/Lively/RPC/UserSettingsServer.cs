using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Core.Display;
using Lively.Grpc.Common.Proto.Settings;
using Lively.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lively.Common;
using Lively.Models;
using System.Linq;
using System.Diagnostics;

namespace Lively.RPC
{
    internal class UserSettingsServer : SettingsService.SettingsServiceBase
    {
        private readonly IDisplayManager displayManager;
        private readonly IUserSettingsService userSettings;

        public UserSettingsServer(IDisplayManager displayManager, IUserSettingsService userSettings)
        {
            this.displayManager = displayManager;
            this.userSettings = userSettings;
        }

        public override async Task GetAppRulesSettings(Empty _, IServerStreamWriter<AppRulesDataModel> responseStream, ServerCallContext context)
        {
            try
            {
                foreach (var app in userSettings.AppRules)
                {
                    var resp = new AppRulesDataModel
                    {
                        AppName = app.AppName,
                        Rule = (AppRules)((int)app.Rule)
                    };
                    await responseStream.WriteAsync(resp);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public override async Task<Empty> SetAppRulesSettings(IAsyncStreamReader<AppRulesDataModel> requestStream, ServerCallContext context)
        {
            try
            {
                userSettings.AppRules.Clear();
                while (await requestStream.MoveNext())
                {
                    var rule = requestStream.Current;
                    userSettings.AppRules.Add(new ApplicationRulesModel(rule.AppName, (AppRulesEnum)((int)rule.Rule)));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            userSettings.Save<List<IApplicationRulesModel>>();
            return new Empty();
        }

        public override Task<Empty> SetSettings(SettingsDataModel resp, ServerCallContext context)
        {
            userSettings.Settings.SavedURL = resp.SavedUrl;
            userSettings.Settings.ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)((int)resp.ProcessMonitorAlogorithm);
            userSettings.Settings.SelectedDisplay = displayManager.DisplayMonitors.FirstOrDefault(x => resp.SelectedDisplay.DeviceId == x.DeviceId) ?? displayManager.PrimaryDisplayMonitor;
            userSettings.Settings.WallpaperArrangement = (WallpaperArrangement)((int)resp.WallpaperArrangement);
            userSettings.Settings.AppVersion = resp.AppVersion;
            userSettings.Settings.Startup = resp.Startup;
            userSettings.Settings.IsFirstRun = resp.IsFirstRun;
            userSettings.Settings.ControlPanelOpened = resp.ControlPanelOpened;
            userSettings.Settings.AppFocusPause = (AppRulesEnum)((int)resp.AppFocusPause);
            userSettings.Settings.AppFullscreenPause = (AppRulesEnum)((int)resp.AppFullscreenPause);
            userSettings.Settings.BatteryPause = (AppRulesEnum)((int)resp.BatteryPause);
            userSettings.Settings.VideoPlayer = (LivelyMediaPlayer)((int)resp.VideoPlayer);
            userSettings.Settings.VideoPlayerHwAccel = resp.VideoPlayerHwAccel;
            userSettings.Settings.WebBrowser = (LivelyWebBrowser)((int)resp.WebBrowser);
            userSettings.Settings.GifPlayer = (LivelyGifPlayer)((int)resp.GifPlayer);
            userSettings.Settings.PicturePlayer = (LivelyPicturePlayer)((int)resp.PicturePlayer);
            userSettings.Settings.WallpaperWaitTime = resp.WallpaperWaitTime;
            userSettings.Settings.ProcessTimerInterval = resp.ProcessTimerInterval;
            userSettings.Settings.StreamQuality = (Common.StreamQualitySuggestion)((int)resp.StreamQuality);
            userSettings.Settings.LivelyZipGenerate = resp.LivelyZipGenerate;
            userSettings.Settings.ScalerVideo = (WallpaperScaler)((int)resp.ScalerVideo);
            userSettings.Settings.ScalerGif = (WallpaperScaler)((int)resp.ScalerGif);
            userSettings.Settings.GifCapture = resp.GifCapture;
            userSettings.Settings.MultiFileAutoImport = resp.MultiFileAutoImport;
            userSettings.Settings.SafeShutdown = resp.SafeShutdown;
            userSettings.Settings.IsRestart = resp.IsRestart;
            userSettings.Settings.InputForward = (Common.InputForwardMode)resp.InputForward;
            userSettings.Settings.MouseInputMovAlways = resp.MouseInputMovAlways;
            userSettings.Settings.TileSize = resp.TileSize;
            userSettings.Settings.LivelyGUIRendering = (LivelyGUIState)((int)resp.LivelyGuiRendering);
            userSettings.Settings.WallpaperDir = resp.WallpaperDir;
            userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir = resp.WallpaperDirMoveExistingWallpaperNewDir;
            userSettings.Settings.SysTrayIcon = resp.SysTrayIcon;
            userSettings.Settings.WebDebugPort = resp.WebDebugPort;
            userSettings.Settings.AutoDetectOnlineStreams = resp.AutoDetectOnlineStreams;
            userSettings.Settings.ExtractStreamMetaData = resp.ExtractStreamMetaData;
            userSettings.Settings.WallpaperBundleVersion = resp.WallpaperBundleVersion;
            userSettings.Settings.AudioVolumeGlobal = resp.AudioVolumeGlobal;
            userSettings.Settings.AudioOnlyOnDesktop = resp.AudioOnlyOnDesktop;
            userSettings.Settings.WallpaperScaling = (WallpaperScaler)resp.WallpaperScaling;
            userSettings.Settings.CefDiskCache = resp.CefDiskCache;
            userSettings.Settings.DebugMenu = resp.DebugMenu;
            userSettings.Settings.TestBuild = resp.TestBuild;
            userSettings.Settings.ApplicationTheme = (Common.AppTheme)resp.ApplicationTheme;
            userSettings.Settings.RemoteDesktopPause = (AppRulesEnum)resp.RemoteDesktopPause;
            userSettings.Settings.PowerSaveModePause = (AppRulesEnum)resp.PowerSaveModePause;
            userSettings.Settings.LockScreenAutoWallpaper = resp.LockScreenAutoWallpaper;
            userSettings.Settings.DesktopAutoWallpaper = resp.DesktopAutoWallpaper;
            userSettings.Settings.SystemTaskbarTheme = (Common.TaskbarTheme)resp.SystemTaskbarTheme;
            userSettings.Settings.ScreensaverIdleWait = (Common.ScreensaverIdleTime)((int)resp.ScreensaverIdleWait);
            userSettings.Settings.ScreensaverOledWarning = resp.ScreensaverOledWarning;
            userSettings.Settings.ScreensaverEmptyScreenShowBlack = resp.ScreensaverEmptyScreenShowBlack;
            userSettings.Settings.ScreensaverLockOnResume = resp.ScreensaverLockOnResume;
            userSettings.Settings.Language = resp.Language;
            userSettings.Settings.KeepAwakeUI = resp.KeepAwakeUi;

            userSettings.Save<ISettingsModel>();
            return Task.FromResult(new Empty());
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
                LivelyGuiRendering = (GuiMode)settings.LivelyGUIRendering,
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
                ScreensaverIdleWait = (Grpc.Common.Proto.Settings.ScreensaverIdleTime)((uint)settings.WallpaperWaitTime),
                ScreensaverOledWarning = settings.ScreensaverOledWarning,
                ScreensaverEmptyScreenShowBlack = settings.ScreensaverEmptyScreenShowBlack,
                ScreensaverLockOnResume = settings.ScreensaverLockOnResume,
                Language = settings.Language,
                KeepAwakeUi = settings.KeepAwakeUI,
            };
            return Task.FromResult(resp);
        }
    }
}
