using Lively.Common;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.UI.Wpf.Helpers
{
    public class LibraryUtil : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly SemaphoreSlim semaphoreSlimInstallLock = new SemaphoreSlim(1, 1);
        private bool disposedValue;

        private readonly LibraryViewModel libraryVm;
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;

        public LibraryUtil(LibraryViewModel libraryVm, IUserSettingsClient userSettings, IDesktopCoreClient desktopCore)
        {
            this.libraryVm = libraryVm;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
        }

        /// <summary>
        /// Stop if running and delete wallpaper from library and disk.<br>
        /// (To be called from UI thread.)</br>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task WallpaperDelete(ILibraryModel obj)
        {
            //close if running.
            await desktopCore.CloseWallpaper(obj, true);
            //delete wp folder.      
            var success = await FileOperations.DeleteDirectoryAsync(obj.LivelyInfoFolderPath, 1000, 4000);

            if (success)
            {
                if (libraryVm.SelectedItem == obj)
                {
                    libraryVm.SelectedItem = null;
                }
                //remove from library.
                libraryVm.LibraryItems.Remove((LibraryModel)obj);
                try
                {
                    if (string.IsNullOrEmpty(obj.LivelyInfoFolderPath))
                        return;

                    //Delete LivelyProperties.json backup folder.
                    string[] wpdataDir = Directory.GetDirectories(Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir));
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
                        _ = FileOperations.DeleteDirectoryAsync(tmpDir, 1000, 2000);
                    }
                }
                else
                {
                    //installed lively wallpaper.
                    ZipCreate.CreateZip(saveFile, new List<string>() { Path.GetDirectoryName(libraryItem.FilePath) });
                }
                FileOperations.OpenFolder(saveFile);
            });
        }

        public void WallpaperShowOnDisk(ILibraryModel libraryItem)
        {
            string folderPath =
                libraryItem.LivelyInfo.Type == WallpaperType.url || libraryItem.LivelyInfo.Type == WallpaperType.videostream
                ? libraryItem.LivelyInfoFolderPath : libraryItem.FilePath;
            FileOperations.OpenFolder(folderPath);
        }

        public async Task<ILibraryModel> AddWallpaperFile(string filePath)
        {
            WallpaperType type;
            if ((type = FileFilter.GetLivelyFileType(filePath)) != (WallpaperType)(-1))
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
                            return libraryVm.AddWallpaper(installDir);
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
                        throw new InvalidOperationException("Not Lively .zip");
                    }
                }
                else
                {
                    var dir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
                    Directory.CreateDirectory(dir);
                    var data = new LivelyInfoModel()
                    {
                        Title = Path.GetFileNameWithoutExtension(filePath),
                        Type = type,
                        IsAbsolutePath = true,
                        Contact = null,
                        FileName = filePath,
                        Preview = null,
                        Thumbnail = null,
                        Arguments = string.Empty,
                    };

                    //TODO generate livelyproperty for gif etc..
                    JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(dir, "LivelyInfo.json"), data);
                    return libraryVm.AddWallpaper(dir);
                }
            }
            throw new InvalidOperationException($"Unsupported file ({Path.GetExtension(filePath)})");
        }

        public ILibraryModel AddWallpaperLink(string url)
        {
            var dir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            var data = new LivelyInfoModel()
            {
                Title = LinkHandler.GetLastSegmentUrl(url),
                Type = WallpaperType.url,
                IsAbsolutePath = true,
                FileName = url,
                Contact = url,
                Preview = null,
                Thumbnail = null,
                Arguments = string.Empty,
            };

            //TODO generate livelyproperty for gif etc..
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(dir, "LivelyInfo.json"), data);
            return libraryVm.AddWallpaper(dir);
        }

        public ILibraryModel AddWallpaperLink(Uri uri) => AddWallpaperLink(uri.OriginalString);

        public void SortWallpaper(ILibraryModel obj) => libraryVm.SortWallpaper((LibraryModel)obj);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    semaphoreSlimInstallLock?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LibraryUtil()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
