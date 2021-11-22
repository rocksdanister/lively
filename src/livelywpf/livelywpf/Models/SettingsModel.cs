using livelywpf.Core;
using livelywpf.Helpers;
using System;
using System.Globalization;
using System.IO;

namespace livelywpf.Models
{

    [Serializable]
    public class SettingsModel : ISettingsModel
    {
        public string AppVersion { get; set; }
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
        public AppRulesEnum AppFocusPause { get; set; }

        public AppRulesEnum AppFullscreenPause { get; set; }
        public AppRulesEnum BatteryPause { get; set; }
        public AppRulesEnum RemoteDesktopPause { get; set; }
        public AppRulesEnum PowerSaveModePause { get; set; }
        public DisplayPauseEnum DisplayPauseSettings { get; set; }
        public ProcessMonitorAlgorithm ProcessMonitorAlgorithm { get; set; }
        /// <summary>
        /// Show animatd library tiles.
        /// </summary>
        public bool LiveTile { get; set; }
        public System.Windows.Media.Stretch ScalerVideo { get; set; }
        public System.Windows.Media.Stretch ScalerGif { get; set; }
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
        public LivelyWebBrowser WebBrowser { get; set; }
        public bool GifCapture { get; set; }
        public bool MultiFileAutoImport { get; set; }
        public ILivelyScreen SelectedDisplay { get; set; }
        public LivelyGUIState LivelyGUIRendering { get; set; }
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
        public ScreensaverIdleTime ScreensaverIdleWait { get; set; }
        public bool ScreensaverOledWarning { get; set; }
        public bool ScreensaverEmptyScreenShowBlack { get; set; }
        public bool ScreensaverLockOnResume { get; set; }

        public SettingsModel()
        {
            SavedURL = "https://www.youtube.com/watch?v=aqz-KE-bpKQ";
            ProcessMonitorAlgorithm = ProcessMonitorAlgorithm.foreground;
            WallpaperArrangement = WallpaperArrangement.per;
            AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Startup = true;
            IsFirstRun = true;
            ControlPanelOpened = false;
            AppFocusPause = AppRulesEnum.ignore;
            AppFullscreenPause = AppRulesEnum.pause;
            BatteryPause = AppRulesEnum.ignore;
            VideoPlayer = LivelyMediaPlayer.mpv;
            VideoPlayerHwAccel = true;
            WebBrowser = LivelyWebBrowser.cef;
            GifPlayer = LivelyGifPlayer.mpv;

            WallpaperWaitTime = 20000; // 20sec
            ProcessTimerInterval = 500; //reduce to 250 for quicker response.
            StreamQuality = StreamQualitySuggestion.High;
            GenerateTile = true;
            LivelyZipGenerate = false;
            WaterMarkTile = true;
            IgnoreUpdateTag = null;

            //media scaling
            ScalerVideo = System.Windows.Media.Stretch.Fill;
            ScalerGif = System.Windows.Media.Stretch.Fill;
            GifCapture = true;
            MultiFileAutoImport = true;

            SafeShutdown = true;
            IsRestart = false;

            InputForward = InputForwardMode.mouse;
            MouseInputMovAlways = true;

            TileSize = 1;
            DisplayIdentification = DisplayIdentificationMode.deviceId;
            SelectedDisplay = ScreenHelper.GetPrimaryScreen();
            LivelyGUIRendering = LivelyGUIState.lite;
            WallpaperDir = Path.Combine(Constants.CommonPaths.AppDataDir, "Library");
            WallpaperDirMoveExistingWallpaperNewDir = false;
            SysTrayIcon = true;
            WebDebugPort = string.Empty;
            AutoDetectOnlineStreams = true;
            ExtractStreamMetaData = true;
            WallpaperBundleVersion = -1;
            AudioVolumeGlobal = 75;
            AudioOnlyOnDesktop = true;
            WallpaperScaling = WallpaperScaler.fill;
            CefDiskCache = false;
            DebugMenu = false;
            TestBuild = false;
            ApplicationTheme = AppTheme.Dark;
            RemoteDesktopPause = AppRulesEnum.pause;
            PowerSaveModePause = AppRulesEnum.ignore;
            LockScreenAutoWallpaper = false;
            DesktopAutoWallpaper = false;
            SystemTaskbarTheme = TaskbarTheme.none;
            ScreensaverIdleWait = ScreensaverIdleTime.none;
            ScreensaverOledWarning = false;
            ScreensaverEmptyScreenShowBlack = true;
            ScreensaverLockOnResume = false;

            try
            {
                Language = CultureInfo.CurrentCulture.Name;
            }
            catch (ArgumentNullException)
            {
                Language = "en";
            }
        }
    }
}
