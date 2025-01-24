using CommandLine;
using Lively.Common;
using Lively.Common.Extensions;
using Lively.Common.Factories;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Core.Suspend;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using static Lively.Common.CommandlineArgs;

namespace Lively.Commandline
{
    //Doc: https://github.com/rocksdanister/lively/wiki/Command-Line-Controls
    //Note: No user settings should be saved here, changes are temporary only.
    public class CommandHandler : ICommandHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;
        private readonly IScreensaverService screenSaver;
        private readonly IPlayback playbackMonitor;
        private readonly IRunnerService runner;
        private readonly ISystray systray;

        private readonly Random rng = new Random();

        public CommandHandler(IWallpaperLibraryFactory wallpaperLibraryFactory,
            IUserSettingsService userSettings,
            IDesktopCore desktopCore,
            IDisplayManager displayManager,
            IScreensaverService screenSaver,
            IPlayback playbackMonitor,
            IRunnerService runner,
            ISystray systray)
        {
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;
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
            if (App.IsExclusiveScreensaverMode)
                return;

            _ = Parser.Default.ParseArguments<AppOptions, SetWallpaperOptions, CustomiseWallpaperOptions, CloseWallpaperOptions, ScreenSaverOptions, SeekWallpaperOptions, ScreenshotOptions>(args)
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
                    // Since it is separate process dispatcher not required.
                    runner.ShowUI();
                }
                else
                {
                    runner.CloseUI();
                }
            }

            if (!string.IsNullOrEmpty(opts.Volume) && float.TryParse(opts.Volume, out float val))
            {
                if (opts.Volume.StartsWith('+') || opts.Volume.StartsWith('-'))
                {
                    var clampedValue = Clamp((int)val, -100, 100);
                    var newVolume = Clamp(userSettings.Settings.AudioVolumeGlobal + clampedValue, 0, 100);
                    userSettings.Settings.AudioVolumeGlobal = newVolume;
                }
                else
                {
                    userSettings.Settings.AudioVolumeGlobal = Clamp((int)val, 0, 100);
                }
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
                App.QuitApp();
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
                userSettings.Save<SettingsModel>();
            }

            return 0;
        }

        private int RunSetWallpaperOptions(SetWallpaperOptions opts)
        {
            if (opts.File != null)
            {
                if (opts.IsReload)
                {
                    var screen = opts.Monitor != null ? displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == (int)opts.Monitor) : null;
                    if (screen != null)
                        _ = desktopCore.RestartWallpaper(screen);
                    else
                        _ = desktopCore.RestartWallpaper();
                }
                else if (opts.IsRandom)
                {
                    switch (userSettings.Settings.WallpaperArrangement)
                    {
                        case WallpaperArrangement.per:
                            {
                                var screen = opts.Monitor != null ? displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == (int)opts.Monitor) : null;
                                if (screen != null)
                                {
                                    var wallpapers = GetRandomWallpaper().Take(2);
                                    using var enumerator = wallpapers.GetEnumerator();
                                    var firstWallpaper = enumerator.MoveNext() ? enumerator.Current : null;
                                    if (firstWallpaper is null)
                                        return 0;

                                    var secondWallpaper = enumerator.MoveNext() ? enumerator.Current : null;
                                    var currentWallpaper = desktopCore.Wallpapers.FirstOrDefault(x => x.Screen.Equals(screen));

                                    // Select different wallpaper if current is same.
                                    var newWallpaper = secondWallpaper != null && currentWallpaper?.Model.LivelyInfoFolderPath == firstWallpaper.LivelyInfoFolderPath ?
                                        secondWallpaper : firstWallpaper;

                                    _ = desktopCore.SetWallpaperAsync(newWallpaper, screen);
                                }
                                else
                                {
                                    // Apply wallpaper to all screens.
                                    var screenCount = displayManager.DisplayMonitors.Count;
                                    // Fetch additional wallpaper for more randomness.
                                    var wallpapers = GetRandomWallpaper().Take(screenCount * 2);
                                    if (!wallpapers.Any())
                                        return 0;

                                    var usedWallpapers = new List<LibraryModel>();
                                    for (int i = 0; i < screenCount; i++)
                                    {
                                        var currentScreen = displayManager.DisplayMonitors[i];
                                        var currentWallpaper = desktopCore.Wallpapers.FirstOrDefault(x => x.Screen.Equals(currentScreen));

                                        // Select a random wallpaper that is Not already used and Not the same as the current wallpaper on this screen.
                                        var newWallpaper =
                                            wallpapers.FirstOrDefault(x => (currentWallpaper == null || x.LivelyInfoFolderPath != currentWallpaper.Model.LivelyInfoFolderPath) && !usedWallpapers.Contains(x))
                                            // Fallback if all match currentWallpaper
                                            ?? wallpapers.FirstOrDefault(x => !usedWallpapers.Contains(x))
                                            // Fallback to the first wallpaper if all are used
                                            ?? wallpapers.First();

                                        usedWallpapers.Add(newWallpaper);

                                        _ = desktopCore.SetWallpaperAsync(newWallpaper, currentScreen);
                                    }
                                }
                            }
                            break;
                        case WallpaperArrangement.span:
                        case WallpaperArrangement.duplicate:
                            {
                                var wallpapers = GetRandomWallpaper().Take(2);
                                using var enumerator = wallpapers.GetEnumerator();
                                var firstWallpaper = enumerator.MoveNext() ? enumerator.Current : null;
                                if (firstWallpaper is null)
                                    return 0;

                                var secondWallpaper = enumerator.MoveNext() ? enumerator.Current : null;
                                var currentWallpaper = desktopCore.Wallpapers.FirstOrDefault();

                                // Select different wallpaper if current is same.
                                var newWallpaper = secondWallpaper != null && currentWallpaper?.Model.LivelyInfoFolderPath == firstWallpaper.LivelyInfoFolderPath ?
                                    secondWallpaper : firstWallpaper;

                                _ = desktopCore.SetWallpaperAsync(newWallpaper, displayManager.PrimaryDisplayMonitor);
                            }
                            break;
                    }
                }
                else if (Directory.Exists(opts.File))
                {
                    //Folder containing LivelyInfo.json file.
                    var screen = opts.Monitor != null ?
                        displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == (int)opts.Monitor) : displayManager.PrimaryDisplayMonitor;
                    try
                    {
                        var di = new DirectoryInfo(opts.File); //Verify path is wallpaper install location.
                        if (di.Parent.FullName.Contains(userSettings.Settings.WallpaperDir, StringComparison.OrdinalIgnoreCase))
                        {
                            var libraryItem = wallpaperLibraryFactory.CreateFromDirectory(opts.File);
                            if (screen != null)
                                _ = desktopCore.SetWallpaperAsync(libraryItem, screen);
                        }
                    }
                    catch { /* TODO */ }
                }
                else if (File.Exists(opts.File))
                {
                    var screen = opts.Monitor != null ?
                        displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == (int)opts.Monitor) : displayManager.PrimaryDisplayMonitor;
                    LibraryModel libraryItem = GetWallpapers().FirstOrDefault(x => x.FilePath != null && x.FilePath.Equals(opts.File, StringComparison.OrdinalIgnoreCase));

                    if (screen is null)
                        return 1;

                    if (libraryItem != null)
                    {
                        _ = desktopCore.SetWallpaperAsync(libraryItem, screen);
                    }
                    else
                    {
                        try
                        {
                            Logger.Info("Wallpaper not found in library, importing as new file..");
                            var type = FileTypes.GetFileType(opts.File);
                            if (type.IsMediaWallpaper())
                            {
                                var dir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
                                Directory.CreateDirectory(dir);
                                var data = new LivelyInfoModel()
                                {
                                    Title = Path.GetFileNameWithoutExtension(opts.File),
                                    Type = type,
                                    IsAbsolutePath = true,
                                    FileName = opts.File,
                                    Contact = string.Empty,
                                    Preview = string.Empty,
                                    Thumbnail = string.Empty,
                                    Arguments = string.Empty,
                                };
                                JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(dir, "LivelyInfo.json"), data);

                                var model = wallpaperLibraryFactory.CreateFromDirectory(dir);
                                model.DataType = LibraryItemType.cmdImport;
                                _ = desktopCore.SetWallpaperAsync(model, screen);
                            }
                            else
                                Logger.Info($"Unsupported command import file:{type}");
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
                        }
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
                displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == (int)opts.Monitor) : displayManager.PrimaryDisplayMonitor;
            if (screen != null)
            {
                var wp = desktopCore.Wallpapers.FirstOrDefault(x => x.Screen.Equals(screen));
                if (wp != null)
                {
                    if (opts.Param != null)
                    {
                        if (opts.Param.StartsWith('+') || opts.Param.StartsWith('-'))
                        {
                            if (float.TryParse(opts.Param, out float val))
                            {
                                SeekWallpaper(Clamp(val, -100, 100), PlaybackPosType.relativePercent, screen, wp.Model);
                            }
                        }
                        else
                        {
                            if (float.TryParse(opts.Param, out float val))
                            {
                                SeekWallpaper(Clamp(val, 0, 100), PlaybackPosType.absolutePercent, screen, wp.Model);
                            }
                        }
                    }
                }
            }
            return 0;
        }

        private void SeekWallpaper(float seek, PlaybackPosType type, DisplayMonitor screen, LibraryModel wp)
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
                    displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == (int)opts.Monitor) : displayManager.PrimaryDisplayMonitor;

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
                        ctype = ctype == null && name.Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase) ? "button" : ctype;
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
                                    msg = new LivelyCheckbox() { Name = name, Value = val == "true" };
                                    lp[name]["value"] = val == "true";
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
            if (opts.Show != null || opts.ShowExclusive != null)
            {
                if (opts.Show == true || opts.ShowExclusive == true)
                    _ = screenSaver.StartAsync();
                else
                    screenSaver.Stop();
            }

            if (opts.Configure != null && opts.Configure == true)
                runner.ShowUI();

            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                if (opts.Preview != null)
                {
                    screenSaver.CreatePreview(new nint((int)opts.Preview));
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
                    displayManager.DisplayMonitors.FirstOrDefault(x => x.Index == (int)opts.Monitor) : displayManager.PrimaryDisplayMonitor;
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

        private IEnumerable<LibraryModel> GetWallpapers()
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
                    LibraryModel libItem = null;
                    try
                    {
                        libItem = wallpaperLibraryFactory.CreateFromDirectory(dir[i][j]);
                    }
                    catch { }

                    if (libItem != null)
                    {
                        yield return libItem;
                    }
                }
            }
        }

        private IEnumerable<LibraryModel> GetRandomWallpaper()
        {
            var dir = new List<string>();
            string[] folderPaths = {
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
            };
            for (int i = 0; i < folderPaths.Count(); i++)
            {
                try
                {
                    dir.AddRange(Directory.GetDirectories(folderPaths[i], "*", SearchOption.TopDirectoryOnly));
                }
                catch { /* TODO */ }
            }

            //Fisher-Yates shuffle
            int n = dir.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = dir[k];
                dir[k] = dir[n];
                dir[n] = value;
            }

            for (int i = 0; i < dir.Count; i++)
            {
                LibraryModel libItem = null;
                try
                {
                    libItem = wallpaperLibraryFactory.CreateFromDirectory(dir[i]);
                }
                catch { }

                if (libItem != null)
                {
                    yield return libItem;
                }
            }
        }

        /// <summary>
        /// Copies LivelyProperties.json from root to the per monitor file.
        /// </summary>
        /// <param name="wallpaperData">Wallpaper info.</param>
        /// <param name="livelyPropertyCopyPath">Modified LivelyProperties.json path.</param>
        /// <returns></returns>
        public static bool RestoreOriginalPropertyFile(LibraryModel wallpaperData, string livelyPropertyCopyPath)
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
