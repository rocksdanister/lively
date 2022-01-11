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
        //TEST, TODO: change in LivelyPropertisView also
        public static string WallpaperDir = @"C:\Users\rocks\AppData\Local\Lively Wallpaper_v2\Library\";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> wallpaperScanFolders = new List<string>() {
                Path.Combine(WallpaperDir, "wallpapers"),
                Path.Combine(WallpaperDir, "saveData", "wptmp")
            };

        private readonly IDesktopCoreClient desktopCore;
        private readonly IUserSettingsClient userSettings;
        private readonly IDisplayManagerClient displayManager;

        public LibraryViewModel(IDesktopCoreClient desktopCore, IDisplayManagerClient displayManager, IUserSettingsClient userSettings)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.userSettings = userSettings;

            foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
            {
                LibraryItems.Add(item);
            }

            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
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
                //_ = desktopCore.SetWallpaper(value.LivelyInfoFolderPath, displayManager.DisplayMonitors[0].DeviceId);
                if (value != null)
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

        #region helpers

        public void WallpaperShowOnDisk(ILibraryModel libraryItem)
        {
            string folderPath =
                libraryItem.LivelyInfo.Type == WallpaperType.url || libraryItem.LivelyInfo.Type == WallpaperType.videostream
                ? libraryItem.LivelyInfoFolderPath
                : libraryItem.FilePath;
            FileOperations.OpenFolder(folderPath);
        }

        public async Task WallpaperDelete(ILibraryModel obj)
        {
            //close if running.
            await desktopCore.CloseWallpaper(obj, true);
            //delete wp folder.      
            var success = await FileOperations.DeleteDirectoryAsync(obj.LivelyInfoFolderPath, 1000, 4000);

            if (success)
            {
                if (SelectedItem == obj)
                {
                    SelectedItem = null;
                }
                //remove from library.
                LibraryItems.Remove((LibraryModel)obj);
                try
                {
                    if (string.IsNullOrEmpty(obj.LivelyInfoFolderPath))
                        return;

                    //Delete LivelyProperties.json backup folder.
                    string[] wpdataDir = Directory.GetDirectories(Path.Combine(WallpaperDir, "SaveData", "wpdata"));
                    var wpFolderName = new DirectoryInfo(obj.LivelyInfoFolderPath).Name;
                    for (int i = 0; i < wpdataDir.Length; i++)
                    {
                        var item = new DirectoryInfo(wpdataDir[i]).Name;
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

        public async Task WallpaperExport(ILibraryModel libraryItem, string saveFile)
        {
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

                    if (libraryItem.LivelyInfo.Type == WallpaperType.videostream
                        || libraryItem.LivelyInfo.Type == WallpaperType.url)
                    {
                        //no wallpaper file on disk, only wallpaper metadata.
                        var tmpDir = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName());
                        try
                        {
                            Directory.CreateDirectory(tmpDir);
                            LivelyInfoModel info = new LivelyInfoModel(libraryItem.LivelyInfo)
                            {
                                IsAbsolutePath = false
                            };

                            //..changing absolute filepaths to relative, FileName is not modified since its url.
                            if (libraryItem.ThumbnailPath != null)
                            {
                                File.Copy(libraryItem.ThumbnailPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.ThumbnailPath)));
                                info.Thumbnail = Path.GetFileName(libraryItem.ThumbnailPath);
                            }
                            if (libraryItem.PreviewClipPath != null)
                            {
                                File.Copy(libraryItem.PreviewClipPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.PreviewClipPath)));
                                info.Preview = Path.GetFileName(libraryItem.PreviewClipPath);
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
                    else if (libraryItem.LivelyInfo.IsAbsolutePath)
                    {
                        //livelyinfo.json only contains the absolute filepath of the file; file is in different location.
                        var tmpDir = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName());
                        try
                        {
                            Directory.CreateDirectory(tmpDir);
                            List<string> files = new List<string>();
                            if (libraryItem.LivelyInfo.Type == WallpaperType.video ||
                            libraryItem.LivelyInfo.Type == WallpaperType.gif ||
                            libraryItem.LivelyInfo.Type == WallpaperType.picture)
                            {
                                files.Add(libraryItem.FilePath);
                            }
                            else
                            {
                                files.AddRange(Directory.GetFiles(Directory.GetParent(libraryItem.FilePath).ToString(), "*.*", SearchOption.AllDirectories));
                            }

                            LivelyInfoModel info = new LivelyInfoModel(libraryItem.LivelyInfo)
                            {
                                IsAbsolutePath = false
                            };
                            info.FileName = Path.GetFileName(info.FileName);

                            //..changing absolute filepaths to relative.
                            if (libraryItem.ThumbnailPath != null)
                            {
                                File.Copy(libraryItem.ThumbnailPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.ThumbnailPath)));
                                info.Thumbnail = Path.GetFileName(libraryItem.ThumbnailPath);
                            }
                            if (libraryItem.PreviewClipPath != null)
                            {
                                File.Copy(libraryItem.PreviewClipPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.PreviewClipPath)));
                                info.Preview = Path.GetFileName(libraryItem.PreviewClipPath);
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
                                new ZipCreate.FileData() { Files = files, ParentDirectory = Directory.GetParent(libraryItem.FilePath).ToString() }
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
                        ZipCreate.CreateZip(saveFile, new List<string>() { Path.GetDirectoryName(libraryItem.FilePath) });
                    }
                    FileOperations.OpenFolder(saveFile);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            });
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
