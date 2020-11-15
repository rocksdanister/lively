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
using System.Windows.Input;
using livelywpf.Core;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Threading;
//using System.Windows.Forms;

namespace livelywpf
{
    public class LibraryViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> wallpaperScanFolders = new List<string>() {
                Path.Combine(Program.WallpaperDir, "wallpapers"),
                Path.Combine(Program.WallpaperDir, "SaveData", "wptmp")
            };

        public LibraryViewModel()
        {
            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Add(item);
            }

            SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
            Program.SettingsVM.LivelyGUIStateChanged += SettingsVM_LivelyGUIStateChanged;
            Program.SettingsVM.LivelyWallpaperDirChange += SettingsVM_LivelyWallpaperDirChange;
            //RestoreWallpaperFromSave();

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
                        if (item.GetWallpaperData() == value)
                        {
                            if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                            {
                                found = true;
                                break;
                            }
                            else if(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.per)
                            {
                                if (ScreenHelper.ScreenCompare(item.GetScreen(), 
                                        Program.SettingsVM.Settings.SelectedDisplay,
                                        DisplayIdentificationMode.screenLayout))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            else if(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                        WallpaperSet(value);
                }
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        #endregion //collections

        #region wallpaper operations

        public bool WallpaperSet(object obj, LivelyScreen targetDisplay = null)
        {
            var selection = (LibraryModel)obj;
            bool fileExists;
            //wallpaper file is outside lively folder, always check.
            if (selection.LivelyInfo.IsAbsolutePath)
            {
                if (selection.LivelyInfo.Type == WallpaperType.url ||
                    selection.LivelyInfo.Type == WallpaperType.videostream)
                {
                    fileExists = true;
                }
                else
                {
                    fileExists = File.Exists(selection.FilePath);
                }
            }
            else
            { 
                fileExists = selection.FilePath != null;
            }

            if (!fileExists)
            {
                MessageBox.Show(Properties.Resources.TextFileNotFound, Properties.Resources.TextError);
                return false;
            }
            if (targetDisplay == null)
                SetupDesktop.SetWallpaper(selection, Program.SettingsVM.Settings.SelectedDisplay);
            else
                SetupDesktop.SetWallpaper(selection, targetDisplay);
            return true;
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
                        if (selection.LivelyInfo.Type == WallpaperType.video ||
                        selection.LivelyInfo.Type == WallpaperType.gif ||
                        selection.LivelyInfo.Type == WallpaperType.picture)
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
            SetupDesktop.TerminateWallpaper(selection);
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
                    if (string.IsNullOrEmpty(selection.LivelyInfoFolderPath))
                        return;

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

        static readonly SemaphoreSlim semaphoreSlimInstallLock = new SemaphoreSlim(1, 1);
        public async void WallpaperInstall(string livelyZipPath, bool verifyLivelyZip = true)
        {
            await semaphoreSlimInstallLock.WaitAsync();
            string installDir = null;
            try
            {
                installDir = Path.Combine(Program.WallpaperDir, "wallpapers", Path.GetRandomFileName());
                await Task.Run(() => ZipExtract.ZipExtractFile(livelyZipPath, installDir, verifyLivelyZip));
                AddWallpaper(installDir);
            }
            catch (Exception e)
            {
                try
                {
                    Directory.Delete(installDir, true);
                }
                catch { }
                Logger.Error(e.ToString());
            }
            finally
            {
                semaphoreSlimInstallLock.Release();
            }
        }

        public void WallpaperVideoConvert(object obj)
        {
            var selection = (LibraryModel)obj;
            var model = new LibraryModel(selection.LivelyInfo, selection.LivelyInfoFolderPath, LibraryTileType.videoConvert);
            //SetupDesktop.SetWallpaper(model, Screen.PrimaryScreen);
            WallpaperSet(model);
        }

        #endregion //wallpaper operations

        #region context actions

        /*
        public RelayCommand SetWallpaperItemClicked { get; set; }
        public RelayCommand DeleteWallpaperItemClicked { get; set; }
        public RelayCommand ShowDiskWallpaperItemClicked { get; set; }
        public RelayCommand ZipWallpaperItemClicked { get; set; }
        */

        #endregion //context actions

        #region helpers

        public void AddWallpaper(string path, WallpaperType wpType, LibraryTileType dataType, LivelyScreen screen)
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
                    Title = Properties.Resources.TextProcessingWallpaper + "...",
                    Type = wpType,
                    IsAbsolutePath = true,
                    FileName = path,
                    Preview = null,
                    Thumbnail = null
                };
                var model = new LibraryModel(data, dir, LibraryTileType.processing);
                LibraryItems.Insert(0, model);
                //SetupDesktop.SetWallpaper(model, screen);
                WallpaperSet(model, screen);
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

        public void SortLibraryItem(LibraryModel item)
        {
            LibraryItems.Remove(item);
            var binarySearchIndex = BinarySearch(LibraryItems, item.Title);
            //LibraryItems.Move(LibraryItems.IndexOf(item), binarySearchIndex);
            LibraryItems.Insert(binarySearchIndex, item);
        }

        /// <summary>
        /// Get LivelyProperties.json copy filepath and corresponding screen logic based on running wallpapers.
        /// null if non-customisable wallpaper.
        /// </summary>
        /// <param name="obj">LibraryModel object</param>
        /// <returns></returns>
        public Tuple<string, LivelyScreen> GetLivelyPropertyDetails(object obj)
        {
            var selection = (LibraryModel)obj;
            if (selection.LivelyPropertyPath == null)
                return null;

            string livelyPropertyCopy = "";
            LivelyScreen screen;
            var items = SetupDesktop.Wallpapers.FindAll(x => x.GetWallpaperData() == obj);
            if(items.Count == 0)
            {                
                //wallpaper not running, give the path for primaryscreen.
                screen = ScreenHelper.GetPrimaryScreen();
                try
                {
                    var dataFolder = Path.Combine(Program.WallpaperDir, "SaveData", "wpdata");
                    if (screen.DeviceNumber != null)
                    {
                        //Create a directory with the wp foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                        //Further modifications are done to the copy file.
                        var wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(selection.LivelyInfoFolderPath).Name, screen.DeviceNumber);
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
                catch (Exception e)
                {
                    //todo: fallback, use the original file (restore feature disabled.)
                    Logger.Error(e.ToString());
                }
            }
            else if(items.Count == 1)
            {
                //send regardless of selected display, if wallpaper is running on non selected display - its modified instead.
                livelyPropertyCopy = items[0].GetLivelyPropertyCopyPath();
                screen = items[0].GetScreen();
            }
            else
            {
                //more than one screen; if selected display, sendpath otherwise send the first one found.
                int index = items.FindIndex(x => ScreenHelper.ScreenCompare(Program.SettingsVM.Settings.SelectedDisplay, x.GetScreen(), DisplayIdentificationMode.screenLayout));
                livelyPropertyCopy = index == -1 ? items[0].GetLivelyPropertyCopyPath() : items[index].GetLivelyPropertyCopyPath();
                screen = index == -1 ? items[0].GetScreen() : items[index].GetScreen();
            }
            return new Tuple<string, LivelyScreen>(livelyPropertyCopy, screen);
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
            return l;//(l - 1);
        }

        #endregion //helpers

        #region setupdesktop

        public void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            bool found = false;
            foreach (var item in SetupDesktop.Wallpapers)
            {
                if (ScreenHelper.ScreenCompare(item.GetScreen(), Program.SettingsVM.Settings.SelectedDisplay, DisplayIdentificationMode.screenLayout) ||
                    Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate
                        {
                            SelectedItem = item.GetWallpaperData();
                        }));
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                   new System.Threading.ThreadStart(delegate
                   {
                       SelectedItem = null;
                   }));
            }
        }

