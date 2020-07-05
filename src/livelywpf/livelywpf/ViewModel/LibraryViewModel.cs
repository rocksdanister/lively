using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;

namespace livelywpf
{
    public class LibraryViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public LibraryViewModel()
        {
            string[] wallpaperScanFolders = new string[] {
                Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper", "wallpapers"),
                Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper", "SaveData", "wptmp")
            };

            foreach (var item in ScanWallpaperFolder(wallpaperScanFolders))
            {
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
        /// <returns>Wallpaper data sorted by Title value.</returns>
        private List<LibraryModel> ScanWallpaperFolder(string[] folderPaths)
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
                    if (File.Exists(Path.Combine(currDir, "LivelyInfo.json")))
                    {
                        LivelyInfoModel info = LivelyInfoJSON.LoadWallpaperMetaData(Path.Combine(currDir, "LivelyInfo.json"));
                        if (info != null)
                        {
                            if (info.Type == WallpaperType.videostream || info.Type == WallpaperType.url)
                            {
                                //online content, no file.
                                Logger.Info("Loading Wallpaper (no-file):- " + info.FileName + " " + info.Type);
                                tmpLibItems.Add(new LibraryModel(info, null));
                            }
                            else
                            {
                                if (info.IsAbsolutePath)
                                {
                                    Logger.Info("Loading Wallpaper(absolute):- " + info.FileName + " " + info.Type);
                                }
                                else
                                {
                                    Logger.Info("Loading Wallpaper(relative):- " + Path.Combine(currDir, info.FileName) + " " + info.Type);
                                }
                                tmpLibItems.Add(new LibraryModel(info, currDir));
                            }
                        }
                    }
                    else
                    {
                        Logger.Info("Not a lively wallpaper folder, skipping:- " + currDir);
                    }
                }
            }
            return SortWallpaper(tmpLibItems);
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

        #endregion helpers
    }
}
