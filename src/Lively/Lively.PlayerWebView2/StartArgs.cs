using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.PlayerWebView2
{
    public class StartArgs
    {
        [Option("url",
        Required = true,
        HelpText = "The url/html-file to load.")]
        public string Url { get; set; }

        [Option("property",
        Required = false,
        Default = null,
        HelpText = "LivelyProperties.info filepath (SaveData/wpdata).")]
        public string Properties { get; set; }

        [Option("type",
        Required = true,
        HelpText = "LinkType class.")]
        public string Type { get; set; }

        [Option("display",
        Required = false,
        HelpText = "Wallpaper running display.")]
        public string DisplayDevice { get; set; }

        [Option("geometry",
        Required = false,
        HelpText = "Window size (WxH).")]
        public string Geometry { get; set; }

        [Option("audio",
        Default = false,
        HelpText = "Analyse system audio(visualiser data.)")]
        public bool AudioVisualizer { get; set; }

        [Option("debug",
        Required = false,
        HelpText = "Debugging port")]
        public string DebugPort { get; set; }

        [Option("cache",
        Required = false,
        HelpText = "disk cache path")]
        public string CachePath { get; set; }

        [Option("volume",
        Required = false,
        Default = 100,
        HelpText = "Audio volume")]
        public int Volume { get; set; }

        [Option("system-information",
        Default = false,
        Required = false,
        HelpText = "Lively hw monitor api")]
        public bool SysInfo { get; set; }

        [Option("system-nowplaying", 
        Default = false, 
        Required = false)]
        public bool NowPlaying { get; set; }

        [Option("pause-event",
        Required = false,
        HelpText = "Wallpaper playback changed notify")]
        public bool PauseEvent { get; set; }

        [Option("verbose-log",
        Required = false,
        HelpText = "Verbose Logging")]
        public bool VerboseLog { get; set; }
    }
}
