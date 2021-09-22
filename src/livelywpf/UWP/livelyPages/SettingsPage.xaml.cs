using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace livelyPages
{
    public sealed partial class SettingsPage : UserControl
    {
        public LocalizeText UIText { get; set; }

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void TextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
        }

        public class LocalizeText
        {
            public string TitleGeneral { get; set; }
            public string TitlePerformance { get; set; }
            public string TitleWallpaper { get; set; }
            public string TitleMisc { get; set; }
            public string TitleSettings { get; set; }

            //general 
            public string TextHelpTranslateLively { get; set; }
            public string TitleWindowsStart { get; set; }
            public string TipWindowsStart { get; set; }
            public string TitleLanguage { get; set; }
            public string TipLanguage { get; set; }
            public string TitleTileSize { get; set; }
            public string TipTitleSize { get; set; }
            public string TitleUIMode { get; set; }
            public string TipUIMode { get; set; }
            public string TitleWallpaperDir { get; set; }
            public string TipWallpaperDir { get; set; }
            public string TitleWallpaperDirMoveExisting { get; set; }
            public string TitleTheme { get; set; }
            public string TipTheme { get; set; }
            public string TextTileSizeSmall { get; set; }
            public string TextTileSizeNormal { get; set; }
            public string TextTileSizeLarge { get; set; }
            public string TextUIModeLite { get; set; }
            public string TextUIModeNormal { get; set; }
            public string TextUImodeHeadless { get; set; }

            //performance
            public string TitleWallpaperPlayback { get; set; }
            public string TitlePerfAppFullScreen { get; set; }
            public string TipPerfAppFullScreen { get; set; }
            public string TitlePerfAppFocused { get; set; }
            public string TipPerfAppFocused { get; set; }
            public string TitlePerfBattery { get; set; }
            public string TipPerfBattery { get; set; }
            public string TitleDisplayPauseRule { get; set; }
            public string TipDisplayPauseRule { get; set; }
            public string TitlePowerSavePauseRule { get; set; }
            public string TipPowerSavePauseRule { get; set; }
            public string TitleRemoteDesktopPauseRule { get; set; }
            public string TipRemoteDesktopPauseRule { get; set; }
            public string TitlePauseAlgorithm { get; set; }
            public string TipPauseAlgorithm { get; set; }
            public string TitleAppRules { get; set; }
            public string TipAppRules { get; set; }
            public string TextPerfPause { get; set; }
            public string TextPerfNone { get; set; }
            public string TextPerfKill { get; set; }
            public string TextDisplayPauseRulePer { get; set; }
            public string TextDisplayPauseRuleAll { get; set; }
            public string TextPauseAlgorithmForeground { get; set; }
            public string TextPauseAlgorithmAll { get; set; }
            public string TextPauseAlgorithmExclusiveMode { get; set; }
            public string TextLearnMore { get; set; }
            //wallpaper
            public string TitleWallpaperFit { get; set; }
            public string TipWallpaperFit { get; set; }
            public string TextWallpaperFitFill { get; set; }
            public string TextWallpaperFitNone { get; set; }
            public string TextWallpaperFitUniform { get; set; }
            public string TextWallpaperFitUniformFill { get; set; }
            public string TitleInteraction { get; set; }
            public string TitleWallpaperInput { get; set; }
            public string TitleMouseOnDesktop { get; set; }
            public string TipWallpaperInput { get; set; }
            public string TitleVideo { get; set; }
            public string TitleVideoPlayer { get; set; }
            public string TipVideoPlayer { get; set; }
            public string TitleGifPlayer { get; set; }
            public string TipGifPlayer { get; set; }
            public string TitleWebBrowser { get; set; }
            public string TitleBrowserEngine { get; set; }
            public string TipBrowserEngine { get; set; }
            public string TitleDiskCache { get; set; }
            public string TipDisCache { get; set; }
            public string TitleBrowserDebuggingPort { get; set; }
            public string TipBrowserDebuggingPort { get; set; }
            public string TitleWallpaperStream { get; set; }
            public string TitleWallpaperStreamQuality { get; set; }
            public string TipWallpaperStreamQuality { get; set; }
            public string TitleDetectWallpaperStream { get; set; }
            public string TipDetectWallpaperStream { get; set; }
            public string TextWallpaperInputOff { get; set; }
            public string TextWallpaperInputMouse { get; set; }
            public string TextWallpaperInputKeyboard { get; set; }
            public string TitleGpuDecode { get; set; }
            public string TipGpuDecode { get; set; }
            //Audio
            public string TitleAudio { get; set; }
            public string TitleMasterAudio { get; set; }
            public string TipMasterAudio { get; set; }
            public string TitleAudioDesktop { get; set; }
            //System
            public string TitleSystem { get; set; }
            public string TitleDesktopPicture { get; set; }
            public string TipDesktopPicture { get; set; }
            public string TitleLockscreenPicture { get; set; }
            public string TipLockscreenPicture { get; set; }
            public string TitleTaskbarTheme { get; set; }
            public string TitleScreensaverLockOnResume { get; set; }
            public string TipTaskbarTheme { get; set; }
            public string TextTaskbarThemeClear { get; set; }
            public string TextTaskbarThemeBlur { get; set; }
            public string TextTaskbarThemeFluent { get; set; }
            public string TextTaskbarThemeColor { get; set; }
            public string TextTaskbarThemeWallpaper { get; set; }
            public string TextTaskbarThemeWallpaperFluent { get; set; }
            public string TitleScreensaver { get; set; }
            public string TipScreensaver { get; set; }
            public string TextMinutes { get; set; }
            public string TextHours { get; set; }
            //Misc
            public string TitleSysTrayIconHide { get; set; }
            public string TipSysTrayIconHide { get; set; }
            public string TitleDebug { get; set; }
            public string TipDebug { get; set; }
            public string TitleExportLog { get; set; }
            public string TipExportLog { get; set; }
            public string TipSwitchBranch { get; set; }

            //Button localization
            public string TextOn { get; set; }
            public string TextOff { get; set; }
        }
    }
}
