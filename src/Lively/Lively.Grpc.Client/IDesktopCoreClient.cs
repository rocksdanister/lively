using Lively.Common;
using Lively.Common.API;
using Lively.Grpc.Common.Proto.Desktop;
using Lively.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface IDesktopCoreClient : IDisposable
    {
        ReadOnlyCollection<WallpaperData> Wallpapers { get; }

        event EventHandler WallpaperChanged;

        Task CloseAllWallpapers(bool terminate = false);
        Task CloseWallpaper(IDisplayMonitor monitor, bool terminate = false);
        Task CloseWallpaper(ILibraryModel item, bool terminate = false);
        Task CloseWallpaper(WallpaperType type, bool terminate = false);
        Task SetWallpaper(ILibraryModel item, IDisplayMonitor display);
        Task SetWallpaper(string livelyInfoPath, string monitorId);
        void SendMessageWallpaper(ILibraryModel obj, IpcMessage msg);
        void SendMessageWallpaper(IDisplayMonitor display, ILibraryModel obj, IpcMessage msg);
        Task ShutDown();
    }

    public class WallpaperData
    {
        public string LivelyInfoFolderPath { get; set; }
        public string LivelyPropertyCopyPath { get; set; }
        public string ThumbnailPath { get; set; }
        public string PreviewPath { get; set; }
        public IDisplayMonitor Display { get; set; }
    }
}