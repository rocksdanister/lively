using Lively.Models.Enums;
using System;
using System.Globalization;
using System.IO;

namespace Lively.Models
{
    public class SettingsModel
    {
        public string AppVersion { get; set; }
        public string AppPreviousVersion { get; set; }
        public string Language { get; set; }
        public bool Startup { get; set; }
        /// <summary>
        /// Add user opened wallpapers to library.
        /// </summary>
        public bool GenerateTile { get; set; }
        public bool LivelyZipGenerate { get; set; }
        /// <summary>
        /// Show wallpaper info icon topright of library tile.
        /// </summary>
        public bool WaterMarkTile { get; set; }
        public bool IsFirstRun { get; set; }
        public bool ControlPanelOpened { get; set; }
        public AppRules AppFocusPause { get; set; }

        public AppRules AppFullscreenPause { get; set; }
        public AppRules BatteryPause { get; set; }
        public AppRules RemoteDesktopPause { get; set; }
        public AppRules PowerSaveModePause { get; set; }
        public DisplayPause DisplayPauseSettings { get; set; }
        public ProcessMonitorAlgorithm ProcessMonitorAlgorithm { get; set; }
        /// <summary>
        /// Show animatd library tiles.
        /// </summary>
        public bool LiveTile { get; set; }
        public WallpaperScaler ScalerVideo { get; set; }
        public WallpaperScaler ScalerGif { get; set; }
        public WallpaperArrangement WallpaperArrangement { get; set; }
        public string SavedURL { get; set; }
        public string IgnoreUpdateTag { get; set; }
        /// <summary>
        /// Timer interval(in milliseconds), used to monitor running apps to determine pause/play of wp's.
        /// </summary>
        public int ProcessTimerInterval { get; set; }
        /// <summary>
        /// Timeout for application wallpaper startup (in milliseconds), lively will kill wp if gui is not ready within this timeframe.
        /// </summary>
        public int WallpaperWaitTime { get; set; }
        public bool SafeShutdown { get; set; }
        public bool IsRestart { get; set; }
        public InputForwardMode InputForward { get; set; }
        /// <summary>
        /// True: Always forward mouse movement, even when foreground apps open;
        /// False: Only forward on desktop.
        /// </summary>
        public bool MouseInputMovAlways { get; set; }
        public int TileSize { get; set; }
        public DisplayIdentificationMode DisplayIdentification { get; set; }
        public LivelyMediaPlayer VideoPlayer { get; set; }
        /// <summary>
        /// Video gpu decode
        /// </summary>
        public bool VideoPlayerHwAccel { get; set; }
        /// <summary>
        /// Gif and picture wallpaper player.
        /// </summary>
        public LivelyGifPlayer GifPlayer { get; set; }
        public LivelyPicturePlayer PicturePlayer { get; set; }
        public LivelyWebBrowser WebBrowser { get; set; }
        public bool GifCapture { get; set; }
        public bool MultiFileAutoImport { get; set; }
        public DisplayMonitor SelectedDisplay { get; set; }
        public LivelyGUIState UIMode { get; set; }
        public string WallpaperDir { get; set; }
        public bool WallpaperDirMoveExistingWallpaperNewDir { get; set; }
        public bool SysTrayIcon { get; set; }
        public bool AutoDetectOnlineStreams { get; set; }
        public bool ExtractStreamMetaData { get; set; }
        /// <summary>
        /// Cefsharp debug port.
        /// </summary>
        public string WebDebugPort { get; set; }
        public int WallpaperBundleVersion { get; set; }
        public StreamQualitySuggestion StreamQuality { get; set; }
        /// <summary>
        /// 0 - 100 sound level, affects every wallpaper type.
        /// </summary>
        public int AudioVolumeGlobal { get; set; }
        public bool AudioOnlyOnDesktop { get; set; }
        public WallpaperScaler WallpaperScaling { get; set; }
        public bool CefDiskCache { get; set; }
        public bool DebugMenu { get; set; }
        /// <summary>
        /// Fetch beta lively release updates from lively-beta repository.
        /// </summary>
        public bool TestBuild { get; set; }
        /// <summary>
        /// Not used currently.
        /// </summary>
        public AppTheme ApplicationTheme { get; set; }
        /// <summary>
        /// Set screen capture of wallpaper as lockscreen image.
        /// </summary>
        public bool LockScreenAutoWallpaper { get; set; }
        /// <summary>
        /// Set screen capture of wallpaper as desktop image.
        /// </summary>
        public bool DesktopAutoWallpaper { get; set; }
        public TaskbarTheme SystemTaskbarTheme { get; set; }
        public ScreensaverType ScreensaverType { get; set; }
        public WallpaperArrangement ScreensaverArragement { get; set; }
        public ScreensaverIdleTime ScreensaverIdleDelay { get; set; }
        public bool ScreensaverOledWarning { get; set; }
        public bool ScreensaverEmptyScreenShowBlack { get; set; }
        public bool ScreensaverLockOnResume { get; set; }
        public bool KeepAwakeUI { get; set; }
        public bool RememberSelectedScreen { get; set; }
        public bool IsUpdated { get; set; }
        public bool IsUpdatedNotify { get; set; }

