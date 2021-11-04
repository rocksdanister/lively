using livelywpf.Core.API;
using livelywpf.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace livelywpf.Core
{
    public class WinDesktopCore
    {
        private readonly List<IWallpaper> wallpapers = new List<IWallpaper>(2);
        /// <summary>
        /// Running wallpapers.
        /// </summary>
        public ReadOnlyCollection<IWallpaper> Wallpapers
        {
            get => wallpapers.AsReadOnly(); 
        }

        /// <summary>
        /// Wallpaper set/removed.
        /// </summary>
        public static event EventHandler WallpaperChanged;

        /// <summary>
        /// Sets the given wallpaper based on layout usersettings.
        /// </summary>
        public void SetWallpaper(ILibraryModel wallpaper, ILivelyScreen display)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reset workerw.
        /// </summary>
        public void ResetWallpaper()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Restore wallpaper from save.
        /// </summary>
        public void RestoreWallpaper()
        {
            throw new NotImplementedException();
        }

        public void CloseAllWallpapers(bool terminate = false)
        {
            throw new NotImplementedException();
        }

        public void CloseWallpaper(ILivelyScreen display, bool terminate = false)
        {
            throw new NotImplementedException();
        }

        public void CloseWallpaper(WallpaperType type, bool terminate = false)
        {
            throw new NotImplementedException();
        }

        public void CloseWallpaper(ILibraryModel wp, bool terminate = false)
        {
            throw new NotImplementedException();
        }

        public static void SendMessageWallpaper(ILibraryModel wp, IpcMessage msg)
        {
            throw new NotImplementedException();
        }

        public static void SendMessageWallpaper(ILivelyScreen display, LibraryModel wp, IpcMessage msg)
        {
            throw new NotImplementedException();
        }

        public static void SeekWallpaper(ILibraryModel wp, float seek, PlaybackPosType type)
        {
            throw new NotImplementedException();
        }

        public static void SeekWallpaper(ILivelyScreen display, float seek, PlaybackPosType type)
        {
            throw new NotImplementedException();
        }

        /*
        public void RefreshDesktop()
        {
            //use static helper instead!
        }
        */
    }
}
