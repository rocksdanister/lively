using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;

namespace livelywpf
{
    public class LibraryViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string[] wallpaperScanFolders = new string[] {
                Path.Combine(Program.LivelyDir, "wallpapers"),
                Path.Combine(Program.LivelyDir, "SaveData", "wptmp")
            };

        public LibraryViewModel()
        {
            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                //LibraryItems.Insert(BinarySearch(LibraryItems, item.Title), item);
                LibraryItems.Add(item);
            }
        }

        private ObservableCollection<LibraryModel> _libraryItems = new ObservableCollection<LibraryModel>();
        public ObservableCollection<LibraryModel> LibraryItems
        {
            get { return _libraryItems; }
            set
            {
                if (value != _libraryItems)
                {
                    _libraryItems = value;
                    OnPropertyChanged("LibraryItems");
                }
            }
        }

        public ICollectionView LibraryItemsFiltered
        {
            get { return CollectionViewSource.GetDefaultView(LibraryItems); }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {   
                _searchText = value;
                FilterCollection(_searchText);
                OnPropertyChanged("SearchText");
            }
        }

        private LibraryModel _selectedItem;
        public LibraryModel SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                SetupDesktop.SetWallpaper(_selectedItem, Screen.PrimaryScreen);
                OnPropertyChanged("SelectedItem");
            }
        }

        #region helpers

        public void FilterCollection(string str)
        {
            if (String.IsNullOrEmpty(str))
                LibraryItemsFiltered.Filter = null;

            LibraryItemsFiltered.Filter = i => (((LibraryModel)i).LivelyInfo.Title.IndexOf(str, StringComparison.OrdinalIgnoreCase)) > -1;
            LibraryItemsFiltered.Refresh();
        }

        /// <summary>
        /// Load wallpapers from the given parent folder(), only top directory is scanned.
        /// </summary>
        /// <param name="folderPaths">Parent folders to search for subdirectories.</param>
        /// <returns>Sorted(based on Title) wallpaper data.</returns>
        private List<LibraryModel> ScanWallpaperFolders(string[] folderPaths)
        {
            List<String[]> dir = new List<string[]>();
            for (int i = 0; i < folderPaths.Length; i++)
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
            return SortWallpaper(tmpLibItems);
        }
        
        private LibraryModel ScanWallpaperFolder(string folderPath)
        {
            if (File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
            {
                LivelyInfoModel info = LivelyInfoJSON.LoadWallpaperMetaData(Path.Combine(folderPath, "LivelyInfo.json"));
                if (info != null)
                {
                    if (info.Type == WallpaperType.videostream || info.Type == WallpaperType.url)
                    {
                        //online content, no file.
                        Logger.Info("Loading Wallpaper (no-file):- " + info.FileName + " " + info.Type);
                        return new LibraryModel(info, null);
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
        
        private List<LibraryModel> SortWallpaper(List<LibraryModel> data)
        {
            try
            {
                return data.OrderBy(x => x.LivelyInfo.Title).ToList();
            }
            catch(ArgumentNullException)
            {
                return null;
            }
        }

        private async void InstallWallpaper(string livelyZipPath)
        {
            var installDir = Path.Combine(Program.LivelyDir, "wallpapers", Path.GetRandomFileName());
            await Task.Run(() =>
            {
                try
                {
                    ZipExtract.ZipExtractFile(livelyZipPath, installDir, true);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    try
                    {
                        Directory.Delete(installDir, true);
                    }
                    catch { }
                }
            });

            var libItem = ScanWallpaperFolder(installDir);
            if (libItem != null)
            {
                var binarySearchIndex = BinarySearch(LibraryItems, libItem.Title);
                LibraryItems.Insert(binarySearchIndex, libItem);
            }       
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
                m = (l+r)/ 2;

                res = String.Compare(x, item[m].Title); 

                if (res == 0)
                    return m;

                if (res > 0)
                    l = m + 1;

                else
                    r = m - 1;
            }
            return l;
        }

        #endregion helpers
    }
}
