using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using CommandLine;
using livelywpf.Core.API;
using livelywpf.Helpers;
using livelywpf.Helpers.Files;
using livelywpf.Helpers.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using livelywpf.Models;
using livelywpf.Core;
using livelywpf.Core.Suspend;
using livelywpf.Services;
using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using livelywpf.Views;
using livelywpf.Views.LivelyProperty;
using livelywpf.Helpers.Storage;

namespace livelywpf.Cmd
{
    //Doc: https://github.com/rocksdanister/lively/wiki/Command-Line-Controls
    public class CommandHandler : ICommandHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [Verb("app", isDefault: true, HelpText = "Application controls.")]
        private class AppOptions
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
        private class SetWallpaperOptions
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
        private class CloseWallpaperOptions
        {
            [Option("monitor",
            Required = true,
            HelpText = "Index of the monitor to close wallpaper, if -1 all running wallpapers are closed.")]
            public int? Monitor { get; set; }
        }

        [Verb("seekwp", HelpText = "Set wallpaper playback position.")]
        private class SeekWallpaperOptions
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
        private class CustomiseWallpaperOptions
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
        private class ScreenSaverOptions
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

        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IScreensaverService screenSaver;
        private readonly IPlayback playbackMonitor;
        private readonly LibraryViewModel libraryVm;
        private readonly SettingsViewModel settingsVm;
        private readonly MainWindow appWindow;

