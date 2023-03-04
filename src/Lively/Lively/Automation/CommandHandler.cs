using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommandLine;
using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Core;
using Lively.Core.Display;
using Lively.Core.Suspend;
using Lively.Helpers;
using Lively.Models;
using Lively.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Lively.Common.AutomationArgs;

namespace Lively.Automation
{
    //Doc: https://github.com/rocksdanister/lively/wiki/Command-Line-Controls
    //Note: No user settings should be saved here, changes are temporary only.
    public class CommandHandler : ICommandHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;
        private readonly IScreensaverService screenSaver;
        private readonly IPlayback playbackMonitor;
        private readonly IRunnerService runner;
        private readonly ISystray systray;

        public CommandHandler(IUserSettingsService userSettings, 
            IDesktopCore desktopCore, 
            IDisplayManager displayManager,
            IScreensaverService screenSaver,
            IPlayback playbackMonitor,
            IRunnerService runner,
            ISystray systray)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.screenSaver = screenSaver;
            this.playbackMonitor = playbackMonitor;
            this.systray = systray;
            this.runner = runner;
        }

        public void ParseArgs(string[] args)
        {
            _ = CommandLine.Parser.Default.ParseArguments<AppOptions, SetWallpaperOptions, CustomiseWallpaperOptions, CloseWallpaperOptions, ScreenSaverOptions, SeekWallpaperOptions, ScreenshotOptions>(args)
                .MapResult(
                    (AppOptions opts) => RunAppOptions(opts),
                    (SetWallpaperOptions opts) => RunSetWallpaperOptions(opts),
                    (CloseWallpaperOptions opts) => RunCloseWallpaperOptions(opts),
                    (SeekWallpaperOptions opts) => RunSeekWallpaperOptions(opts),
                    (CustomiseWallpaperOptions opts) => RunCustomiseWallpaperOptions(opts),
                    (ScreenSaverOptions opts) => RunScreenSaverOptions(opts),
                    (ScreenshotOptions opts) => RunScreenshotOptions(opts),
                    errs => HandleParseError(errs));
        }

        private int RunAppOptions(AppOptions opts)
        {
            if (opts.ShowApp != null)
            {
                if ((bool)opts.ShowApp)
                {
                    //process so dispatcher not required.
                    runner.ShowUI();
                }
                else
                {
                    runner.CloseUI();
                }
            }
            /*
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                if (opts.ShowTray != null)
                {
                    systray.Visibility((bool)opts.ShowTray);
                }
            }));
            */
            if (opts.Volume != null)
            {
                userSettings.Settings.AudioVolumeGlobal = Clamp((int)opts.Volume, 0, 100);
            }

            if (opts.Play != null)
            {
                playbackMonitor.WallpaperPlayback = (bool)opts.Play ? PlaybackState.play : PlaybackState.paused;
            }

            if (opts.Startup != null)
            {
                try
                {
                    _ = WindowsStartup.SetStartup((bool)opts.Startup);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            if (opts.ShowIcons != null)
            {
                DesktopUtil.SetDesktopIconVisibility((bool)opts.ShowIcons);
            }

            if (opts.ShutdownApp != null)
            {
                App.ShutDown();
            }

            if (opts.RestartApp != null)
            {
                //TODO
            }

            if (!string.IsNullOrEmpty(opts.WallpaperArrangement))
            {
                desktopCore.CloseAllWallpapers();
                userSettings.Settings.WallpaperArrangement = opts.WallpaperArrangement switch
                {
                    "per" => WallpaperArrangement.per,
                    "span" => WallpaperArrangement.span,
                    "duplicate" => WallpaperArrangement.duplicate,
                    _ => WallpaperArrangement.per,
                };
                userSettings.Save<ISettingsModel>();
            }

            return 0;
        }

        private int RunSetWallpaperOptions(SetWallpaperOptions opts)
        {
            if (opts.File != null)
            {
                if (Directory.Exists(opts.File))
                {
                    //Folder containing LivelyInfo.json file.
                    var screen = opts.Monitor != null ?
                        displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == ((int)opts.Monitor)) : displayManager.PrimaryDisplayMonitor;
                    try
                    {
                        var di = new DirectoryInfo(opts.File); //Verify path is wallpaper install location.
                        if (di.Parent.FullName.Contains(userSettings.Settings.WallpaperDir, StringComparison.OrdinalIgnoreCase))
                        {
                            var libraryItem = WallpaperUtil.ScanWallpaperFolder(opts.File);
                            if (screen != null)
                            {
                                desktopCore.SetWallpaper(libraryItem, screen);
                            }
                        }
                    }
                    catch { /* TODO */ }
                }
                else if (File.Exists(opts.File))
                {
                    var screen = opts.Monitor != null ?
                        displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == ((int)opts.Monitor)) : displayManager.PrimaryDisplayMonitor;
                    ILibraryModel libraryItem = null;
                    foreach (var x in GetWallpapers())
                    {
                        if (x.FilePath != null && x.FilePath.Equals(opts.File, StringComparison.OrdinalIgnoreCase))
                        {
                            libraryItem = x;
                            break;
                        }
                    }

                    if (screen != null)
                    {
                        if (libraryItem != null)
                        {
                            desktopCore.SetWallpaper(libraryItem, screen);
                        }
                        /*
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
                        */
                    }
                }
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
                    var screen = displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == id);
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
                displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == ((int)opts.Monitor)) : displayManager.PrimaryDisplayMonitor;
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

        private void SeekWallpaper(float seek, Core.PlaybackPosType type, IDisplayMonitor screen, ILibraryModel wp)
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
                    displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == ((int)opts.Monitor)) : displayManager.PrimaryDisplayMonitor;

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
                                    if (RestoreOriginalPropertyFile(wp.Model, wp.LivelyPropertyCopyPath))
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
                                        desktopCore.SendMessageWallpaper(screen, wp.Model.LivelyInfoFolderPath, msg);
                                        break;
                                    case WallpaperArrangement.span:
                                    case WallpaperArrangement.duplicate:
                                        desktopCore.SendMessageWallpaper(wp.Model.LivelyInfoFolderPath, msg);
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

            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                if (opts.Configure != null)
                {
                    //TODO
                }

                if (opts.Preview != null)
                {
                    screenSaver.CreatePreview(new IntPtr((int)opts.Preview));
                }
            }));
            return 0;
        }

        private int RunScreenshotOptions(ScreenshotOptions opts)
        {
            if (opts.File is not null)
            {
                if (Path.GetExtension(opts.File) != ".jpg")
                {
                    opts.File += ".jpg";
                }

                //use primary screen if none found..
                var screen = opts.Monitor != null ?
                    displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == ((int)opts.Monitor)) : displayManager.PrimaryDisplayMonitor;
                if (screen is not null)
                {
                    var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => x.Screen.Equals(screen));
                    _ = wallpaper?.ScreenCapture(opts.File);
                }
            }
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

        private IEnumerable<ILibraryModel> GetWallpapers()
        {
            var dir = new List<string[]>();
            string[] folderPaths = {
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
            };
            for (int i = 0; i < folderPaths.Count(); i++)
            {
                try
                {
                    dir.Add(Directory.GetDirectories(folderPaths[i], "*", SearchOption.TopDirectoryOnly));
                }
                catch { /* TODO */ }
            }

            for (int i = 0; i < dir.Count; i++)
            {
                for (int j = 0; j < dir[i].Length; j++)
                {
                    ILibraryModel libItem = null;
                    try
                    {
                        libItem = WallpaperUtil.ScanWallpaperFolder(dir[i][j]);
                    }
                    catch { }

                    if (libItem != null)
                    {
                        yield return libItem;
                    }
                }
            }
        }

        /// <summary>
        /// Copies LivelyProperties.json from root to the per monitor file.
        /// </summary>
        /// <param name="wallpaperData">Wallpaper info.</param>
        /// <param name="livelyPropertyCopyPath">Modified LivelyProperties.json path.</param>
        /// <returns></returns>
        public static bool RestoreOriginalPropertyFile(ILibraryModel wallpaperData, string livelyPropertyCopyPath)
        {
            bool status = false;
            try
            {
                File.Copy(wallpaperData.LivelyPropertyPath, livelyPropertyCopyPath, true);
                status = true;
            }
            catch { /* TODO */ }
            return status;
        }

        #endregion //helpers
    }
}
