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
        [Description(">= 8K")]
        best,
        [Description("3840 x 2160, 4K")]
        h2160p,
        [Description("2560 x 1440, 2K")]
        h1440p,
        [Description("1920 x 1080")]
        h1080p,
        [Description("1280 x 720")]
        h720p,
        [Description("840 x 480")]
        h480p
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
        [Description("All Wallpapers Paused")]
        paused,
        [Description("Normal")]
        play
    }
}
