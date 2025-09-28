using CommandLine;
using Lively.Models.Enums;

namespace Lively.Player.Vlc
{
    public class StartArgs
    {
        [Option("wallpaper-path",
        Required = true,
        HelpText = "The file/video stream path.")]
        public string FilePath { get; set; }

        [Option("wallpaper-volume",
        Required = false,
        Default = 100,
        HelpText = "Audio volume")]
        public int Volume { get; set; }

        [Option("wallpaper-hardware-decoding",
        Default = true,
        HelpText = "Use hardware-decoding.)")]
        public bool HardwareDecoding { get; set; }

        [Option("wallpaper-property",
        Required = false,
        Default = null,
        HelpText = "LivelyProperties.json filepath.")]
        public string Properties { get; set; }

        [Option("wallpaper-geometry",
        Required = false,
        HelpText = "Window size (WxH).")]
        public string Geometry { get; set; }

        [Option("wallpaper-color-scheme",
        Required = false,
        HelpText = "Set preferred theme color.")]
        public AppTheme Theme { get; set; }

        [Option("wallpaper-verbose-log",
        Required = false,
        HelpText = "Verbose Logging")]
        public bool VerboseLog { get; set; }
    }
}
