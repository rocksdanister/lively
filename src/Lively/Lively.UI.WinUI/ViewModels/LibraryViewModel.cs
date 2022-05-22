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
using Lively.Common.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Lively.UI.WinUI.Services;

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
        private readonly IDialogService dialogService;

        public LibraryViewModel(IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            IUserSettingsClient userSettings,
            SettingsViewModel settingsVm,
            IDialogService dialogService)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.settingsVm = settingsVm;
            this.userSettings = userSettings;
            this.dialogService = dialogService;

            wallpaperScanFolders = new List<string>
            {
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
            };

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Insert(BinarySearch(LibraryItems, item.Title), item);
            }

            LibrarySelectionMode = userSettings.Settings.RememberSelectedScreen ? "Single" : "None";
            //Select already running item when UI program is started again..
            UpdateSelectedWallpaper();

            settingsVm.UIStateChanged += (s, e) =>
            {
                foreach (var item in LibraryItems)
                {
                    item.ImagePath = e == LivelyGUIState.lite ? item.ThumbnailPath : item.PreviewClipPath ?? item.ThumbnailPath;
                }
            };

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
                if (!userSettings.Settings.RememberSelectedScreen)
                    return;

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

        private string _librarySelectionMode = "Single";
        public string LibrarySelectionMode
        {
            get => _librarySelectionMode;
            set
            {
                _librarySelectionMode = value;
                OnPropertyChanged();
            }

        }

        private RelayCommand<LibraryModel> _libraryClickCommand;
        public RelayCommand<LibraryModel> LibraryClickCommand => _libraryClickCommand ??= new RelayCommand<LibraryModel>(async (wp) =>
        {
            if (userSettings.Settings.RememberSelectedScreen)
                return;

            var monitor = displayManager.DisplayMonitors.Count == 1 || userSettings.Settings.WallpaperArrangement != WallpaperArrangement.per ?
                displayManager.DisplayMonitors.FirstOrDefault(x => x.IsPrimary) : await dialogService.ShowDisplayChooseDialog();
            if (monitor != null && wp != null)
            {
                await desktopCore.SetWallpaper(wp, monitor);
            }
        });

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
                        item.ImagePath = e.Info.IsAbsolutePath ? e.Info.Thumbnail : Path.Combine(e.InfoPath, e.Info.Thumbnail);
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
            try
            {
                var libItem = ScanWallpaperFolder(folderPath);
                var index = processing ? 0 : BinarySearch(LibraryItems, libItem.Title);
                libItem.DataType = processing ? LibraryItemType.processing : LibraryItemType.ready;
                LibraryItems.Insert(index, libItem);
                return libItem;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return null;
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
                    Logger.Error(e);
                }
            }

            for (int i = 0; i < dir.Count; i++)
            {
                for (int j = 0; j < dir[i].Length; j++)
                {
                    var currDir = dir[i][j];
                    LibraryModel libItem = null;
                    try
                    {
                        libItem = ScanWallpaperFolder(currDir);
                        Logger.Info($"Loaded wallpaper: {libItem.FilePath}");
                    }
                    catch(Exception e)
                    {
                        Logger.Error(e);
                    }

                    if (libItem != null)
                    {
                        yield return libItem;
                    }
                }
            }
        }

        private LibraryModel ScanWallpaperFolder(string folderPath)
        {
            if (File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
            {
                LivelyInfoModel info = JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(folderPath, "LivelyInfo.json"));
                return info != null ? 
                    new LibraryModel(info, folderPath, LibraryItemType.ready, userSettings.Settings.UIMode != LivelyGUIState.lite) : 
                    throw new Exception("Corrupted wallpaper metadata");
            }
            throw new Exception("Wallpaper not found.");
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