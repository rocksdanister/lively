using CommandLine;

namespace Lively.Player.CefSharp
{
    public class StartArgs
    {
        [Option("wallpaper-url",
        Required = true,
        HelpText = "The url/html-file to load.")]
        public string Url { get; set; }

        [Option("wallpaper-property",
        Required = false,
        Default = null,
        HelpText = "LivelyProperties.info filepath (SaveData/wpdata).")]
        public string Properties { get; set; }

        [Option("wallpaper-type",
        Required = true,
        HelpText = "LinkType class.")]
        public string Type { get; set; }

        [Option("wallpaper-display",
        Required = true,
        HelpText = "Wallpaper running display.")]
        public string DisplayDevice { get; set; }

        [Option("wallpaper-geometry",
        Required = false,
        HelpText = "Window size (WxH).")]
        public string Geometry { get; set; }

        [Option("wallpaper-audio",
        Default = false,
        HelpText = "Analyse system audio(visualiser data.)")]
        public bool AudioVisualizer { get; set; }

        [Option("wallpaper-debug",
        Required = false,
        HelpText = "Debugging port")]
        public string DebugPort { get; set; }

        [Option("wallpaper-cache",
        Required = false,
        HelpText = "disk cache path")]
        public string CachePath { get; set; }

        [Option("wallpaper-volume",
        Required = false,
        Default = 100,
        HelpText = "Audio volume")]
        public int Volume { get; set; }

        [Option("wallpaper-system-information",
        Default = false,
        Required = false,
        HelpText = "Lively hw monitor api")]
        public bool SysInfo { get; set; }

        [Option("wallpaper-system-nowplaying",
        Default = false,
        Required = false)]
        public bool NowPlaying { get; set; }

        [Option("wallpaper-pause-event",
        Required = false,
        HelpText = "Wallpaper playback changed notify")]
        public bool PauseEvent { get; set; }

        [Option("wallpaper-verbose-log",
        Required = false,
        HelpText = "Verbose Logging")]
        public bool VerboseLog { get; set; }
    }
}
