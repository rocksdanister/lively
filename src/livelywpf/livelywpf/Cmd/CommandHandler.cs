using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using CommandLine;

namespace livelywpf.Cmd
{
    class CommandHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [Verb("app", isDefault:true)]
        class AppOptions
        {
            [Option("showApp",
            Required = false,
            HelpText = "Open app window (true/false).")]
            public bool? ShowApp { get; set; }
        }

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
        class SetWallpaperOptions
        {
            [Option("file",
            Required = true,
            HelpText = "Path to the LivelyInfo.json file of the wallpaper.")]
            public string File { get; set; }

            [Option("monitor",
            Required = false,
            HelpText = "Index of the monitor to load the wallpaper on (optional).")]
            public int? Monitor { get; set; }
        }

        public static void ParseArgs(string[] args)
        {
            _ = CommandLine.Parser.Default.ParseArguments<ControlOptions, SetWallpaperOptions, AppOptions>(args)
                .MapResult(
                    (AppOptions opts) => RunAppOptions(opts),
                    (ControlOptions opts) => RunControlOptions(opts),
                    (SetWallpaperOptions opts) => RunSetWallpaperOptions(opts),
                    errs => HandleParseError(errs));
        }

        private static int RunAppOptions(AppOptions opts)
        {
            if (opts.ShowApp != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    if ((bool)opts.ShowApp)
                    {
                        Program.ShowMainWindow();
                    }
                    else
                    {
                        App.AppWindow?.Hide();
                    }
                }));
            }
            return 0;
        }

        private static int RunControlOptions(ControlOptions opts)
        {
            if (opts.Volume != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    Program.SettingsVM.GlobalWallpaperVolume = Clamp((int)opts.Volume, 0, 100);
                }));
            }

            if (opts.ShowIcons != null)
            {
                Helpers.DesktopUtil.SetDesktopIconVisibility((bool)opts.ShowIcons);
            }

            if (opts.Play != null)
            {
                //todo
            }
            return 0;
        }

        private static int RunSetWallpaperOptions(SetWallpaperOptions opts)
        {
            //todo
            return 0;
        }

        private static int HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var item in errs)
            {
                Logger.Error(item.ToString());
            }
            return 0;
        }

        #region helpers

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;

            return value;
        }

        #endregion //helpers
    }
}
