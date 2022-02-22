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
using System.Windows.Threading;

namespace Lively.UI.Wpf.ViewModels
{
    public class LibraryViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<string> wallpaperScanFolders;

        private readonly IDesktopCoreClient desktopCore;
        private readonly IUserSettingsClient userSettings;
        private readonly IDisplayManagerClient displayManager;
        private readonly SettingsViewModel settingsVm;

        public LibraryViewModel(IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            IUserSettingsClient userSettings,
            SettingsViewModel settingsVm)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.settingsVm = settingsVm;

            wallpaperScanFolders = new List<string>
            {
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
            };

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Insert(BinarySearch(LibraryItems, item.Title), item);
            }

            var selectedWallpaper = desktopCore.Wallpapers.FirstOrDefault(x => x.Display.Equals(userSettings.Settings.SelectedDisplay));
            if (selectedWallpaper != null)
            {
                //Select already running item when UI program is started again..
                SelectedItem = LibraryItems.FirstOrDefault(x => selectedWallpaper.LivelyInfoFolderPath == x.LivelyInfoFolderPath);
            }

            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            settingsVm.WallpaperDirChanged += SettingsVm_WallpaperDirChanged;
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
                    var wallpapers = desktopCore.Wallpapers.Where(x => x.LivelyInfoFolderPath == value.LivelyInfoFolderPath);
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

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Threading.ThreadStart(delegate
                {
                    UpdateSelection();
                }));
        }

        /// <summary>
        /// Update library selected item based on selected display.
        /// </summary>
        public void UpdateSelection()
        {
            if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span && desktopCore.Wallpapers.Count > 0)
            {
                SelectedItem = LibraryItems.FirstOrDefault(x => desktopCore.Wallpapers[0].LivelyInfoFolderPath == x.LivelyInfoFolderPath);
            }
            else
            {
                var wp = desktopCore.Wallpapers.FirstOrDefault(x => userSettings.Settings.SelectedDisplay.Equals(x.Display));
                SelectedItem = LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath == wp?.LivelyInfoFolderPath);
            }
        }

        private void SettingsVm_WallpaperDirChanged(object sender, string dir)
        {
            LibraryItems.Clear();
            wallpaperScanFolders.Clear();
            wallpaperScanFolders.Add(Path.Combine(dir, Constants.CommonPartialPaths.WallpaperInstallDir));
            wallpaperScanFolders.Add(Path.Combine(dir, Constants.CommonPartialPaths.WallpaperInstallTempDir));

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Insert(BinarySearch(LibraryItems, item.Title), item);
            }
        }

        #region helpers

        public ILibraryModel AddWallpaper(string folderPath)
        {
            var libItem = ScanWallpaperFolder(folderPath);
            if (libItem != null)
            {
                var binarySearchIndex = BinarySearch(LibraryItems, libItem.Title);
                LibraryItems.Insert(binarySearchIndex, libItem);
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
                        return new LibraryModel(info, folderPath);
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
                        return new LibraryModel(info, folderPath);
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
                var binarySearchIndex = BinarySearch(LibraryItems, item.Title);
                //LibraryItems.Move(LibraryItems.IndexOf(item), binarySearchIndex);
                LibraryItems.Insert(binarySearchIndex, item);
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

        #endregion //helpers
    }
}
