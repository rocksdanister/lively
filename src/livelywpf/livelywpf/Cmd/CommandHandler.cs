using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using CommandLine;
using Newtonsoft.Json.Linq;

namespace livelywpf.Cmd
{
    class CommandHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [Verb("app", isDefault:true, HelpText = "Application controls.")]
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
            HelpText = "Seek percentage from current position.")]
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

        public static void ParseArgs(string[] args)
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
        }

        private static int RunAppOptions(AppOptions opts)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                if (opts.ShowApp != null)
                {
                    if ((bool)opts.ShowApp)
                    {
                        Program.ShowMainWindow();
                    }
                    else
                    {
                        App.AppWindow?.HideWindow();
                    }
                }

                if (opts.Volume != null)
                {
                    Program.SettingsVM.GlobalWallpaperVolume = Clamp((int)opts.Volume, 0, 100);
                }

                if (opts.Play != null)
                {
                    Core.Playback.WallpaperPlaybackState = (bool)opts.Play ? PlaybackState.play : PlaybackState.paused;
                }

            }));

            if (opts.ShowIcons != null)
            {
                Helpers.DesktopUtil.SetDesktopIconVisibility((bool)opts.ShowIcons);
            }
            return 0;
        }

        private static int RunSetWallpaperOptions(SetWallpaperOptions opts)
        {
            if (opts.File != null)
            {
                //todo: Rewrite fn in libraryvm
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    if (Directory.Exists(opts.File))
                    {
                        //Folder containing LivelyInfo.json file.
                        Core.LivelyScreen screen = opts.Monitor != null ?
                            ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();
                        var libraryItem = Program.LibraryVM.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath != null && x.LivelyInfoFolderPath.Equals(opts.File));
                        if (libraryItem != null && screen != null)
                        {
                            SetupDesktop.SetWallpaper(libraryItem, screen);
                        }
                    }
                    else if (File.Exists(opts.File))
                    {
                        //File path, outside of Lively folder.
                        //todo: If not present in library -> load wallpaper file(video, website etc..) -> create quick thumbnail without user input -> set as wallpaper.
                        //related: https://github.com/rocksdanister/lively/issues/273 (Batch wallpaper import.) 
                        Core.LivelyScreen screen = opts.Monitor != null ?
                            ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();
                        var libraryItem = Program.LibraryVM.LibraryItems.FirstOrDefault(x => x.FilePath != null && x.FilePath.Equals(opts.File));
                        if (libraryItem != null && screen != null)
                        {
                            SetupDesktop.SetWallpaper(libraryItem, screen);
                        }
                    }
                }));
            }
            return 0;
        }

        private static int RunCloseWallpaperOptions(CloseWallpaperOptions opts)
        {
            if (opts.Monitor != null)
            {
                var id = (int)opts.Monitor;
                if (id == -1 ||
                    Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate ||
                    Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                {
                    SetupDesktop.CloseAllWallpapers();
                }
                else
                {
                    var screen = ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == (id).ToString());
                    if (screen != null)
                    {
                        SetupDesktop.CloseWallpaper(screen);
                    }
                }
            }
            return 0;
        }

        private static int RunSeekWallpaperOptions(SeekWallpaperOptions opts)
        {
            Core.LivelyScreen screen = opts.Monitor != null ?
                ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();
            if (screen != null)
            {
                var wp = SetupDesktop.Wallpapers.Find(x => ScreenHelper.ScreenCompare(x.GetScreen(), screen, DisplayIdentificationMode.deviceId));
                if (wp != null)
                {
                    if (opts.Param != null)
                    {
                        if ((opts.Param.StartsWith('+') || opts.Param.StartsWith('-')))
                        {
                            if (float.TryParse(opts.Param, out float val))
                            {
                                wp.SetPlaybackPos(Clamp(val, -100, 100), Core.PlaybackPosType.relativePercent);
                            }
                        }
                        else
                        {
                            if (float.TryParse(opts.Param, out float val))
                            {
                                wp.SetPlaybackPos(Clamp(val, 0, 100), Core.PlaybackPosType.absolutePercent);
                            }
                        }
                    }
                }
            }
            return 0;
        }

        private static int RunCustomiseWallpaperOptions(CustomiseWallpaperOptions opts)
        {
            if (opts.Param != null)
            {
                Core.LivelyScreen screen = opts.Monitor != null ?
                    ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();

                if (screen != null)
                {
                    try
                    {
                        var wp = SetupDesktop.Wallpapers.Find(x => ScreenHelper.ScreenCompare(x.GetScreen(), screen, DisplayIdentificationMode.deviceId));
                        //delimiter
                        var tmp = opts.Param.Split("=");
                        string name = tmp[0], val = tmp[1], ctype = null;
                        var lp = JObject.Parse(File.ReadAllText(wp.GetLivelyPropertyCopyPath()));
                        foreach (var item in lp)
                        {
                            //Searching for the given control in the json file.
                            if (item.Key.ToString().Equals(name, StringComparison.Ordinal))
                            {
                                ctype = item.Value["type"].ToString();
                                val = ctype.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase) ?
                                    Path.Combine(item.Value["folder"].ToString(), val) : val;
                                break;
                            }
                        }

                        ctype = (ctype == null && name.Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase)) ? "button" : ctype;
                        if (ctype != null)
                        {
                            if (ctype.Equals("button", StringComparison.OrdinalIgnoreCase))
                            {
                                if (name.Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (Cef.LivelyPropertiesView.RestoreOriginalPropertyFile(wp.GetWallpaperData(), wp.GetLivelyPropertyCopyPath()))
                                    {
                                        SetupDesktop.SendMessageWallpaper(screen, "lively:customise button lively_default_settings_reload 1");
                                    }
                                }
                                else
                                {
                                    SetupDesktop.SendMessageWallpaper(screen, "lively:customise " + ctype + " " + name + " " + val);
                                }
                            }
                            else
                            {
                                if (ctype.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                                {
                                    SetupDesktop.SendMessageWallpaper(screen, "lively:customise " + ctype + " " + name + " " + (val == "true"));
                                    lp[name]["value"] = (val == "true");
                                }
                                else if (ctype.Equals("slider", StringComparison.OrdinalIgnoreCase))
                                {
                                    SetupDesktop.SendMessageWallpaper(screen, "lively:customise " + ctype + " " + name + " " + double.Parse(val));
                                    lp[name]["value"] = double.Parse(val);
                                }
                                else if (ctype.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                                {
                                    SetupDesktop.SendMessageWallpaper(screen, "lively:customise " + ctype + " " + name + " " + int.Parse(val));
                                    lp[name]["value"] = int.Parse(val);
                                }
                                else if (ctype.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                                {
                                    SetupDesktop.SendMessageWallpaper(screen, "lively:customise " + ctype + " " + name + " " + "\"" + val + "\"");
                                    lp[name]["value"] = Path.GetFileName(val);
                                }
                                else if (ctype.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                                {
                                    SetupDesktop.SendMessageWallpaper(screen, "lively:customise " + ctype + " " + name + " " + "\"" + val + "\"");
                                    lp[name]["value"] = val;
                                }
                                else if (ctype.Equals("color", StringComparison.OrdinalIgnoreCase))
                                {
                                    SetupDesktop.SendMessageWallpaper(screen, "lively:customise " + ctype + " " + name + " " + val);
                                    lp[name]["value"] = val;
                                }

                                //Saving changes to copy file.
                                Cef.LivelyPropertiesJSON.SaveLivelyProperties(wp.GetLivelyPropertyCopyPath(), lp);
                            }
                        }
                    }
                    catch { }
                }
            }
            return 0;
        }

        private static int RunScreenSaverOptions(ScreenSaverOptions opts)
        {
            if (opts.Show != null)
            {
                if (opts.Show == true)
                {
                    Helpers.ScreenSaverService.Instance.Start();
                }
                else
                {
                    Helpers.ScreenSaverService.Instance.Stop();
                }
            }

            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                if (opts.Configure != null)
                {
                    App.AppWindow?.ShowControlPanelDialog();
                }

                if (opts.Preview != null)
                {
                    Helpers.ScreenSaverService.Instance.CreatePreview(new IntPtr((int)opts.Preview));
                }
            }));
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
