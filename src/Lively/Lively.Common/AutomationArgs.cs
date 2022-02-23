using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common
{
    public static class AutomationArgs
    {
        [Verb("app", isDefault: true, HelpText = "Application controls.")]
        public class AppOptions
        {
            [Option("showApp",
            Required = false,
            HelpText = "Open app window (true/false).")]
            public bool? ShowApp { get; set; }
            /*
            [Option("showTray",
            Required = false,
            HelpText = "Tray-icon visibility (true/false).")]
            public bool? ShowTray { get; set; }
            */
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

            [Option("startup",
            Required = false,
            HelpText = "Start with Windows (true/false).")]
            public bool? Startup { get; set; }
        }

        [Verb("setwp", HelpText = "Apply wallpaper.")]
        public class SetWallpaperOptions
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
        public class CloseWallpaperOptions
        {
            [Option("monitor",
            Required = true,
            HelpText = "Index of the monitor to close wallpaper, if -1 all running wallpapers are closed.")]
            public int? Monitor { get; set; }
        }

        [Verb("seekwp", HelpText = "Set wallpaper playback position.")]
        public class SeekWallpaperOptions
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
        public class CustomiseWallpaperOptions
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
        public class ScreenSaverOptions
        {
            [Option("preview",
            Required = false,
            HelpText = "Show the ss in the ss selection dialog box, number represents the handle to the parent's window.")]
            public int? Preview { get; set; }

            [Option("configure",
            Required = false,
            HelpText = "Show the ss configuration dialog box.")]
            public int? Configure { get; set; }


            [Option("show",
            Required = false,
            HelpText = "Show the ss full-screen, false cancels running ss.")]
            public bool? Show { get; set; }
        }
    }
}
