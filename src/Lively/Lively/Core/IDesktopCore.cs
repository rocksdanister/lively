using Lively.Common;
using Lively.Common.API;
using Lively.Models;
using System;
using System.Collections.ObjectModel;

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
        void CloseWallpaper(ILibraryModel wp, bool terminate = false);
        void CloseWallpaper(IDisplayMonitor display, bool terminate = false);
        void CloseWallpaper(WallpaperType type, bool terminate = false);
        void ResetWallpaper();
        void RestoreWallpaper();
        void SeekWallpaper(ILibraryModel wp, float seek, PlaybackPosType type);
        void SeekWallpaper(IDisplayMonitor display, float seek, PlaybackPosType type);
        void SendMessageWallpaper(string info_path, IpcMessage msg);
        void SendMessageWallpaper(IDisplayMonitor display, string info_path, IpcMessage msg);
        void SetWallpaper(ILibraryModel wallpaper, IDisplayMonitor display);

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
        public ILivelyInfoModel Info { get; set; }
        public string InfoPath { get; set; }
    }
}