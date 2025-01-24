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
        void CloseAllWallpapers(bool terminate = false);
        void CloseWallpaper(LibraryModel wp, bool terminate = false);
        void CloseWallpaper(DisplayMonitor display, bool terminate = false);
        void CloseWallpaper(WallpaperType type, bool terminate = false);
        Task ResetWallpaperAsync();
        Task RestartWallpaper();
        Task RestartWallpaper(DisplayMonitor display);
        void RestoreWallpaper();
        void SeekWallpaper(LibraryModel wp, float seek, PlaybackPosType type);
        void SeekWallpaper(DisplayMonitor display, float seek, PlaybackPosType type);
        void SendMessageWallpaper(string info_path, IpcMessage msg);
        void SendMessageWallpaper(DisplayMonitor display, string info_path, IpcMessage msg);
        Task SetWallpaperAsync(LibraryModel wallpaper, DisplayMonitor display);

        /// <summary>
        /// Wallpaper set/removed.
        /// </summary>
        public event EventHandler WallpaperChanged;
        /// <summary>
        /// Update/remove preview clips, metadata or wallpaper.
        /// </summary>
        public event EventHandler<WallpaperUpdateArgs> WallpaperUpdated;
        /// <summary>
        /// Error occured in wallpaper core.
        /// </summary>
        public event EventHandler<Exception> WallpaperError;
        /// <summary>
        /// Wallpaper core services restarted.
        /// </summary>
        public event EventHandler WallpaperReset;
    }

    public class WallpaperUpdateArgs : EventArgs
    {
        public UpdateWallpaperType Category { get; set; }
        public LivelyInfoModel Info { get; set; }
        public string InfoPath { get; set; }
    }
}