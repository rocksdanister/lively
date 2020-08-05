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

namespace livelysettings
{
    public class LocalizeText
    {
        public string TitleGeneral { get; set; }
        public string TitlePerformance { get; set; }
        public string TitleWallpaper { get; set; }
        public string TitleMisc { get; set; }
        public string TitleSettings { get; set; }

        //general 
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

        //wallpaper
        public string TitleInteraction { get; set; }
        public string TitleWallpaperInput { get; set; }
        public string TitleMouseOnDesktop { get; set; }
        public string TipWallpaperInput { get; set; }
        public string TitleVideo { get; set; }
        public string TitleVideoPlayer { get; set; }
        public string TipVideoPlayer { get; set; }
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

        //Misc
        public string TitleSysTrayIconHide { get; set; }
        public string TipSysTrayIconHide { get; set; }
    }

    public sealed partial class SettingsPage : UserControl
    {
        public LocalizeText UIText { get; set; }
        public SettingsPage()
        {
            this.InitializeComponent();
        }
    }
}
