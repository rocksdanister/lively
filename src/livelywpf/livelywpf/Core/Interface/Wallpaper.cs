using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using livelywpf.Model;

namespace livelywpf.Core
{
    public interface IWallpaper
    {
        /// <summary>
        /// Get lively wallpaper type.
        /// </summary>
        /// <returns></returns>
        WallpaperType GetWallpaperType();
        /// <summary>
        /// Get wallpaper metadata.
        /// </summary>
        /// <returns></returns>
        LibraryModel GetWallpaperData();
        /// <summary>
        /// Get window handle.
        /// </summary>
        /// <returns></returns>
        IntPtr GetHWND();
        /// <summary>
        /// Set window handle.
        /// This is only metadata, have no effect on actual handle.
        /// </summary>
        /// <param name="hwnd">HWND</param>
        void SetHWND(IntPtr hwnd);
        /// <summary>
        /// Get process information.
        /// </summary>
        /// <returns>null if not a program wallpaper.</returns>
        Process GetProcess();
        /// <summary>
        /// Start wallpaper.
        /// </summary>
        void Show();
        /// <summary>
        /// Pause wallpaper playback.
        /// </summary>
        void Pause();
        /// <summary>
        /// Resume wallpaper playback.
        /// </summary>
        void Resume();
        /// <summary>
        /// Start/resume wallpaper playback.
        /// </summary>
        void Play();
        /// <summary>
        /// Stop wallpaper plabyack.
        /// </summary>
        void Stop();
        /// <summary>
        /// Close wallpaper gracefully.
        /// </summary>
        void Close();
        /// <summary>
        /// Immediately kill wallpaper.
        /// Only have effect if Program wallpaper, otherwise same as Close().
        /// </summary>
        void Terminate();
        /// <summary>
        /// Get display device in which wallpaper is currently running.
        /// </summary>
        /// <returns></returns>
        LivelyScreen GetScreen();
        /// <summary>
        /// Set display device in which wallpaper is currently running.
        /// This is only metadata, have no effect on actual wallpaper position.
        /// </summary>
        /// <param name="display"></param>
        void SetScreen(LivelyScreen display);
        /// <summary>
        /// Send ipc message to program wallpaper.
        /// </summary>
        /// <param name="msg"></param>
        void SendMessage(string msg);
        /// <summary>
        /// Get location of LivelyProperties.json copy file in Savedata/wpdata.
        /// This will be a copy of the original file (different screens will have different copy.)
        /// </summary>
        /// <returns>null if no file.</returns>
        string GetLivelyPropertyCopyPath();
        /// <summary>
        /// Set wallpaper volume.
        /// </summary>
        /// <param name="volume">Range 0 - 100</param>
        void SetVolume(int volume);
        /// <summary>
        /// Fires after Show() method is called.
        /// Check success status to check if wallpaper ready/failed.
        /// </summary>
        event EventHandler<WindowInitializedArgs> WindowInitialized;
    }

    public class WindowInitializedArgs : EventArgs
    {
        /// <summary>
        /// True if wallpaper window is ready.
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Error if any.
        /// Null if no error.
        /// </summary>
        public Exception Error { get; set; }
        /// <summary>
        /// Custom message.
        /// Null if no message.
        /// </summary>
        public string Msg { get; set; }
    }
}