        /// <summary>
        /// Notify user if plugin is missing.
        /// </summary>
        public bool IsScreensaverPluginNotify { get; set; }
        public string ApplicationThemeBackgroundPath { get; set; }
        public AppThemeBackground ApplicationThemeBackground { get; set; }
        public int ThemeBundleVersion { get; set; }
        /// <summary>
        /// Time in seconds between taskbar restart (hinting system instability) to stop Lively.
        /// </summary>
        public int TaskbarCrashTimeOutDelay { get; set; }

        public SettingsModel()
        {
            SavedURL = "https://www.youtube.com/watch?v=aqz-KE-bpKQ";
            ProcessMonitorAlgorithm = ProcessMonitorAlgorithm.foreground;
            WallpaperArrangement = WallpaperArrangement.per;
            ScreensaverArragement = WallpaperArrangement.per;
            ScreensaverType = ScreensaverType.wallpaper;
            AppVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            AppPreviousVersion = string.Empty;
            Startup = true;
            IsFirstRun = true;
            ControlPanelOpened = false;
            AppFocusPause = AppRules.ignore;
            AppFullscreenPause = AppRules.pause;
            BatteryPause = AppRules.ignore;
            VideoPlayer = LivelyMediaPlayer.mpv;
            VideoPlayerHwAccel = true;
            WebBrowser = LivelyWebBrowser.cef;
            GifPlayer = LivelyGifPlayer.mpv;
            PicturePlayer = LivelyPicturePlayer.mpv;

            WallpaperWaitTime = 20000; // 20sec
            ProcessTimerInterval = 500; //reduce to 250 for quicker response.
            StreamQuality = StreamQualitySuggestion.High;
            GenerateTile = true;
            LivelyZipGenerate = false;
            WaterMarkTile = true;
            IgnoreUpdateTag = null;

            //media scaling
            ScalerVideo = WallpaperScaler.fill;
            ScalerGif = WallpaperScaler.fill;
            GifCapture = true;
            MultiFileAutoImport = true;

            SafeShutdown = true;
            IsRestart = false;

            InputForward = InputForwardMode.mouse;
            MouseInputMovAlways = true;

            TileSize = 1;
            DisplayIdentification = DisplayIdentificationMode.deviceId;
            //SelectedDisplay = ScreenHelper.GetPrimaryScreen();
            UIMode = LivelyGUIState.normal;
            WallpaperDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lively Wallpaper", "Library");
            WallpaperDirMoveExistingWallpaperNewDir = true;
            SysTrayIcon = true;
            WebDebugPort = string.Empty;
            AutoDetectOnlineStreams = true;
            ExtractStreamMetaData = true;
            WallpaperBundleVersion = -1;
            ThemeBundleVersion = -1;
            AudioVolumeGlobal = 75;
            AudioOnlyOnDesktop = true;
            WallpaperScaling = WallpaperScaler.fill;
            CefDiskCache = false;
            DebugMenu = false;
            TestBuild = false;
            ApplicationTheme = AppTheme.Dark;
            RemoteDesktopPause = AppRules.pause;
            PowerSaveModePause = AppRules.ignore;
            LockScreenAutoWallpaper = false;
            DesktopAutoWallpaper = false;
            SystemTaskbarTheme = TaskbarTheme.none;
            ScreensaverIdleDelay = ScreensaverIdleTime.none;
            ScreensaverOledWarning = false;
            ScreensaverEmptyScreenShowBlack = true;
            ScreensaverLockOnResume = false;
            KeepAwakeUI = false;
            RememberSelectedScreen = true;
            IsUpdated = false;
            IsUpdatedNotify = false;
            IsScreensaverPluginNotify = true;
            ApplicationThemeBackgroundPath = null;
            ApplicationThemeBackground = AppThemeBackground.default_mica;
            TaskbarCrashTimeOutDelay = 30;
            Language = string.Empty;
        }
    }
}
