using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lively.Common
{
    public static class Constants
    {
        public static class CommonPaths
        {
            public static string AppDataDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lively Wallpaper");
            public static string LogDir { get; } = Path.Combine(AppDataDir, "logs");
            public static string LogDirUI { get; } = Path.Combine(AppDataDir, "UI"); //temp use..
            public static string TempDir { get; } = Path.Combine(AppDataDir, "temp");
            public static string TempCefDir { get; } = Path.Combine(AppDataDir, "Cef");
            public static string TempWebView2Dir { get; } = Path.Combine(AppDataDir, "WebView2");
            public static string TempVideoDir { get; } = Path.Combine(AppDataDir, "Mpv");
            public static string AppRulesPath { get; } = Path.Combine(AppDataDir, "AppRules.json");
            public static string WallpaperLayoutPath { get; } = Path.Combine(AppDataDir, "WallpaperLayout.json");
            public static string ScreenSaverLayoutPath { get; } = Path.Combine(AppDataDir, "ScreenSaverLayout.json");
            public static string UserSettingsPath { get; } = Path.Combine(AppDataDir, "Settings.json");
            public static string MusicAppExclusionRulesPath { get; } = Path.Combine(AppDataDir, "MusicAppExclusionRules.json");
            public static string WeatherSettingsPath { get; } = Path.Combine(AppDataDir, "WeatherSettings.json");
            public static string ThemeDir { get; } = Path.Combine(AppDataDir, "Themes");
            public static string ThemeCacheDir { get; } = Path.Combine(Path.GetTempPath(), "Lively Wallpaper", "themes");
            public static string CefRootCacheDir { get; } = Path.Combine(Path.GetTempPath(), "Lively Wallpaper", "CEF");
            public static string TokensPath { get; } = Path.Combine(AppDataDir, "Tokens.dat");
        }

        /// <summary>
        /// To be combined with dynamic path Settings.WallpaperDir
        /// </summary>
        public static class CommonPartialPaths
        {
            public static string WallpaperInstallDir { get; } = "wallpapers";
            public static string WallpaperInstallTempDir { get; } = Path.Combine("SaveData", "wptmp");
            public static string WallpaperSettingsDir { get; } = Path.Combine("SaveData", "wpdata");
        }

        /// <summary>
        /// To be combined with Core base directory.
        /// </summary>
        public static class PlayerPartialPaths
        {
            public static string MpvDir { get; } = Path.Combine("plugins", "mpv");
            public static string MpvPath { get; } = Path.Combine(MpvDir, "mpv.exe");
            public static string CefSharpDir { get; } = Path.Combine("plugins", "cef");
            public static string CefSharpPath { get; } = Path.Combine(CefSharpDir, "Lively.Player.CefSharp.exe");
            public static string WebView2Dir { get; } = Path.Combine("plugins", "webview2");
            public static string WebView2Path { get; } = Path.Combine(WebView2Dir, "Lively.Player.WebView2.exe");
            public static string WmfDir { get; } = Path.Combine("plugins", "wmf");
            public static string WmfPath { get; } = Path.Combine(WmfDir, "Lively.PlayerWmf.exe");
            public static string VlcDir { get; } = Path.Combine("plugins", "vlc");
            public static string VlcPath { get; } = Path.Combine(VlcDir, "vlc.exe");
            //public static string LibVlcExtDir { get; } = Path.Combine("plugins", "libVLCPlayer");
            //public static string LibVlcExtPath { get; } = Path.Combine(LibVlcExtDir, "libVLCPlayer.exe");
            //public static string LibMpvExtDir { get; } = Path.Combine("plugins", "libMPVPlayer");
            //public static string LibMpvExtPath { get; } = Path.Combine(LibMpvExtDir, "libMPVPlayer.exe");
        }

        public static class MachineLearning
        {
            public static string BaseDir { get; } = Path.Combine(CommonPaths.AppDataDir, "ML");
            public static string MiDaSDir { get; } = Path.Combine(BaseDir, "Midas");
        }

        public static class SingleInstance
        {
            public static string UniqueAppName { get; } = "LIVELY:DESKTOPWALLPAPERSYSTEM";
            public static string PipeServerName { get; } = UniqueAppName + Environment.UserName; //backward compatibility < v1.9
            public static string GrpcPipeServerName { get; } = "Grpc_" + PipeServerName;
        }
        
        public static class ApplicationType
        {
            public static bool IsMSIX { get; } = new DesktopBridge.Helpers().IsRunningAsUwp();
            //todo: make compile-time flag.
            public static bool IsTestBuild { get; } = false;
        }

        public static class Weather
        {
            //todo: make compile-time flag.
            public static string OpenWeatherMapAPIKey = string.Empty;
        }
    }
}
