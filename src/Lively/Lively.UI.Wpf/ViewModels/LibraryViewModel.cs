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

namespace Lively.UI.Wpf.ViewModels
{
    public class LibraryViewModel : ObservableObject
    {
        //TEST
        public static string WallpaperDir = @"C:\Users\rocks\AppData\Local\Lively Wallpaper_v2\Library\";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> wallpaperScanFolders = new List<string>() {
                Path.Combine(WallpaperDir, "wallpapers"),
                Path.Combine(WallpaperDir, "saveData", "wptmp")
            };

        private readonly IDesktopCoreClient desktopCore;

        public LibraryViewModel(IDesktopCoreClient desktopCore)
        {
            this.desktopCore = desktopCore;

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Add(item);
            }
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
                _ = desktopCore.SetWallpaper(value.LivelyInfoFolderPath, desktopCore.DisplayMonitors[0].DeviceId);
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        #endregion //collections

        #region helpers

        public void AddWallpaper(string folderPath)
        {
            var libItem = ScanWallpaperFolder(folderPath);
            if (libItem != null)
            {
                var binarySearchIndex = BinarySearch(LibraryItems, libItem.Title);
                LibraryItems.Insert(binarySearchIndex, libItem);
            }
        }

        /// <summary>
        /// Load wallpapers from the given parent folder(), only top directory is scanned.
        /// </summary>
        /// <param name="folderPaths">Parent folders to search for subdirectories.</param>
        /// <returns>Sorted(based on Title) wallpaper data.</returns>
        private List<LibraryModel> ScanWallpaperFolders(List<string> folderPaths)
        {
            List<String[]> dir = new List<string[]>();
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
            List<LibraryModel> tmpLibItems = new List<LibraryModel>();

            for (int i = 0; i < dir.Count; i++)
            {
                for (int j = 0; j < dir[i].Length; j++)
                {
                    var currDir = dir[i][j];
                    var libItem = ScanWallpaperFolder(currDir);
                    if (libItem != null)
                    {
                        tmpLibItems.Add(libItem);
                    }
                }
            }
            return SortWallpapers(tmpLibItems);
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

        public void SortLibraryItem(LibraryModel item)
        {
            LibraryItems.Remove(item);
            var binarySearchIndex = BinarySearch(LibraryItems, item.Title);
            //LibraryItems.Move(LibraryItems.IndexOf(item), binarySearchIndex);
            LibraryItems.Insert(binarySearchIndex, item);
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
