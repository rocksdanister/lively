using CommandLine;

namespace Lively.Player.Wmf
{
    public class StartArgs
    {
        [Option("path",
        Required = true,
        HelpText = "The file/video stream path.")]
        public string FilePath { get; set; }

        [Option("stretch",
        Required = false,
        Default = 0,
        HelpText = "Video Scaling algorithm.")]
        public int StretchMode { get; set; }

        [Option("volume",
        Required = false,
        Default = 100,
        HelpText = "Audio volume")]
        public int Volume { get; set; }

        [Option("property",
        Required = false,
        Default = null,
        HelpText = "LivelyProperties.json filepath.")]
        public string Properties { get; set; }

        [Option("verbose-log",
        Required = false,
        HelpText = "Verbose Logging")]
        public bool VerboseLog { get; set; }
    }
}
