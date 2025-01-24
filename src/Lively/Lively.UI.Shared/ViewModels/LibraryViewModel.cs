using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using Lively.Common;
using Lively.Common.Extensions;
using Lively.Common.Factories;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Gallery.Client;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.UI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class LibraryViewModel : ObservableObject
    {
        public bool IsWorking => downloading.Count != 0;
        public EventHandler<string> WallpaperDeleted;
        public event EventHandler<string> WallpaperDownloadFailed;
        public event EventHandler<string> WallpaperDownloadCompleted;
        public event EventHandler<(string, float, float, float)> WallpaperDownloadProgress;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<(string, CancellationTokenSource)> downloading = new();
        private readonly SemaphoreSlim semaphoreSlimInstallLock = new SemaphoreSlim(1, 1);
        private readonly List<string> wallpaperScanFolders;

        private readonly IDispatcherService dispatcher;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IUserSettingsClient userSettings;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly IDisplayManagerClient displayManager;
        private readonly IDialogService dialogService;
        private readonly GalleryClient galleryClient;

        private readonly IResourceService i18n;
        private TaskCompletionSource<LibraryModel> selectionTaskCompletionSource;

        public LibraryViewModel(IWallpaperLibraryFactory wallpaperLibraryFactory, 
            IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            IUserSettingsClient userSettings,
            IDialogService dialogService,
            IDispatcherService dispatcher,
            IResourceService i18n,
            GalleryClient galleryClient)
        {
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.dialogService = dialogService;
            this.dispatcher = dispatcher;
            this.galleryClient = galleryClient;

            this.i18n = i18n;

            wallpaperScanFolders = new List<string>
            {
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
            };

            LibraryItemsFiltered = new AdvancedCollectionView(LibraryItems, false);
            LibraryItemsFiltered.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));
            using (LibraryItemsFiltered.DeferRefresh())
            {
                foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
                {
                    LibraryItems.Add(item);
                }
            }

            LibrarySelectionMode = userSettings.Settings.RememberSelectedScreen ? "Single" : "None";
            //Select already running item when UI program is started again..
            UpdateSelectedWallpaper();

            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            desktopCore.WallpaperUpdated += DesktopCore_WallpaperUpdated;
        }

        [ObservableProperty]
        private AdvancedCollectionView libraryItemsFiltered;

        [ObservableProperty]
        private ObservableCollection<LibraryModel> libraryItems = new();

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
                    if (wallpapers.Any())
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
                SetProperty(ref _selectedItem, value);
            }
        }

        [ObservableProperty]
        private string librarySelectionMode = "Single";

        [RelayCommand]
        private async Task LibraryClick(LibraryModel model)
        {
            if (IsSelectionOnlyMode)
            {
                selectionTaskCompletionSource?.TrySetResult(model);
                return;
            }

            if (model is null || userSettings.Settings.RememberSelectedScreen || model.DataType != LibraryItemType.ready)
                return;

            var monitor = displayManager.DisplayMonitors.Count == 1 || userSettings.Settings.WallpaperArrangement != WallpaperArrangement.per ?
                displayManager.DisplayMonitors.FirstOrDefault(x => x.IsPrimary) : await dialogService.ShowDisplayChooseDialogAsync();
            if (monitor is not null)
                await desktopCore.SetWallpaper(model, monitor);
        }

        private bool _isSelectionOnlyMode;
        public bool IsSelectionOnlyMode
        {
            get => _isSelectionOnlyMode;
            set
            {
                if (!value && userSettings.Settings.RememberSelectedScreen)
                {
                    LibrarySelectionMode = "Single";
                    //Updating library selected item.
                    UpdateSelectedWallpaper();
                }
                else
                {
                    LibrarySelectionMode = "None";
                }
                SetProperty(ref _isSelectionOnlyMode, value);
            }
        }

        public async Task<LibraryModel> SelectItem()
        {
            if (LibraryItems.Count == 0)
                return null;

            selectionTaskCompletionSource = new TaskCompletionSource<LibraryModel>();

            try
            {
                IsSelectionOnlyMode = true;
                return await selectionTaskCompletionSource.Task;
            }
            finally
            {
                IsSelectionOnlyMode = false;
                selectionTaskCompletionSource = null;
            }
        }

        [RelayCommand]
        private void SelectItemCancel()
        {
            selectionTaskCompletionSource?.TrySetResult(null);
        }

        [RelayCommand]
        private void CancelDownload(LibraryModel model)
        {
            CancelDownload(model.LivelyInfo.Id);
        }

        [ObservableProperty]
        private bool isBusy;

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            dispatcher.TryEnqueue(UpdateSelectedWallpaper);
        }

        private void DesktopCore_WallpaperUpdated(object sender, WallpaperUpdatedData e)
        {
            dispatcher.TryEnqueue(() =>
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
                SelectedItem = LibraryItems.FirstOrDefault(x => x.DataType == LibraryItemType.ready && desktopCore.Wallpapers[0].LivelyInfoFolderPath.Equals(x.LivelyInfoFolderPath, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => userSettings.Settings.SelectedDisplay.Equals(x.Display));
                SelectedItem = LibraryItems.FirstOrDefault(x => x.DataType == LibraryItemType.ready && x.LivelyInfoFolderPath.Equals(wallpaper?.LivelyInfoFolderPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        #region public methods

        /// <summary>
        /// Stop if running and delete wallpaper from library and disk.<br>
        /// (To be called from UI thread.)</br>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task WallpaperDelete(LibraryModel obj, bool unsubscribe = true)
        {
            try
            {
                if (obj.DataType == LibraryItemType.gallery)
                {
                    CancelDownload(obj.LivelyInfo.Id);
                    return;
                }

                //close if running.
                await desktopCore.CloseWallpaper(obj, true);
                //delete wp folder.      
                var success = await FileUtil.TryDeleteDirectoryAsync(obj.LivelyInfoFolderPath, 1000, 4000);

                if (success)
                {
                    if (SelectedItem == obj)
                    {
                        SelectedItem = null;
                    }
                    //remove from library.
                    LibraryItems.Remove(obj);
                    WallpaperDeleted?.Invoke(this, obj.LivelyInfo.Id);
                    try
                    {
                        if (!string.IsNullOrEmpty(obj.LivelyPropertyPath))
                        {
                            //Delete LivelyProperties.json backup folder.
                            string[] wpdataDir = Directory.GetDirectories(Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir));
                            var wpFolderName = new DirectoryInfo(obj.LivelyInfoFolderPath).Name;
                            for (int i = 0; i < wpdataDir.Length; i++)
                            {
                                var item = new DirectoryInfo(wpdataDir[i]).Name;
                                if (wpFolderName.Equals(item, StringComparison.Ordinal))
                                {
                                    _ = FileUtil.TryDeleteDirectoryAsync(wpdataDir[i], 100, 1000);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }
                }
            }
            finally
            {
                if (unsubscribe && !string.IsNullOrEmpty(obj.LivelyInfo.Id) && galleryClient.IsLoggedIn)
                {
                    try
                    {
                        await galleryClient.UnsubscribeFromWallpaperAsync(obj.LivelyInfo.Id);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to unsubscribe wallpaper: {e}");
                    }
                }
            }
        }

        public async Task WallpaperExport(LibraryModel libraryItem, string saveFile)
        {
            await Task.Run(() =>
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

                        JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(tmpDir, "LivelyInfo.json"), info);
                        ZipCreate.CreateZip(saveFile, new List<string>() { tmpDir });
                    }
                    finally
                    {
                        _ = FileUtil.TryDeleteDirectoryAsync(tmpDir, 1000, 2000);
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

                        JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(tmpDir, "LivelyInfo.json"), info);
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
                        _ = FileUtil.TryDeleteDirectoryAsync(tmpDir, 1000, 2000);
                    }
                }
                else
                {
                    //installed lively wallpaper.
                    ZipCreate.CreateZip(saveFile, new List<string>() { Path.GetDirectoryName(libraryItem.FilePath) });
                }
            });
        }

        public async Task WallpaperShowOnDisk(LibraryModel libraryItem)
        {
            string folderPath =
                libraryItem.LivelyInfo.Type == WallpaperType.url || libraryItem.LivelyInfo.Type == WallpaperType.videostream
                ? libraryItem.LivelyInfoFolderPath : libraryItem.FilePath;
            await DesktopBridgeUtil.OpenFolder(folderPath);
        }

        public void CancelDownload(string id)
        {
            var item = downloading.Find(x => x.Item1 == id);
            if (item.Item1 != default)
            {
                item.Item2?.Cancel();
                downloading.Remove(item);
            }
        }

        ///<see cref="AddWallpaperGallery(ILibraryModel)"/>
        public void CancelAllDownloads()
        {
            foreach (var item in downloading)
            {
                item.Item2?.Cancel();
            }
        }

        public async Task AddWallpaperGallery(GalleryModel obj)
        {
            var libItem = wallpaperLibraryFactory.CreateFromMetadata(obj.LivelyInfo);
            libItem.ImagePath = obj.Image;
            var downloadFile = Path.Combine(Constants.CommonPaths.TempDir, Path.ChangeExtension(libItem.LivelyInfo.Id, ".zip"));
            var cts = new CancellationTokenSource();
            var downloadItem = (libItem.LivelyInfo.Id, cts);
            downloading.Add(downloadItem);

            try
            {
                await galleryClient.SubscribeToWallpaperAsync(libItem.LivelyInfo.Id);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to subscribe wallpaper: {e}");
            }

            try
            {
                libItem.IsDownloading = true;
                Logger.Info($"Download Wallpaper -> Title: {libItem.Title} Id: {libItem.LivelyInfo.Id}");
                LibraryItems.Insert(0, libItem);
                await galleryClient.DownloadWallpaperAsync(libItem.LivelyInfo.Id, downloadFile, cts.Token, (progress, downloaded, totalSize) =>
                {
                    downloaded /= 1024 * 1024;
                    totalSize /= 1024 * 1024;

                    libItem.DownloadingProgress = progress;
                    libItem.DownloadingProgressText = $"{downloaded:0.##}/{totalSize:0.##} MB";
                    WallpaperDownloadProgress?.Invoke(this, (libItem.LivelyInfo.Id, progress, downloaded, totalSize));
                });
                var isSelected = SelectedItem == libItem;
                LibraryItems.Remove(libItem);
                var installed = await AddWallpaperFileGallery(downloadFile, libItem.LivelyInfo.Id);
                if (isSelected)
                {
                    //Apply wallpaper
                    SelectedItem = (LibraryModel)installed;
                }

                libItem.IsSubscribed = true;
                WallpaperDownloadCompleted?.Invoke(this, libItem.LivelyInfo.Id);
            }
            catch (OperationCanceledException)
            {
                Logger.Info($"User cancelled gallery wallpaper download: {libItem.Title}");
                WallpaperDownloadFailed?.Invoke(this, libItem.LivelyInfo.Id);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to add gallery wallpaper: {e}");
                WallpaperDownloadFailed?.Invoke(this, libItem.LivelyInfo.Id);
            }
            finally
            {
                libItem.IsDownloading = false;
                libItem.DownloadingProgress = 0;
                libItem.DownloadingProgressText = "-/- MB";
                downloading.Remove(downloadItem);

                //Remove visual only item from user library
                if (LibraryItems.Contains(libItem))
                    LibraryItems.Remove(libItem);

                //Delete temp file(s)
                if (File.Exists(downloadFile))
                {
                    try
                    {
                        File.Delete(downloadFile);
                    }
                    catch { /* Core will empty temp directory next time app is started */ }
                }
            }
        }

        private async Task<LibraryModel> AddWallpaperFileGallery(string filePath, string id)
        {
            if (ZipExtract.IsLivelyZip(filePath))
            {
                await semaphoreSlimInstallLock.WaitAsync();
                string installDir = null;
                try
                {
                    installDir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir, Path.GetRandomFileName());
                    await Task.Run(() => ZipExtract.ZipExtractFile(filePath, installDir, false));
                    var info = JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(installDir, "LivelyInfo.json"));
                    info.Id = id;
                    JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(installDir, "LivelyInfo.json"), info);
                    return AddWallpaper(installDir);
                }
                catch (Exception)
                {
                    try
                    {
                        Directory.Delete(installDir, true);
                    }
                    catch { }
                    throw;
                }
                finally
                {
                    semaphoreSlimInstallLock.Release();
                }
            }
            else
            {
                throw new InvalidOperationException("Not Lively .zip");
            }
        }

        public async Task<LibraryModel> AddWallpaperFile(string filePath)
        {
            WallpaperType type;
            if ((type = FileTypes.GetFileType(filePath)) != (WallpaperType)(-1))
            {
                if (type == (WallpaperType)100)
                {
                    //lively .zip is not a wallpaper type.
                    if (ZipExtract.IsLivelyZip(filePath))
                    {
                        await semaphoreSlimInstallLock.WaitAsync();
                        string installDir = null;
                        try
                        {
                            installDir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir, Path.GetRandomFileName());
                            await Task.Run(() => ZipExtract.ZipExtractFile(filePath, installDir, false));
                            return AddWallpaper(installDir);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                Directory.Delete(installDir, true);
                            }
                            catch { }
                        }
                        finally
                        {
                            semaphoreSlimInstallLock.Release();
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(i18n.GetString("LivelyExceptionNotLivelyZip"));
                    }
                }
                else
                {
                    var arguments = string.Empty;
                    if (type.IsApplicationWallpaper())
                        arguments = await dialogService.ShowTextInputDialogAsync(i18n.GetString("TextWallpaperCommandlineArgs"), "Examples: --myarguments1 -myargument2");

                    var dir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
                    Directory.CreateDirectory(dir);
                    var data = new LivelyInfoModel()
                    {
                        Title = Path.GetFileNameWithoutExtension(filePath),
                        Type = type,
                        IsAbsolutePath = true,
                        FileName = filePath,
                        Contact = string.Empty,
                        Preview = string.Empty,
                        Thumbnail = string.Empty,
                        Arguments =  string.IsNullOrWhiteSpace(arguments) ? string.Empty : arguments,
                    };

                    JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(dir, "LivelyInfo.json"), data);
                    return AddWallpaper(dir, true);
                }
            }
            throw new InvalidOperationException($"{i18n.GetString("TextUnsupportedFile")} ({Path.GetExtension(filePath)})");
        }

        public LibraryModel AddWallpaperLink(string url)
        {
            var dir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            var data = new LivelyInfoModel()
            {
                Title = LinkUtil.GetLastSegmentUrl(url),
                Type = (userSettings.Settings.AutoDetectOnlineStreams && StreamUtil.IsSupportedStream(url)) ? WallpaperType.videostream : WallpaperType.url,
                IsAbsolutePath = true,
                FileName = url,
                Contact = url,
                Preview = string.Empty,
                Thumbnail = string.Empty,
                Arguments = string.Empty,
            };

            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(dir, "LivelyInfo.json"), data);
            return AddWallpaper(dir, true);
        }

        public LibraryModel AddWallpaperLink(Uri uri) => AddWallpaperLink(uri.OriginalString);

        public async Task AddWallpapers(List<string> files, CancellationToken cancellationToken, IProgress<int> progress)
        {
            //display all Lively zip files first since its the first items to get processed.
            files = files.OrderByDescending(x => Path.GetExtension(x).Equals(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            var tcs = new TaskCompletionSource<bool>();
            desktopCore.WallpaperChanged += WallpaperChanged;
            void WallpaperChanged(object sender, EventArgs e)
            {
                tcs.SetResult(true);
            }

            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    var wallpaper = await AddWallpaperFile(files[i]);
                    //Skipping .zip files already processed..
                    if (wallpaper.DataType == LibraryItemType.processing)
                    {
                        wallpaper.DataType = LibraryItemType.multiImport;
                        await desktopCore.SetWallpaper(wallpaper, userSettings.Settings.SelectedDisplay);
                        await tcs.Task;
                        tcs = new TaskCompletionSource<bool>();
                    }

                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                progress.Report(100 * (i + 1) / files.Count);
            }

            desktopCore.WallpaperChanged -= WallpaperChanged;
        }

        public LibraryModel AddWallpaperFolder(string folder)
        {
            return AddWallpaper(folder, false);
        }

        /// <summary>
        /// Rescans wallpaper directory and update library.
        /// </summary>
        public void UpdateWallpaperDirectory(string newDir)
        {
            LibraryItems.Clear();
            wallpaperScanFolders.Clear();
            wallpaperScanFolders.Add(Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallDir));
            wallpaperScanFolders.Add(Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallTempDir));
            using (LibraryItemsFiltered.DeferRefresh())
            {
                foreach (var item in ScanWallpaperFolders(wallpaperScanFolders))
                {
                    LibraryItems.Add(item);
                }
            }
        }

        public void UpdateAnimationSettings(LivelyGUIState state)
        {
            foreach (var item in LibraryItems)
                item.ImagePath = state == LivelyGUIState.lite ? item.ThumbnailPath : item.PreviewClipPath ?? item.ThumbnailPath;
        }

        #endregion //public methods

        #region helpers

        private LibraryModel AddWallpaper(string folderPath, bool processing = false)
        {
            try
            {
                var item = wallpaperLibraryFactory.CreateFromDirectory(folderPath);
                item.ImagePath = userSettings.Settings.UIMode != LivelyGUIState.lite ? item.ImagePath : item.ThumbnailPath;
                //var index = processing ? 0 : BinarySearch(LibraryItems, libItem.Title);
                item.DataType = processing ? LibraryItemType.processing : LibraryItemType.ready;
                //LibraryItems.Insert(index, libItem);
                LibraryItems.Add(item);
                return item;
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
                    LibraryModel item = null;
                    try
                    {
                        item = wallpaperLibraryFactory.CreateFromDirectory(currDir);
                        item.ImagePath = userSettings.Settings.UIMode != LivelyGUIState.lite ? item.ImagePath : item.ThumbnailPath;
                        Logger.Info($"Loaded wallpaper: {item.FilePath}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    if (item != null)
                    {
                        yield return item;
                    }
                }
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