        public CommandHandler(IUserSettingsService userSettings, 
            IDesktopCore desktopCore, 
            IScreensaverService screenSaver, 
            IPlayback playbackMonitor, 
            LibraryViewModel libraryVm, 
            SettingsViewModel settingsVm, 
            MainWindow appWindow)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.screenSaver = screenSaver;
            this.playbackMonitor = playbackMonitor;
            this.libraryVm = libraryVm;
            this.settingsVm = settingsVm;
            this.appWindow = appWindow;
        }

        public void ParseArgs(string[] args)
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

        private int RunAppOptions(AppOptions opts)
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
                        if (appWindow.IsVisible)
                        {
                            appWindow?.HideWindow();
                        }
                    }
                }

                if (opts.Volume != null)
                {
                    settingsVm.GlobalWallpaperVolume = Clamp((int)opts.Volume, 0, 100);
                }

                if (opts.Play != null)
                {
                    playbackMonitor.WallpaperPlayback = (bool)opts.Play ? PlaybackState.play : PlaybackState.paused;
                }

            }));

            if (opts.ShowIcons != null)
            {
                DesktopUtil.SetDesktopIconVisibility((bool)opts.ShowIcons);
            }
            return 0;
        }

        private int RunSetWallpaperOptions(SetWallpaperOptions opts)
        {
            if (opts.File != null)
            {
                //todo: Rewrite fn in libraryvm
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    if (Directory.Exists(opts.File))
                    {
                        //Folder containing LivelyInfo.json file.
                        var screen = opts.Monitor != null ?
                            ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();
                        var libraryItem = libraryVm.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath != null && x.LivelyInfoFolderPath.Equals(opts.File));
                        if (libraryItem != null && screen != null)
                        {
                            desktopCore.SetWallpaper(libraryItem, screen);
                        }
                    }
                    else if (File.Exists(opts.File))
                    {
                        var screen = opts.Monitor != null ?
                            ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();
                        var libraryItem = libraryVm.LibraryItems.FirstOrDefault(x => x.FilePath != null && x.FilePath.Equals(opts.File));
                        if (screen != null)
                        {
                            if (libraryItem != null)
                            {
                                desktopCore.SetWallpaper(libraryItem, screen);
                            }
                            else
                            {
                                Logger.Info("Wallpaper not found in library, importing as new file.");
                                WallpaperType type = FileFilter.GetLivelyFileType(opts.File);
                                switch (type)
                                {
                                    case WallpaperType.web:
                                    case WallpaperType.webaudio:
                                    case WallpaperType.url:
                                        Logger.Info("Web type wallpaper import is disabled for cmd control.");
                                        break;
                                    case WallpaperType.video:
                                    case WallpaperType.gif:
                                    case WallpaperType.videostream:
                                    case WallpaperType.picture:
                                        libraryVm.AddWallpaper(opts.File,
                                            type,
                                            LibraryTileType.cmdImport,
                                            userSettings.Settings.SelectedDisplay);
                                        break;
                                    case WallpaperType.app:
                                    case WallpaperType.bizhawk:
                                    case WallpaperType.unity:
                                    case WallpaperType.godot:
                                    case WallpaperType.unityaudio:
                                        Logger.Info("App type wallpaper import is disabled for cmd control.");
                                        break;
                                    case (WallpaperType)100:
                                        Logger.Info("Lively .zip type wallpaper import is disabled for cmd control.");
                                        break;
                                    case (WallpaperType)(-1):
                                        Logger.Info("Wallpaper format not supported.");
                                        break;
                                    default:
                                        Logger.Info("No wallpaper type recognised.");
                                        break;
                                }
                            }
                        }
                    }
                }));
            }
            return 0;
        }

        private int RunCloseWallpaperOptions(CloseWallpaperOptions opts)
        {
            if (opts.Monitor != null)
            {
                var id = (int)opts.Monitor;
                if (id == -1 ||
                    userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate ||
                    userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span)
                {
                    desktopCore.CloseAllWallpapers();
                }
                else
                {
                    var screen = ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == (id).ToString());
                    if (screen != null)
                    {
                        desktopCore.CloseWallpaper(screen);
                    }
                }
            }
            return 0;
        }

        private int RunSeekWallpaperOptions(SeekWallpaperOptions opts)
        {
            var screen = opts.Monitor != null ?
                ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();
            if (screen != null)
            {
                var wp = desktopCore.Wallpapers.FirstOrDefault(x => x.Screen.Equals(screen));
                if (wp != null)
                {
                    if (opts.Param != null)
                    {
                        if ((opts.Param.StartsWith('+') || opts.Param.StartsWith('-')))
                        {
                            if (float.TryParse(opts.Param, out float val))
                            {
                                SeekWallpaper(Clamp(val, -100, 100), Core.PlaybackPosType.relativePercent, screen, wp.Model);
                            }
                        }
                        else
                        {
                            if (float.TryParse(opts.Param, out float val))
                            {
                                SeekWallpaper(Clamp(val, 0, 100), Core.PlaybackPosType.absolutePercent, screen, wp.Model);
                            }
                        }
                    }
                }
            }
            return 0;
        }

        private void SeekWallpaper(float seek, Core.PlaybackPosType type, ILivelyScreen screen, ILibraryModel wp)
        {
            switch (userSettings.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    desktopCore.SeekWallpaper(screen, seek, type);
                    break;
                case WallpaperArrangement.span:
                case WallpaperArrangement.duplicate:
                    desktopCore.SeekWallpaper(wp, seek, type);
                    break;
            }
        }

        private int RunCustomiseWallpaperOptions(CustomiseWallpaperOptions opts)
        {
            if (opts.Param != null)
            {
                //use primary screen if none found..
                var screen = opts.Monitor != null ?
                    ScreenHelper.GetScreen().FirstOrDefault(x => x.DeviceNumber == ((int)opts.Monitor).ToString()) : ScreenHelper.GetPrimaryScreen();

                if (screen != null)
                {
                    try
                    {
                        var wp = desktopCore.Wallpapers.FirstOrDefault(x => x.Screen.Equals(screen));
                        //only for running wallpaper instance unlike gui property..
                        if (wp == null)
                            return 0;

                        //delimiter
                        var tmp = opts.Param.Split("=");
                        string name = tmp[0], val = tmp[1], ctype = null;
                        var lp = JObject.Parse(File.ReadAllText(wp.LivelyPropertyCopyPath));
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

                        IpcMessage msg = null;
                        ctype = (ctype == null && name.Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase)) ? "button" : ctype;
                        if (ctype != null)
                        {
                            if (ctype.Equals("button", StringComparison.OrdinalIgnoreCase))
                            {
                                if (name.Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (LivelyPropertiesView.RestoreOriginalPropertyFile(wp.Model, wp.LivelyPropertyCopyPath))
                                    {
                                        msg = new LivelyButton() { Name = "lively_default_settings_reload", IsDefault = true };
                                    }
                                }
                                else
                                {
                                    msg = new LivelyButton() { Name = name };
                                }
                            }
                            else
                            {
                                if (ctype.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                                {
                                    msg = new LivelyCheckbox() { Name = name, Value = (val == "true") };
                                    lp[name]["value"] = (val == "true");
                                }
                                else if (ctype.Equals("slider", StringComparison.OrdinalIgnoreCase))
                                {
                                    var sliderValue = val.StartsWith("++") || val.StartsWith("--") ?
                                        (double)lp[name]["value"] + double.Parse(val[1..]) : double.Parse(val);
                                    sliderValue = Clamp(sliderValue, (double)lp[name]["min"], (double)lp[name]["max"]);

                                    msg = new LivelySlider() { Name = name, Value = sliderValue };
                                    lp[name]["value"] = sliderValue;
                                }
                                else if (ctype.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                                {
                                    var selectedIndex = val.StartsWith("++") || val.StartsWith("--") ?
                                        (int)lp[name]["value"] + int.Parse(val[1..]) : int.Parse(val);
                                    selectedIndex = Clamp(selectedIndex, 0, lp[name]["items"].Count() - 1);

                                    msg = new LivelyDropdown() { Name = name, Value = selectedIndex };
                                    lp[name]["value"] = selectedIndex;
                                }
                                else if (ctype.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                                {
                                    msg = new LivelyFolderDropdown() { Name = name, Value = val };
                                    lp[name]["value"] = Path.GetFileName(val);
                                }
                                else if (ctype.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                                {
                                    msg = new LivelyTextBox() { Name = name, Value = val };
                                    lp[name]["value"] = val;
                                }
                                else if (ctype.Equals("color", StringComparison.OrdinalIgnoreCase))
                                {
                                    msg = new LivelyColorPicker() { Name = name, Value = val };
                                    lp[name]["value"] = val;
                                }

                                try
                                {
                                    JsonUtil.Write(wp.LivelyPropertyCopyPath, lp);
                                }
                                catch (Exception e)
                                {
                                    Logger.Error(e.ToString());
                                }
                            }

                            if (msg != null)
                            {
                                switch (userSettings.Settings.WallpaperArrangement)
                                {
                                    case WallpaperArrangement.per:
                                        desktopCore.SendMessageWallpaper(screen, wp.Model, msg);
                                        break;
                                    case WallpaperArrangement.span:
                                    case WallpaperArrangement.duplicate:
                                        desktopCore.SendMessageWallpaper(wp.Model, msg);
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.ToString());
                    }
                }
            }
            return 0;
        }

        private int RunScreenSaverOptions(ScreenSaverOptions opts)
        {
            if (opts.Show != null)
            {
                if (opts.Show == true)
                {
                    screenSaver.Start();
                }
                else
                {
                    screenSaver.Stop();
                }
            }

            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                if (opts.Configure != null)
                {
                    appWindow?.ShowControlPanelDialog();
                }

                if (opts.Preview != null)
                {
                    screenSaver.CreatePreview(new IntPtr((int)opts.Preview));
                }
            }));
            return 0;
        }

        private int HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var item in errs)
            {
                Logger.Error(item.ToString());
            }
            return 0;
        }

        #region helpers

        private static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
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
