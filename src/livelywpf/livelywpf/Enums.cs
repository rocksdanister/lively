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
        [Description("Unity Audio Visualiser")]
        unityaudio,
        [Description("Video Streams")]
        videostream
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
        all
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
        screenClass,
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
        libvlc,
        libvlcExt,
        libmpv,
        libmpvExt
    }

}
