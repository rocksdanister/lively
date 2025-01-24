using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Lively.Core
{
    public interface IWallpaper
    {
        /// <summary>
        /// Wallpaper exit event fired
        /// </summary>
        bool IsExited { get; }
        /// <summary>
        /// Wallpaper loading complete status (includes LivelyProperties restoration.)
        /// </summary>
        /// <returns></returns>
        bool IsLoaded { get; }
        /// <summary>
        /// Get lively wallpaper type.
        /// </summary>
        /// <returns></returns>
        WallpaperType Category { get; }
        /// <summary>
        /// Get wallpaper metadata.
        /// </summary>
        /// <returns></returns>
        LibraryModel Model { get; }
        /// <summary>
        /// Get window handle.
        /// </summary>
        /// <returns></returns>
        IntPtr Handle { get; }
        /// <summary>
        /// Get handle to input window.
        /// </summary>
        /// <returns></returns>
        IntPtr InputHandle { get; }
        /// <summary>
        /// Get process information.
        /// </summary>
        /// <returns>null if not a program wallpaper.</returns>
        Process Proc { get; }
        /// <summary>
        /// Start wallpaper.
        /// </summary>
        Task ShowAsync();
        /// <summary>
        /// Pause wallpaper playback.
        /// </summary>
        void Pause();
        /// <summary>
        /// Start/resume wallpaper playback.
        /// </summary>
        void Play();
        /// <summary>
        /// Stop wallpaper playback.
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
        DisplayMonitor Screen { get; set; }
        /// <summary>
        /// Send ipc message to program wallpaper.
        /// </summary>
        /// <param name="msg"></param>
        void SendMessage(IpcMessage obj);
        /// <summary>
        /// Get location of LivelyProperties.json copy file in Savedata/wpdata.
        /// This will be a copy of the original file (different screens will have different copy.)
        /// </summary>
        /// <returns>null if no file.</returns>
        string LivelyPropertyCopyPath { get; }
        /// <summary>
        /// Set wallpaper volume.
        /// </summary>
        /// <param name="volume">Range 0 - 100</param>
        void SetVolume(int volume);
        /// <summary>
        /// Mute/disable audio track.
        /// </summary>
        /// <param name="mute">true: mute audio</param>
        void SetMute(bool mute);
        /// <summary>
        /// Sets wallpaper position in timeline. <br>
        /// Only value 0 works for non-video wallpapers.</br>
        /// </summary>
        /// <param name="pos">Range 0 - 100</param>
        void SetPlaybackPos(float pos, PlaybackPosType type);
        /// <summary>
        /// Capture wallpaper view and save as image (.jpg)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        Task ScreenCapture(string filePath);
    }

    public enum PlaybackPosType
    {
        absolutePercent, 
        relativePercent
    }
}
