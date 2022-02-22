using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Lively.Common.Helpers.MVVM;
using Lively.Models;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Common.Helpers.Archive;
using Lively.Grpc.Client;
using Windows.System;
using Lively.UI.WinUI.Helpers;
using System.Diagnostics;

namespace Lively.UI.WinUI.ViewModels
{
    public class LibraryViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<string> wallpaperScanFolders;

        private readonly IDesktopCoreClient desktopCore;
        private readonly IUserSettingsClient userSettings;
        private readonly SettingsViewModel settingsVm;
        private readonly IDisplayManagerClient displayManager;

        public LibraryViewModel(IDesktopCoreClient desktopCore, IDisplayManagerClient displayManager, IUserSettingsClient userSettings, SettingsViewModel settingsVm)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.settingsVm = settingsVm;
            this.userSettings = userSettings;

            wallpaperScanFolders = new List<string>
            {
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
            };

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Insert(BinarySearch(LibraryItems, item.Title), item);        
            }

            //Select already running item when UI program is started again..
            UpdateSelectedWallpaper();

            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            desktopCore.WallpaperUpdated += DesktopCore_WallpaperUpdated;
            settingsVm.WallpaperDirChanged += (s, e) => WallpaperDirectoryUpdate(e);

            _ = InstallDefaultWallpapers();
        }

        #region collections

        private ObservableCollection<LibraryModel> _libraryItems = new ObservableCollection<LibraryModel>();
        public ObservableCollection<LibraryModel> LibraryItems
        {
            get { return _libraryItems; }
            set
            {
                if (value != _libraryItems)
                {
                    _libraryItems = value;
                    OnPropertyChanged();
                }
            }
        }

        private LibraryModel _selectedItem;
        public LibraryModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value != null && value.DataType == LibraryItemType.ready)
                {
                    var wallpapers = desktopCore.Wallpapers.Where(x => x.LivelyInfoFolderPath.Equals(value.LivelyInfoFolderPath, StringComparison.OrdinalIgnoreCase));
                    if (wallpapers.Count() > 0)
                    {
                        switch (userSettings.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                                if (!wallpapers.Any(x => userSettings.Settings.SelectedDisplay.Equals(x.Display)))
                                {
                                    desktopCore.SetWallpaper(value, userSettings.Settings.SelectedDisplay);
                                }
                                break;
                            case WallpaperArrangement.span:
                                //Wallpaper already set!
                                break;
                            case WallpaperArrangement.duplicate:
                                //Wallpaper already set!
                                break;
                        }
                    }
                    else
                    {
                        desktopCore.SetWallpaper(value, userSettings.Settings.SelectedDisplay);
                    }
                }
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        #endregion //collections

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                UpdateSelectedWallpaper();
            });
        }

        private void DesktopCore_WallpaperUpdated(object sender, WallpaperUpdatedData e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                var item = LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(e.InfoPath, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    if (e.Category == UpdateWallpaperType.changed)
                    {
                        //temporary for visual appearance only..
                        item.Title = e.Info.Title;
                        item.Desc = e.Info.Desc;
                        item.ImagePath = e.Info.Thumbnail;
                    }
                    else if (e.Category == UpdateWallpaperType.done)
                    {
                        LibraryItems.Remove(item);
                        AddWallpaper(item.LivelyInfoFolderPath);
                    }
                    else if (e.Category == UpdateWallpaperType.remove)
                    {
                        LibraryItems.Remove(item);
                    }
                }
            });
        }

        /// <summary>
        /// Update library selected item based on selected display.
        /// </summary>
        public void UpdateSelectedWallpaper()
        {
            if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span && desktopCore.Wallpapers.Count > 0)
            {
                SelectedItem = LibraryItems.FirstOrDefault(x => desktopCore.Wallpapers[0].LivelyInfoFolderPath.Equals(x.LivelyInfoFolderPath, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => userSettings.Settings.SelectedDisplay.Equals(x.Display));
                SelectedItem = LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(wallpaper?.LivelyInfoFolderPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        #region helpers

        public ILibraryModel AddWallpaper(string folderPath, bool processing = false)
        {
            var libItem = ScanWallpaperFolder(folderPath);
            if (libItem != null)
            {
                var index = processing ? 0 : BinarySearch(LibraryItems, libItem.Title);
                libItem.DataType = processing ? LibraryItemType.processing : LibraryItemType.ready;
                libItem.WallpaperCategory = LocalizationUtil.GetLocalizedWallpaperCategory(libItem.LivelyInfo.Type);
                LibraryItems.Insert(index, libItem);
            }
            return libItem;
        }

        /// <summary>
        /// Load wallpapers from the given parent folder(), only top directory is scanned.
        /// </summary>
        /// <param name="folderPaths">Parent folders to search for subdirectories.</param>
        /// <returns>Sorted(based on Title) wallpaper data.</returns>
        private IEnumerable<LibraryModel> ScanWallpaperFolders(List<string> folderPaths)
        {
            var dir = new List<string[]>();
            for (int i = 0; i < folderPaths.Count; i++)
            {
                try
                {
                    dir.Add(Directory.GetDirectories(folderPaths[i], "*", SearchOption.TopDirectoryOnly));
                }
                catch (Exception e)
                {
                    Logger.Error("Skipping wp folder-scan:" + e.ToString());
                }
            }

            for (int i = 0; i < dir.Count; i++)
            {
                for (int j = 0; j < dir[i].Length; j++)
                {
                    var currDir = dir[i][j];
                    var libItem = ScanWallpaperFolder(currDir);
                    if (libItem != null)
                    {
                        libItem.WallpaperCategory = LocalizationUtil.GetLocalizedWallpaperCategory(libItem.LivelyInfo.Type);
                        yield return libItem;
                    }
                }
            }
        }

        private LibraryModel ScanWallpaperFolder(string folderPath)
        {
            if (File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
            {
                LivelyInfoModel info = null;
                try
                {
                    info = JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(folderPath, "LivelyInfo.json"));
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }

                if (info != null)
                {
                    if (info.Type == WallpaperType.videostream || info.Type == WallpaperType.url)
                    {
                        //online content, no file.
                        Logger.Info("Loading Wallpaper (no-file):- " + info.FileName + " " + info.Type);
                        return new LibraryModel(info, folderPath, LibraryItemType.ready, true);
                    }
                    else
                    {
                        if (info.IsAbsolutePath)
                        {
                            Logger.Info("Loading Wallpaper(absolute):- " + info.FileName + " " + info.Type);
                        }
                        else
                        {
                            Logger.Info("Loading Wallpaper(relative):- " + Path.Combine(folderPath, info.FileName) + " " + info.Type);
                        }
                        return new LibraryModel(info, folderPath, LibraryItemType.ready, true);
                    }
                }
            }
            else
            {
                Logger.Info("Not a lively wallpaper folder, skipping:- " + folderPath);
            }
            return null;
        }

        private List<LibraryModel> SortWallpapers(List<LibraryModel> data)
        {
            try
            {
                return data.OrderBy(x => x.LivelyInfo.Title).ToList();
            }
            catch (ArgumentNullException)
            {
                return null;
            }
        }

        public void SortWallpaper(LibraryModel item)
        {
            try
            {
                LibraryItems.Remove(item);
                var index = BinarySearch(LibraryItems, item.Title);
                //LibraryItems.Move(LibraryItems.IndexOf(item), binarySearchIndex);
                LibraryItems.Insert(index, item);
            }
            catch { }
        }

        private int BinarySearch(ObservableCollection<LibraryModel> item, string x)
        {
            if (x is null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            int l = 0, r = item.Count - 1, m, res;
            while (l <= r)
            {
                m = (l + r) / 2;

                res = String.Compare(x, item[m].Title);

                if (res == 0)
                    return m;

                if (res > 0)
                    l = m + 1;

                else
                    r = m - 1;
            }
            return l;//(l - 1);
        }

        /// <summary>
        /// Rescans wallpaper directory and update library.
        /// </summary>
        private void WallpaperDirectoryUpdate(string newDir)
        {
            LibraryItems.Clear();
            wallpaperScanFolders.Clear();
            wallpaperScanFolders.Add(Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallDir));
            wallpaperScanFolders.Add(Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallTempDir));
            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Insert(BinarySearch(LibraryItems, item.Title), item);
            }
        }

        private async Task InstallDefaultWallpapers()
        {
            if (userSettings.Settings.IsFirstRun)
            {
                try
                {
                    IsBusy = true;
                    userSettings.Settings.WallpaperBundleVersion = await Task.Run(() =>
                        ExtractWallpaperBundle(userSettings.Settings.WallpaperBundleVersion, Path.Combine(desktopCore.BaseDirectory, "bundle"), userSettings.Settings.WallpaperDir));
                    userSettings.Save<ISettingsModel>();
                    await settingsVm.WallpaperDirectoryChange(userSettings.Settings.WallpaperDir);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Extract default wallpapers and incremental if any.
        /// </summary>
        public static int ExtractWallpaperBundle(int currentBundleVer, string currentBundleDir, string currentWallpaperDir)
        {
            //Lively stores the last extracted bundle filename, extraction proceeds from next file onwards.
            int maxExtracted = currentBundleVer;
            try
            {
                //wallpaper bundles filenames are 0.zip, 1.zip ...
                var sortedBundles = Directory.GetFiles(currentBundleDir).OrderBy(x => x);

                foreach (var item in sortedBundles)
                {
                    if (int.TryParse(Path.GetFileNameWithoutExtension(item), out int val))
                    {
                        if (val > maxExtracted)
                        {
                            //Sharpzip library will overwrite files if exists during extraction.
                            ZipExtract.ZipExtractFile(item, Path.Combine(currentWallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir), false);
                            maxExtracted = val;
                        }
                    }
                }
            }
            catch { /* TODO */ }
            return maxExtracted;
        }

        #endregion //helpers
    }
}
