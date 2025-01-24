using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface IDesktopCoreClient : IDisposable
    {
        ReadOnlyCollection<WallpaperData> Wallpapers { get; }
        string BaseDirectory { get; }
        Version AssemblyVersion { get; }
        bool IsCoreInitialized { get; }

        Task CloseAllWallpapers(bool terminate = false);
        Task CloseWallpaper(DisplayMonitor monitor, bool terminate = false);
        Task CloseWallpaper(LibraryModel item, bool terminate = false);
        Task CloseWallpaper(WallpaperType type, bool terminate = false);
        Task SetWallpaper(LibraryModel item, DisplayMonitor display);
        Task SetWallpaper(string livelyInfoPath, string monitorId);
        void SendMessageWallpaper(LibraryModel obj, IpcMessage msg);
        void SendMessageWallpaper(DisplayMonitor display, LibraryModel obj, IpcMessage msg);
        Task PreviewWallpaper(string livelyInfoPath);
        Task TakeScreenshot(string monitorId, string savePath);

        event EventHandler WallpaperChanged;
        event EventHandler<Exception> WallpaperError;
        event EventHandler<WallpaperUpdatedData> WallpaperUpdated;
    }

    public class WallpaperData
    {
        public string LivelyInfoFolderPath { get; set; }
        public string LivelyPropertyCopyPath { get; set; }
        public string ThumbnailPath { get; set; }
        public string PreviewPath { get; set; }
        public DisplayMonitor Display { get; set; }
        public WallpaperType Category { get; set; }
    }

    public class WallpaperUpdatedData
    {
        public LivelyInfoModel Info { get; set; }
        public UpdateWallpaperType Category { get; set; }
        public string InfoPath { get; set; }
    }
}