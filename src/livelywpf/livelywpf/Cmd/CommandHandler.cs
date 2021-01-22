using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace livelywpf.Cmd
{
    class CommandHandler
    {
        [Verb("control", HelpText = "Wallpaper control.")]
        class ControlOptions
        {
            [Option("volume",
            Required = false,
            HelpText = "Wallpaper audio level (0-100).")]
            public int? Volume { get; set; }

            [Option("play",
            Required = false,
            HelpText = "Wallpaper playback state (true/false).")]
            public bool? Play { get; set; }

            [Option("showIcons",
            Required = false,
            HelpText = "Desktop icons visibility (true/false).")]
            public bool? ShowIcons { get; set; }
        }

        [Verb("playWallpaper", HelpText = "Set Wallpaper.")]
        class PlayWallpaper
        {

        }

        public static void ParseArgs(string[] args)
        {
            _ = CommandLine.Parser.Default.ParseArguments<ControlOptions, PlayWallpaper>(args)
              .MapResult(
                  (ControlOptions opts) => RunControlOptionsAndReturnExitCode(opts),
                  (PlayWallpaper opts) => RunPlayWallpaperAndReturnExitCode(opts),
                  errs => 1);
        }

        private static int RunControlOptionsAndReturnExitCode(ControlOptions opts)
        {
            return 0;
        }

        private static int RunPlayWallpaperAndReturnExitCode(PlayWallpaper opts)
        {
            return 0;
        }
    }
}
