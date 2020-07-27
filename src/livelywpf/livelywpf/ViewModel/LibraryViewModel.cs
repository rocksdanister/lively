using ICSharpCode.SharpZipLib.Zip;
using Octokit;
using System;
using System.CodeDom;
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
                Path.Combine(Program.WallpaperDir, "wallpapers"),
                Path.Combine(Program.WallpaperDir, "SaveData", "wptmp")
            };

        public LibraryViewModel()
        {
            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                //LibraryItems.Insert(BinarySearch(LibraryItems, item.Title), item);
                LibraryItems.Add(item);
            }

            SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
            //not possible currently, turn on when possible.
            //ref: https://github.com/microsoft/microsoft-ui-xaml/issues/911
            //SetWallpaperItemClicked = new RelayCommand(WallpaperSet);
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
                if (value != null)
                {
                    bool found = false;
                    foreach (var item in SetupDesktop.Wallpapers)
                    {
                        found = item.GetWallpaperData() == value;
                    }
                    if(!found)
                        WallpaperSet(value);
                }
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        #endregion collections

        #region wallpaper operations

        public void WallpaperSet(object obj)
        {
            var selection = (LibraryModel)obj;
            SetupDesktop.SetWallpaper(selection, Screen.PrimaryScreen);
        }

        public void WallpaperSendMsg(object obj, string message)
        {
            var selection = (LibraryModel)obj;
            //currently enabled for these types.
            if (selection.LivelyInfo.Type == WallpaperType.web 
            || selection.LivelyInfo.Type == WallpaperType.webaudio)
            {
                SetupDesktop.SendMessageWallpaper(selection, message);
            }
        }

        public void WallpaperShowOnDisk(object obj)
        {
            var selection = (LibraryModel)obj;
            string folderPath;
            if (selection.LivelyInfo.Type == WallpaperType.url 
            || selection.LivelyInfo.Type == WallpaperType.videostream)
            {
                folderPath = selection.LivelyInfoFolderPath;
            }
            else
            {
                folderPath = selection.FilePath;
            }
            FileOperations.OpenFolder(folderPath);
        }

        public async void WallpaperExport(object obj, string saveFile)
        {
            var selection = (LibraryModel)obj;
            await Task.Run(() =>
            {
                try
                {           
                    if(selection.LivelyInfo.Type == WallpaperType.videostream 
                    || selection.LivelyInfo.Type == WallpaperType.url)
                    {
                        //no wallpaper file on disk, only wallpaper metadata.
                        var tmpDir = Path.Combine(Program.AppDataDir, "temp");
                        FileOperations.EmptyDirectory(tmpDir);
                        LivelyInfoModel info = new LivelyInfoModel(selection.LivelyInfo)
                        {
                            IsAbsolutePath = false
                        };

                        //..changing absolute filepaths to relative, FileName is not modified since its url.
                        if (selection.ThumbnailPath != null)
                        {
                            File.Copy(selection.ThumbnailPath, Path.Combine(tmpDir, Path.GetFileName(selection.ThumbnailPath)));
                            info.Thumbnail = Path.GetFileName(selection.ThumbnailPath);
                        }
                        if (selection.PreviewClipPath != null)
                        {
                            File.Copy(selection.PreviewClipPath, Path.Combine(tmpDir, Path.GetFileName(selection.PreviewClipPath)));
                            info.Preview = Path.GetFileName(selection.PreviewClipPath);
                        }
                        LivelyInfoJSON.SaveWallpaperMetaData(info, Path.Combine(tmpDir, "LivelyInfo.json"));

                        ZipCreate.CreateZip(saveFile, new List<string>() { tmpDir });
                    }
                    else if(selection.LivelyInfo.IsAbsolutePath)
                    {
                        //livelyinfo.json only contains the absolute filepath of the file; file is in different location.
                        List<string> files = new List<string>();
                        if (selection.LivelyInfo.Type == WallpaperType.video || selection.LivelyInfo.Type == WallpaperType.gif)
                        {
                            files.Add(selection.FilePath);
                        }
                        else
                        {
                            files.AddRange(Directory.GetFiles(Directory.GetParent(selection.FilePath).ToString(), "*.*", SearchOption.AllDirectories));
                        }

                        var tmpDir = Path.Combine(Program.AppDataDir, "temp");
                        FileOperations.EmptyDirectory(tmpDir);
                        LivelyInfoModel info = new LivelyInfoModel(selection.LivelyInfo)
                        {
                            IsAbsolutePath = false
                        };
                        info.FileName = Path.GetFileName(info.FileName);

                        //..changing absolute filepaths to relative.
                        if (selection.ThumbnailPath != null)
                        {
                            File.Copy(selection.ThumbnailPath, Path.Combine(tmpDir, Path.GetFileName(selection.ThumbnailPath)));
                            info.Thumbnail = Path.GetFileName(selection.ThumbnailPath);
                        }
                        if (selection.PreviewClipPath != null)
                        {
                            File.Copy(selection.PreviewClipPath, Path.Combine(tmpDir, Path.GetFileName(selection.PreviewClipPath)));
                            info.Preview = Path.GetFileName(selection.PreviewClipPath);
                        }
                        LivelyInfoJSON.SaveWallpaperMetaData(info, Path.Combine(tmpDir, "LivelyInfo.json"));

                        List<string> metaData = new List<string>();
                        metaData.AddRange(Directory.GetFiles(tmpDir, "*.*", SearchOption.TopDirectoryOnly));
                        var fileData = new List<ZipCreate.FileData>
                        {
                            new ZipCreate.FileData() { Files = metaData, ParentDirectory = tmpDir },
                            new ZipCreate.FileData() { Files = files, ParentDirectory = Directory.GetParent(selection.FilePath).ToString() }
                        };
         
                        ZipCreate.CreateZip(saveFile, fileData);
                    }
                    else 
                    {
                        //installed lively wallpaper.
                        ZipCreate.CreateZip(saveFile, new List<string>() { Path.GetDirectoryName(selection.FilePath) });
                    }
                    FileOperations.OpenFolder(saveFile);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            });
        }

        public async void WallpaperDelete(object obj)
        {
            var selection = (LibraryModel)obj;
            //close if running.
            SetupDesktop.CloseWallpaper(selection);
            //delete wp folder.      
            var success = await FileOperations.DeleteDirectoryAsync(selection.LivelyInfoFolderPath, 1000, 4000);

            if (success)
            {
                if (SelectedItem == selection)
                {
                    SelectedItem = null;
                }
                //remove from library.
                LibraryItems.Remove(selection);
                try
                {
                    //Delete LivelyProperties.json backup folder.
                    string[] wpdataDir = Directory.GetDirectories(Path.Combine(Program.WallpaperDir, "SaveData", "wpdata"));
                    var wpFolderName = new System.IO.DirectoryInfo(selection.LivelyInfoFolderPath).Name;
                    for (int i = 0; i < wpdataDir.Length; i++)
                    {
                        var item = new System.IO.DirectoryInfo(wpdataDir[i]).Name;
                        if (wpFolderName.Equals(item, StringComparison.Ordinal))
                        {
                            _ = FileOperations.DeleteDirectoryAsync(wpdataDir[i], 1000, 4000);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
        }

        public async void WallpaperInstall(string livelyZipPath)
        {
            var installDir = Path.Combine(Program.WallpaperDir, "wallpapers", Path.GetRandomFileName());
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

            AddWallpaper(installDir);
        }

        public void WallpaperVideoConvert(object obj)
        {
            var selection = (LibraryModel)obj;
            var model = new LibraryModel(selection.LivelyInfo, selection.LivelyInfoFolderPath, LibraryTileType.videoConvert);
            SetupDesktop.SetWallpaper(model, Screen.PrimaryScreen);
        }

        #endregion wallpaper operations

        #region contextmenu

        public RelayCommand SetWallpaperItemClicked { get; set; }
        public RelayCommand DeleteWallpaperItemClicked { get; set; }
        public RelayCommand ShowDiskWallpaperItemClicked { get; set; }
        public RelayCommand ZipWallpaperItemClicked { get; set; }

        #endregion contextmenu

        #region helpers

        public void AddWallpaper(string path, WallpaperType wpType, LibraryTileType dataType, Screen screen)
        {
            var dir = Path.Combine(Program.WallpaperDir, "SaveData", "wptmp", Path.GetRandomFileName());
            if (dataType == LibraryTileType.processing)
            {
                //Preview gif and thumbnail to be captured..
                //Create a tile at index 0, updates value realtime.
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    return;
                }
                var data = new LivelyInfoModel() { 
                    Title = "Processing..",
                    Type = wpType,
                    IsAbsolutePath = true,
                    FileName = path,
                    Preview = null,
                    Thumbnail = null
                };
                var model = new LibraryModel(data, dir, LibraryTileType.processing);
                LibraryItems.Insert(0, model);
                SetupDesktop.SetWallpaper(model, screen);
            }
        }

        public void AddWallpaper(string folderPath)
        {
            var libItem = ScanWallpaperFolder(folderPath);
            if (libItem != null)
            {
                var binarySearchIndex = BinarySearch(LibraryItems, libItem.Title);
                LibraryItems.Insert(binarySearchIndex, libItem);
            }
        }

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
            return SortWallpapers(tmpLibItems);
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
            catch(ArgumentNullException)
            {
                return null;
            }
        }

        public void SortExistingWallpaper(LibraryModel item)
        {
            var binarySearchIndex = BinarySearch(LibraryItems, item.Title);
            LibraryItems.Move(LibraryItems.IndexOf(item), binarySearchIndex);
        }

        /// <summary>
        /// Get LivelyProperties.json copy filepath for currently running screen if wallpaper is active, otherwise returns for primaryscreen.
        /// null if non-customisable wallpaper.
        /// </summary>
        /// <param name="obj">LibraryModel object</param>
        /// <returns></returns>
        public string GetLivelyPropertyCopyPath(object obj)
        {
            var selection = (LibraryModel)obj;
            if (selection.LivelyPropertyPath == null)
                return null;

            foreach (var item in SetupDesktop.Wallpapers)
            {
                if(item.GetWallpaperData() == selection)
                {
                    return item.GetLivelyPropertyCopyPath();
                }
            }

            //wallpaper not running, give the path for primaryscreen.
            string livelyPropertyCopy = "";
            try
            {
                var screenNumber = ScreenHelper.GetScreenNumber(Screen.PrimaryScreen);
                var dataFolder = Path.Combine(Program.WallpaperDir, "SaveData", "wpdata");
                if (screenNumber != null)
                {
                    //Create a directory with the wp foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                    //Further modifications are done to the copy file.
                    var wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(selection.LivelyInfoFolderPath).Name, screenNumber);
                    Directory.CreateDirectory(wpdataFolder);

                    livelyPropertyCopy = Path.Combine(wpdataFolder, "LivelyProperties.json");
                    if (!File.Exists(livelyPropertyCopy))
                        File.Copy(selection.LivelyPropertyPath, livelyPropertyCopy);

                }
                else
                {
                    //todo: fallback, use the original file (restore feature disabled.)
                }
            }
            catch
            {
                //todo: fallback, use the original file (restore feature disabled.)
            }

            return livelyPropertyCopy;
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

        #region setupdesktop

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Wallpaper Changed!");
            foreach (var item in SetupDesktop.Wallpapers)
            {
                //testing, check if it is the user selected screen in the ui/display mode etc..
                if (true)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate
                        {
                            SelectedItem = item.GetWallpaperData();
                        }));
                    break;
                }
            }

            if (SetupDesktop.Wallpapers.Count == 0)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                   new System.Threading.ThreadStart(delegate
                   {
                       SelectedItem = null;
                   }));
            }
        }

        public void RestoreWallpaper()
        {
            //todo: remove the missing wallpaper from the save file etc..
            var layout = WallpaperLayoutJSON.LoadWallpaperLayout(Path.Combine(Program.WallpaperDir, "wallpaper_layout.json"));
            if (layout == null)
            {
                return;
            }

            foreach (var item in layout)
            {
                var found = LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(item.LivelyInfoPath));
                var screen = ScreenHelper.GetScreen(item.DeviceName, item.Bounds, item.WorkingArea, DisplayIdentificationMode.screenLayout);
                if (found != null && screen != null)
                {
                    SetupDesktop.SetWallpaper(found, screen);
                }
            }
        }

        #endregion setupdesktop
    }
}
