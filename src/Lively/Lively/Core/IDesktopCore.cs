using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Lively.Core
{
    public interface IDesktopCore : IDisposable
    {
        IntPtr DesktopWorkerW { get; }
        /// <summary>
        /// Running wallpapers.
        /// </summary>
        ReadOnlyCollection<IWallpaper> Wallpapers { get; }
        void CloseAllWallpapers();
        void CloseWallpaper(LibraryModel wp);
        void CloseWallpaper(DisplayMonitor display);
        void CloseWallpaper(WallpaperType type);
        Task ResetWallpaperAsync();
        Task RestartWallpaper();
        Task RestartWallpaper(DisplayMonitor display);
        void RestoreWallpaper();
        void SendMessageWallpaper(string info_path, IpcMessage msg);
        void SendMessageWallpaper(DisplayMonitor display, string info_path, IpcMessage msg);
        Task SetWallpaperAsync(LibraryModel wallpaper, DisplayMonitor display);
        /// <summary>
        /// Apply the WallpaperVirtualDesktopId confinement setting to running wallpapers.
        /// </summary>
        void UpdateVirtualDesktopVisibility();

        /// <summary>
        /// Wallpaper set/removed.
        /// </summary>
        public event EventHandler WallpaperChanged;
        /// <summary>
        /// Error occured in wallpaper core.
        /// </summary>
        public event EventHandler<Exception> WallpaperError;
        /// <summary>
        /// Wallpaper core services restarted.
        /// </summary>
        public event EventHandler WallpaperReset;
    }
}