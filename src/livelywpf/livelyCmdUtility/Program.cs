using CommandLine;
using System;
using System.Collections.Generic;

/// <summary>
/// Small pgm to communicate with Lively for command line control.<br>
/// Single executable, can be added to system PATH for easy access.</br>
/// <br>Alternatively you can just directly msg through Lively executable instead.</br>
/// </summary>
namespace livelyCmdUtility
{   
    class Program
    {
        private static readonly string uniqueAppName = "LIVELY:DESKTOPWALLPAPERSYSTEM";
        private static readonly string pipeServerName = uniqueAppName + Environment.UserName;

        [Verb("app", isDefault: true, HelpText = "Application controls.")]
        class AppOptions
        {
            [Option("showApp",
            Required = false,
            HelpText = "Open app window (true/false).")]
            public bool? ShowApp { get; set; }

            [Option("showIcons",
            Required = false,
            HelpText = "Desktop icons visibility (true/false).")]
            public bool? ShowIcons { get; set; }

            [Option("volume",
            Required = false,
            HelpText = "Wallpaper audio level (0-100).")]
            public int? Volume { get; set; }

            [Option("play",
            Required = false,
            HelpText = "Wallpaper playback state (true/false).")]
            public bool? Play { get; set; }
        }

        [Verb("setwp", HelpText = "Apply wallpaper.")]
        class SetWallpaperOptions
        {
            [Option("file",
            Required = true,
            HelpText = "Path containing LivelyInfo.json project file.")]
            public string File { get; set; }

            [Option("monitor",
            Required = false,
            HelpText = "Index of the monitor to load the wallpaper on (optional).")]
            public int? Monitor { get; set; }
        }

        [Verb("closewp", HelpText = "Close wallpaper.")]
        class CloseWallpaperOptions
        {
            [Option("monitor",
            Required = true,
            HelpText = "Index of the monitor to close wallpaper, if -1 all running wallpapers are closed.")]
            public int? Monitor { get; set; }
        }

        [Verb("seekwp", HelpText = "Set wallpaper playback position.")]
        class SeekWallpaperOptions
        {
            [Option("value",
            Required = true,
            HelpText = "Seek percentage, optionally add +/- to seek from current position.")]
            public string Param { get; set; }

            [Option("monitor",
            Required = false,
            HelpText = "Index of the monitor to load the wallpaper on (optional).")]
            public int? Monitor { get; set; }
        }

        [Verb("setprop", HelpText = "Customise wallpaper.")]
        class CustomiseWallpaperOptions
        {
            [Option("property",
            Required = true,
            HelpText = "syntax: keyvalue=value")]
            public string Param { get; set; }

            [Option("monitor",
            Required = false,
            HelpText = "Index of the monitor to apply the wallpaper customisation.")]
            public int? Monitor { get; set; }
        }

        [Verb("screensaver", HelpText = "Screen saver control.")]
        class ScreenSaverOptions
        {
            [Option("show",
            Required = false,
            HelpText = "Show the ss full-screen, false cancels running ss.")]
            public bool? Show { get; set; }
        }

        static void Main(string[] args)
        {
            _ = CommandLine.Parser.Default.ParseArguments<AppOptions, SetWallpaperOptions, CustomiseWallpaperOptions, CloseWallpaperOptions, ScreenSaverOptions, SeekWallpaperOptions>(args)
                 .MapResult(
                     (AppOptions opts) => RunAppOptions(opts),
                     (SetWallpaperOptions opts) => RunSetWallpaperOptions(opts),
                     (CloseWallpaperOptions opts) => RunCloseWallpaperOptions(opts),
                     (SeekWallpaperOptions opts) => RunSeekWallpaperOptions(opts),
                     (CustomiseWallpaperOptions opts) => RunCustomiseWallpaperOptions(opts),
                     (ScreenSaverOptions opts) => RunScreenSaverOptions(opts),
                     errs => HandleParseError(errs));

            try
            {
                livelywpf.Helpers.PipeClient.SendMessage(pipeServerName, args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        private static object RunAppOptions(AppOptions opts)
        {
            return 0;
        }

        private static object RunSetWallpaperOptions(SetWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunCloseWallpaperOptions(CloseWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunSeekWallpaperOptions(SeekWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunCustomiseWallpaperOptions(CustomiseWallpaperOptions opts)
        {
            return 0;
        }

        private static object RunScreenSaverOptions(ScreenSaverOptions opts)
        {
            return 0;
        }

        private static object HandleParseError(IEnumerable<Error> errs)
        {
            return 0;
        }
    }
}
