using Lively.Common;

namespace Lively.Models
{
    public interface ISettingsModel
    {
        AppRulesEnum AppFocusPause { get; set; }
        AppRulesEnum AppFullscreenPause { get; set; }
        AppTheme ApplicationTheme { get; set; }
        string AppVersion { get; set; }
        bool AudioOnlyOnDesktop { get; set; }
        int AudioVolumeGlobal { get; set; }
        bool AutoDetectOnlineStreams { get; set; }
        AppRulesEnum BatteryPause { get; set; }
        bool CefDiskCache { get; set; }
        bool ControlPanelOpened { get; set; }
        bool DebugMenu { get; set; }
        bool DesktopAutoWallpaper { get; set; }
        DisplayIdentificationMode DisplayIdentification { get; set; }
        DisplayPauseEnum DisplayPauseSettings { get; set; }
        bool ExtractStreamMetaData { get; set; }
        bool GenerateTile { get; set; }
        bool GifCapture { get; set; }
        LivelyGifPlayer GifPlayer { get; set; }
        LivelyPicturePlayer PicturePlayer { get; set; }
        string IgnoreUpdateTag { get; set; }
        InputForwardMode InputForward { get; set; }
        bool IsFirstRun { get; set; }
        bool IsRestart { get; set; }
        string Language { get; set; }
        LivelyGUIState UIMode { get; set; }
        bool LivelyZipGenerate { get; set; }
        bool LiveTile { get; set; }
        bool LockScreenAutoWallpaper { get; set; }
        bool MouseInputMovAlways { get; set; }
        bool MultiFileAutoImport { get; set; }
        AppRulesEnum PowerSaveModePause { get; set; }
        ProcessMonitorAlgorithm ProcessMonitorAlgorithm { get; set; }
        int ProcessTimerInterval { get; set; }
        AppRulesEnum RemoteDesktopPause { get; set; }
        bool SafeShutdown { get; set; }
        string SavedURL { get; set; }
        WallpaperScaler ScalerGif { get; set; }
        WallpaperScaler ScalerVideo { get; set; }
        bool ScreensaverEmptyScreenShowBlack { get; set; }
        ScreensaverIdleTime ScreensaverIdleDelay { get; set; }
        bool ScreensaverLockOnResume { get; set; }
        bool ScreensaverOledWarning { get; set; }
        DisplayMonitor SelectedDisplay { get; set; }
        bool Startup { get; set; }
        StreamQualitySuggestion StreamQuality { get; set; }
        TaskbarTheme SystemTaskbarTheme { get; set; }
        bool SysTrayIcon { get; set; }
        bool TestBuild { get; set; }
        int TileSize { get; set; }
        LivelyMediaPlayer VideoPlayer { get; set; }
        bool VideoPlayerHwAccel { get; set; }
        WallpaperArrangement WallpaperArrangement { get; set; }
        int WallpaperBundleVersion { get; set; }
        string WallpaperDir { get; set; }
        bool WallpaperDirMoveExistingWallpaperNewDir { get; set; }
        WallpaperScaler WallpaperScaling { get; set; }
        int WallpaperWaitTime { get; set; }
        bool WaterMarkTile { get; set; }
        LivelyWebBrowser WebBrowser { get; set; }
        string WebDebugPort { get; set; }
        bool KeepAwakeUI { get; set; }
        bool RememberSelectedScreen { get; set; }
        bool IsUpdated { get; set; }
        string ApplicationThemeBackgroundPath { get; set; }
        AppThemeBackground ApplicationThemeBackground { get; set; }
        int ThemeBundleVersion { get; set; }
    }
}