        public void RestoreWallpaperFromSave()
        {
            //todo: remove the missing wallpaper from the save file etc..
            var wallpaperLayout = WallpaperLayoutJSON.LoadWallpaperLayout(Path.Combine(Program.AppDataDir, "WallpaperLayout.json"));
            if (wallpaperLayout == null)
            {
                return;
            }

            if(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span ||
                Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
            {
                if(wallpaperLayout.Count != 0)
                {
                    var libraryItem = LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(wallpaperLayout[0].LivelyInfoPath));
                    WallpaperSet(libraryItem, ScreenHelper.GetPrimaryScreen());
                }
            }
            else
            {
                foreach (var layout in wallpaperLayout)
                {
                    var libraryItem = LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(layout.LivelyInfoPath));
                    var screen = ScreenHelper.GetScreen(layout.LivelyScreen.DeviceName, layout.LivelyScreen.Bounds, layout.LivelyScreen.WorkingArea, DisplayIdentificationMode.screenLayout);
                    if (libraryItem != null && screen != null)
                    {
                        Logger.Info("Restoring Wallpaper: " + libraryItem.Title + " " + libraryItem.LivelyInfoFolderPath);
                        WallpaperSet(libraryItem, screen);
                    }
                }
            }
        }

        #endregion //setupdesktop

        #region settings changed

        private void SettingsVM_LivelyGUIStateChanged(object sender, LivelyGUIState mode)
        {
            if (mode == LivelyGUIState.normal)
            {
                foreach (var item in LibraryItems)
                {
                    item.ImagePath = File.Exists(item.PreviewClipPath) ? item.PreviewClipPath : item.ThumbnailPath;
                }
            }
            else if (mode == LivelyGUIState.lite)
            {
                foreach (var item in LibraryItems)
                {
                    item.ImagePath = item.ThumbnailPath;
                }
            }
        }

        private void SettingsVM_LivelyWallpaperDirChange(object sender, string dir)
        {
            LibraryItems.Clear();
            wallpaperScanFolders.Clear();
            wallpaperScanFolders.Add(Path.Combine(dir, "wallpapers"));
            wallpaperScanFolders.Add(Path.Combine(dir, "SaveData", "wptmp"));

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Add(item);
            }
        }

        #endregion //settings changed
    }
}
