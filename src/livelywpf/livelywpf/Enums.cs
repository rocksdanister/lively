using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace livelywpf
{
    public enum WallpaperType
    {
        [Description("Application")]
        app,
        [Description("Webpage")]
        web,
        [Description("Webpage Audio Visualiser")]
        webaudio,
        [Description("Webpage Link")] //"Type" tab only, not for "Library"! 
        url,
        [Description("Bizhawk Emulator")]
        bizhawk,
        [Description("Unity Game")]
        unity,
        [Description("Godot Game")]
        godot,
        [Description("Video")]
        video,
        [Description("Animated Gif")]
        gif,
        //backward compatibility with lively pre v1.0
        [Description("Unity Audio Visualiser")]
        unityaudio,
        [Description("Video Streams")]
        videostream,
        [Description("Static picture")]
        picture,
        /*
        [Description("Animated sequence HEIC file")]
        heic
        */
    }

    public enum AudioPauseAlgorithm
    {
        none,
        cursor,
        primaryscreen,
    }
    
    /// <summary>
    /// Per application pause behavior.
    /// </summary>
    public enum AppRulesEnum
    {
        [Description("Pause")]
        pause,
        [Description("Ignore")]
        ignore,
        [Description("Kill(Free Memory)")]
        kill
    }

    public enum DisplayPauseEnum
    {
        perdisplay,
        all
    }

    public enum ProcessMonitorAlgorithm
    {
        foreground,
        all,
        gamemode
    }

    /// <summary>
    /// Suggested stream quality, youtube-dl will pick upto the suggested resolution.
    /// </summary>
    public enum StreamQualitySuggestion
    {
        [Description("144p")]
        Lowest,
        [Description("240p")]
        Low,
        [Description("360p")]
        LowMedium,
        [Description("480p")]
        Medium,
        [Description("720p")]
        MediumHigh,
        [Description("1080p")]
        High,
        [Description("1081p")]
        Highest
    }

    /// <summary>
    /// Wallpaper screen layout.
    /// </summary>
    public enum WallpaperArrangement
    {
        [Description("Per Display")]
        per,
        [Description("Span Across All Display(s)")]
        span,
        [Description("Same wp for all Display(s)")]
        duplicate
    }

    public enum LivelyGUIState
    {
        normal,
        lite,
        headless
    }

    public enum InputForwardMode
    {
        off,
        mouse,
        mousekeyboard,
    }

    public enum DisplayIdentificationMode
    {
        deviceName,
        deviceId,
        screenLayout
    }

    public enum PlaybackState
    {
        [Description("Normal")]
        play,
        [Description("All Wallpapers Paused")]
        paused
    }

    public enum LivelyMediaPlayer
    {
        wmf,
        //depreciated
        libvlc,
        libvlcExt,
        //depreciated
        libmpv,
        libmpvExt,
        mpv,
        vlc
    }

    public enum LivelyGifPlayer
    {
        win10Img,
        libmpvExt,
        mpv
    }

    public enum LivelyPicturePlayer
    {
        win10Img,
        picture
    }

    public enum LivelyWebBrowser
    {
        cef,
        webview2
    }

    /// <summary>
    /// Same as: https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.stretch?view=netcore-3.1
    /// </summary>
    public enum WallpaperScaler
    {
        none,
        fill,
        uniform,
        uniformFill,
        auto
    }

    public enum AppTheme
    {
        Auto,
        Light,
        Dark,
    }

    public enum ScreensaverIdleTime
    {
        none,
        min1,
        min2,
        min3,
        min5,
        min10,
        min15,
        min20,
        min25,
        min30,
        min45,
        min60,
        min120
    }

    public enum ScreensaverMode
    {
        [Description("Same as running wallpaper.")]
        wallpaper,
        [Description("Different from running wallpaper.")]
        standalone
    }

    /// <summary>
    /// System taskbar theme
    /// </summary>
    public enum TaskbarTheme
    {
        [Description("System default.")]
        none,
        [Description("Fully transparent.")]
        clear,
        [Description("Blurred.")]
        blur,
        [Description("Fluent design.")]
        fluent,
        [Description("User defined color.")]
        color,
        [Description("Live wallpaper color avg.")]
        wallpaper,
        [Description("Live wallpaper color avg fluet style.")]
        wallpaperFluent,
    }
}
