using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using livelywpf.Core;
using System.Threading;
using livelywpf.Helpers.Files;
using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.Storage;
using livelywpf.Helpers.Archive;
using livelywpf.Helpers;
using livelywpf.Models;
using livelywpf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace livelywpf.ViewModels
{
    public class LibraryViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<string> wallpaperScanFolders = new List<string>() {
                Path.Combine(Program.WallpaperDir, "wallpapers"),
                Path.Combine(Program.WallpaperDir, "SaveData", "wptmp")
            };
        private readonly IDesktopCore desktopCore;
        private readonly IUserSettingsService userSettings;
        private readonly SettingsViewModel settingsVm;

        public LibraryViewModel(IUserSettingsService userSettings, IDesktopCore desktopCore, SettingsViewModel settingsVm)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.settingsVm = settingsVm;

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Add(item);
            }
            //Unused for now, several issues need fixing.
            //LibraryItemsFiltered = new ObservableCollection<LibraryModel>(LibraryItems);

            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;
            settingsVm.LivelyGUIStateChanged += SettingsVM_LivelyGUIStateChanged;
            settingsVm.LivelyWallpaperDirChange += SettingsVM_LivelyWallpaperDirChange;

            //ref: https://github.com/microsoft/microsoft-ui-xaml/issues/911
            //SetWallpaperItemClicked = new RelayCommand(WallpaperSet);
        }

        #region collections

        private ObservableCollection<ILibraryModel> _libraryItems = new ObservableCollection<ILibraryModel>();
        public ObservableCollection<ILibraryModel> LibraryItems
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

        private ObservableCollection<ILibraryModel> _libraryItemsFiltered;
        public ObservableCollection<ILibraryModel> LibraryItemsFiltered
        {
            get { return _libraryItemsFiltered; }
            set
            {
                if (value != _libraryItemsFiltered)
                {
                    _libraryItemsFiltered = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {   
                _searchText = value;
                FilterCollection(_searchText);
                OnPropertyChanged();
            }
        }

        private ILibraryModel _selectedItem;
        public ILibraryModel SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                if (value != null)
                {
                    var wp = desktopCore.Wallpapers.Where(x => x.Model == value);
                    if (wp.Count() > 0)
                    {
                        switch (userSettings.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                                if (!wp.Any(x => userSettings.Settings.SelectedDisplay.Equals(x.Screen)))
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

        #region wallpaper operations

        public void WallpaperShowOnDisk(object obj)
        {
            var selection = (ILibraryModel)obj;
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
            var selection = (ILibraryModel)obj;
            await Task.Run(() =>
            {
                try
                {
                    //title ending with '.' can have diff extension (example: parallax.js) or
                    //user made a custom filename with diff extension.
                    if (Path.GetExtension(saveFile) != ".zip")
                    {
                        saveFile += ".zip";
                    }

                    if (selection.LivelyInfo.Type == WallpaperType.videostream 
                        || selection.LivelyInfo.Type == WallpaperType.url)
                    {
                        //no wallpaper file on disk, only wallpaper metadata.
                        var tmpDir = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName());
                        try
                        {
                            Directory.CreateDirectory(tmpDir);
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

                            try
                            {
                                JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(tmpDir, "LivelyInfo.json"), info);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e.ToString());
                            }

                            ZipCreate.CreateZip(saveFile, new List<string>() { tmpDir });
                        }
                        finally
                        {
                            _ = FileOperations.DeleteDirectoryAsync(tmpDir, 1000, 2000);
                        }
                    }
                    else if(selection.LivelyInfo.IsAbsolutePath)
                    {
                        //livelyinfo.json only contains the absolute filepath of the file; file is in different location.
                        var tmpDir = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName());
                        try
                        {
                            Directory.CreateDirectory(tmpDir);
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

                            try
                            {
                                JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(tmpDir, "LivelyInfo.json"), info);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e.ToString());
                            }

                            List<string> metaData = new List<string>();
                            metaData.AddRange(Directory.GetFiles(tmpDir, "*.*", SearchOption.TopDirectoryOnly));
                            var fileData = new List<ZipCreate.FileData>
                            {
                                new ZipCreate.FileData() { Files = metaData, ParentDirectory = tmpDir },
                                new ZipCreate.FileData() { Files = files, ParentDirectory = Directory.GetParent(selection.FilePath).ToString() }
                            };

                            ZipCreate.CreateZip(saveFile, fileData);
                        }
                        finally
                        {
                            _ = FileOperations.DeleteDirectoryAsync(tmpDir, 1000, 2000);
                        }
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
            var selection = (ILibraryModel)obj;
            //close if running.
            desktopCore.CloseWallpaper(selection, true);
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
        public async Task WallpaperInstall(string livelyZipPath, bool verifyLivelyZip = true)
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
            var selection = (ILibraryModel)obj;
            var model = new LibraryModel(selection.LivelyInfo, selection.LivelyInfoFolderPath, LibraryTileType.videoConvert, userSettings.Settings.LivelyGUIRendering == LivelyGUIState.normal);
            desktopCore.SetWallpaper(model, userSettings.Settings.SelectedDisplay);
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

        public void AddWallpaper(string path, WallpaperType wpType, LibraryTileType dataType, ILivelyScreen screen, string cmdArgs = null)
        {
            var dir = Path.Combine(Program.WallpaperDir, "SaveData", "wptmp", Path.GetRandomFileName());
            if (dataType == LibraryTileType.processing || 
                dataType == LibraryTileType.cmdImport ||
                dataType == LibraryTileType.multiImport)
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
                    Thumbnail = null,
                    Arguments = cmdArgs
                };
                var model = new LibraryModel(data, dir, dataType, userSettings.Settings.LivelyGUIRendering == LivelyGUIState.normal);
                LibraryItems.Insert(0, model);
                desktopCore.SetWallpaper(model, screen);
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

        public void EditWallpaper(ILibraryModel obj)
        {
            //Kill wp if running..
            desktopCore.CloseWallpaper(obj, true);
            LibraryItems.Remove(obj);
            obj.DataType = LibraryTileType.edit;
            LibraryItems.Insert(0, obj);
            desktopCore.SetWallpaper(obj, ScreenHelper.GetPrimaryScreen());
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
                        return new LibraryModel(info, folderPath, LibraryTileType.ready, userSettings.Settings.LivelyGUIRendering == LivelyGUIState.normal);
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
                        return new LibraryModel(info, folderPath, LibraryTileType.ready, userSettings.Settings.LivelyGUIRendering == LivelyGUIState.normal);
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

        //ref: https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/listview-filtering
        private void FilterCollection(string str)
        {
            /*In order for the ListView to animate in the most intuitive way when adding and subtracting items, 
            it's important to remove and add items to the ListView's ItemsSource collection itself*/
            var tmpFilter = LibraryItems.Where(item => item.LivelyInfo.Title.Contains(str, StringComparison.OrdinalIgnoreCase)).ToList();
            // First, remove any objects in LibraryItemsFiltered that are not in tmpFilter
            for (int i = 0; i < LibraryItemsFiltered.Count; i++)
            {
                var item = LibraryItemsFiltered[i];
                if (!tmpFilter.Contains(item))
                {
                    LibraryItemsFiltered.Remove(item);
                }
            }

            /* Next, add back any objects that are included in tmpFilter and may 
            not currently be in LibraryItemsFiltered (in case of a backspace) */
            for (int i = 0; i < tmpFilter.Count; i++)
            {
                var item = tmpFilter[i];
                if (!LibraryItemsFiltered.Contains(item))
                {
                    var index = BinarySearch(LibraryItemsFiltered, item.Title);
                    LibraryItemsFiltered.Insert(index, item);
                }
            }
        }

        private int BinarySearch(ObservableCollection<ILibraryModel> item, string x)
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
            System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            new System.Threading.ThreadStart(delegate
            {
                SelectedItem = userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span && desktopCore.Wallpapers.Count > 0 ?
                    desktopCore.Wallpapers[0].Model :
                    desktopCore.Wallpapers.FirstOrDefault(wp => userSettings.Settings.SelectedDisplay.Equals(wp.Screen))?.Model;
            }));
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

        //todo: do it automatically using filesystem watcher..
        /// <summary>
        /// Rescans wallpaper directory and update library.
        /// </summary>
        public void WallpaperDirectoryUpdate()
        {
            LibraryItems.Clear();
            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Add(item);
            }
        }

        #endregion //settings changed
    }
}
