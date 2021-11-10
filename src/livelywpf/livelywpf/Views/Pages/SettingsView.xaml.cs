using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace livelywpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : System.Windows.Controls.Page
    {
        public SettingsView()
        {
            InitializeComponent();
            //SettingsViewModel vm = new SettingsViewModel();
            this.DataContext = App.Services.GetRequiredService<SettingsViewModel>();
        }

        private void SettingsPageHost_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            global::livelyPages.SettingsPage userControl =
                windowsXamlHost.GetUwpInternalObject() as global::livelyPages.SettingsPage;

            if (userControl != null)
            {
                userControl.UIText = new livelyPages.SettingsPage.LocalizeText()
                {
                    TitleGeneral = Properties.Resources.TitleGeneral,
                    TitleMisc = Properties.Resources.TitleMisc,
                    TitlePerformance = Properties.Resources.TitlePerformance,
                    TitleWallpaper = Properties.Resources.TitleWallpaper,
                    TitleSettings = Properties.Resources.TitleSettings,
                    TitleWindowsStart = Properties.Resources.TitleWindowsStartup,
                    TipWindowsStart = Properties.Resources.TipWindowsStartup,
                    TitleLanguage = Properties.Resources.TitleLanguage,
                    TipLanguage = Properties.Resources.TipLanguage,
                    TitleTileSize = Properties.Resources.TitleTileSize,
                    TipTitleSize = Properties.Resources.TipTileSize,
                    TitleUIMode = Properties.Resources.TitleUIMode,
                    TipUIMode = Properties.Resources.TipUIMode,
                    TitleWallpaperDir = Properties.Resources.TitleWallpaperDir,
                    TipWallpaperDir = Properties.Resources.TipWallpaperDir,
                    TitleTheme = Properties.Resources.TitleAppTheme,
                    TipTheme = Properties.Resources.TipAppTheme,
                    TextTileSizeLarge = Properties.Resources.TextTileSizeLarge,
                    TextTileSizeNormal = Properties.Resources.TextTileSizeNormal,
                    TextTileSizeSmall = Properties.Resources.TextTileSizeSmall,
                    TextUImodeHeadless = Properties.Resources.TextUIHeadless,
                    TextUIModeLite = Properties.Resources.TextUILite,
                    TextUIModeNormal = Properties.Resources.TextUINormal,
                    TitleWallpaperDirMoveExisting = Properties.Resources.TitleWallpaperDirMoveExisting,
                    TextHelpTranslateLively = Properties.Resources.TextHelpTranslate,
                    //perf
                    TitleWallpaperPlayback = Properties.Resources.TitleWallpaperPlayback,
                    TitlePerfAppFullScreen = Properties.Resources.TitleAppFullScreen,
                    TipPerfAppFullScreen = Properties.Resources.TipAppFullScreen,
                    TitlePerfAppFocused = Properties.Resources.TitleAppFocus,
                    TipPerfAppFocused = Properties.Resources.TipAppFocus,
                    TitlePerfBattery = Properties.Resources.TitleBatteryPower,
                    TipPerfBattery = Properties.Resources.TipBatteryPower,
                    TitleDisplayPauseRule = Properties.Resources.TitleDisplayPauseRule,
                    TipDisplayPauseRule = Properties.Resources.TipDisplayPauseRule,
                    TitlePauseAlgorithm = Properties.Resources.TitlePauseAlgo,
                    TipPauseAlgorithm = Properties.Resources.TipPauseAlgorithm,
                    TitleAppRules = Properties.Resources.TitleAppRules,
                    TipAppRules = Properties.Resources.TipAppRules,
                    TextPerfPause = Properties.Resources.TextPerformancePause,
                    TextPerfNone = Properties.Resources.TextPerformanceNone,
                    TextPerfKill = Properties.Resources.TextPerformanceKill,
                    TextDisplayPauseRuleAll = Properties.Resources.TextDisplayPauseRuleAllScreen,
                    TextDisplayPauseRulePer = Properties.Resources.TextDisplayPauseRulePerScreen,
                    TextPauseAlgorithmAll = Properties.Resources.TextPauseAlgoAllProcess,
                    TextPauseAlgorithmForeground = Properties.Resources.TextPauseAlgoForegroundProcess,
                    TextLearnMore = Properties.Resources.TextLearnMore,
                    TextPauseAlgorithmExclusiveMode = Properties.Resources.TextPauseAlgoExclusiveMode,
                    TitlePowerSavePauseRule = Properties.Resources.TitlePowerSavingModePower,
                    TipPowerSavePauseRule = Properties.Resources.TipPowerSavingModePower,
                    TitleRemoteDesktopPauseRule = Properties.Resources.TitleRemoteDesktopPower,
                    TipRemoteDesktopPauseRule = Properties.Resources.TipRemoteDesktopPower,
                    //wallpaper
                    TitleWallpaperFit = Properties.Resources.TitleWallpaperFit,
                    TipWallpaperFit = Properties.Resources.TipWallpaperFit,
                    TextWallpaperFitFill = Properties.Resources.TextWallpaperFitFill,
                    TextWallpaperFitNone = Properties.Resources.TextWallpaperFitNone,
                    TextWallpaperFitUniform = Properties.Resources.TextWallpaperFitUniform,
                    TextWallpaperFitUniformFill = Properties.Resources.TextWallpaperFitUniformToFill,
                    TitleInteraction = Properties.Resources.TitleInteraction,
                    TitleWallpaperInput = Properties.Resources.TitleWallpaperInput,
                    TipWallpaperInput = Properties.Resources.TipWallpaperInput,
                    TitleMouseOnDesktop = Properties.Resources.TextMouseInteractOnDesktop,
                    TitleVideo = Properties.Resources.TitleVideo,
                    TitleVideoPlayer = Properties.Resources.TileVideoPlayer,
                    TipVideoPlayer = Properties.Resources.TipVideoPlayer,
                    TitleGifPlayer = Properties.Resources.TitleGifPlayer,
                    TipGifPlayer = Properties.Resources.TipGifPlayer,
                    TitleWebBrowser = Properties.Resources.TitleWebBrowser,
                    TitleBrowserEngine = Properties.Resources.TitleWebBrowserEngine,
                    TipBrowserEngine = Properties.Resources.TipWebBrowserEngine,
                    TitleDiskCache = Properties.Resources.TitleWebBrowserDiskCache,
                    TipDisCache = Properties.Resources.TipWebBrowserDiskCache,
                    TitleBrowserDebuggingPort = Properties.Resources.TitleBrowserDebuggingPort,
                    TipBrowserDebuggingPort = Properties.Resources.TipBrowserDebuggingPort,
                    TitleWallpaperStream = Properties.Resources.TitleStreamWallpaper,
                    TitleWallpaperStreamQuality = Properties.Resources.TitleStreamWallpaperVideoQuality,
                    TipWallpaperStreamQuality = Properties.Resources.TipStreamWallpaperVideoQuality,
                    TitleDetectWallpaperStream = Properties.Resources.TitleStreamWallpaperDetect,
                    TipDetectWallpaperStream = Properties.Resources.TipStreamWallpaperDetect,
                    TextWallpaperInputOff = Properties.Resources.TextOff,
                    TextWallpaperInputKeyboard = Properties.Resources.TextKeyboard,
                    TextWallpaperInputMouse = Properties.Resources.TextMouse,
                    TitleGpuDecode = Properties.Resources.TitleVideoHardwareDecode,
                    TipGpuDecode = Properties.Resources.TipVideoHardwareDecode,
                    //audio
                    TitleAudio = Properties.Resources.TitleAudio,
                    TitleMasterAudio = Properties.Resources.TitleAudioMaster,
                    TipMasterAudio = Properties.Resources.TipAudioMaster,
                    TitleAudioDesktop = Properties.Resources.TitleAudioDesktopOnly,
                    //system
                    TitleSystem = Properties.Resources.TitleSystem,
                    TitleDesktopPicture = Properties.Resources.TitleDesktopPicture,
                    TitleLockscreenPicture = Properties.Resources.TitleLockscreenPicture,
                    TitleScreensaverLockOnResume = Properties.Resources.TitleScreensaverLockOnResume,
                    TipDesktopPicture = Properties.Resources.TipDesktopPicture,
                    TipLockscreenPicture = Properties.Resources.TipLockscreenPicture,
                    TitleTaskbarTheme = Properties.Resources.TitleTaskbarTheme,
                    TipTaskbarTheme = Properties.Resources.TipTaskbarTheme,
                    TextTaskbarThemeClear = Properties.Resources.TextTaskbarThemeClear,
                    TextTaskbarThemeBlur = Properties.Resources.TextTaskbarThemeBlur,
                    TextTaskbarThemeColor = Properties.Resources.TextTaskbarThemeColor,
                    TextTaskbarThemeFluent = Properties.Resources.TextTaskbarThemeFluent,
                    TextTaskbarThemeWallpaper = Properties.Resources.TextTaskbarThemeWallpaper,
                    TextTaskbarThemeWallpaperFluent = Properties.Resources.TextTaskbarThemeWallpaperFluent,
                    TitleScreensaver = Properties.Resources.TitleScreensaver,
                    TipScreensaver = Properties.Resources.TipScreensaver,
                    TextMinutes = Properties.Resources.TextMinutes,
                    TextHours = Properties.Resources.TextHours,
                    //misc
                    TitleSysTrayIconHide = Properties.Resources.TitleHideSysTray,
                    TipSysTrayIconHide = Properties.Resources.TipHideSysTray,
                    TitleDebug = Properties.Resources.TitleDebug,
                    TipDebug = Properties.Resources.TipDebug,
                    TitleExportLog = Properties.Resources.TitleExportLogs,
                    TipExportLog = Properties.Resources.TipExportLogs,
                    TipSwitchBranch = Properties.Resources.TipSwitchSoftwareBranch,
                    //button localization
                    TextOn = Properties.Resources.TextOn,
                    TextOff = Properties.Resources.TextOff,
                };               
            }
        }
    }
}
