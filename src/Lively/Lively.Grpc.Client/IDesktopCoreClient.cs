using Desktop;
using Lively.Common;
using Lively.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface IDesktopCoreClient : IDisposable
    {
        ReadOnlyCollection<GetScreensResponse> DisplayMonitors { get; }
        ReadOnlyCollection<GetWallpapersResponse> Wallpapers { get; }

        event EventHandler DisplayChanged;
        event EventHandler WallpaperChanged;

        Task CloseAllWallpapers(bool terminate = false);
        Task CloseWallpaper(IDisplayMonitor monitor, bool terminate = false);
        Task CloseWallpaper(ILibraryModel item, bool terminate = false);
        Task CloseWallpaper(WallpaperType type, bool terminate = false);
        Task SetWallpaper(ILibraryModel item, IDisplayMonitor display);
        Task SetWallpaper(string livelyInfoPath, string monitorId);
        Task ShutDown();
    }
}