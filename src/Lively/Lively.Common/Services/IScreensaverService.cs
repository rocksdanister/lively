using System;
using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public interface IScreensaverService
    {
        ScreensaverApplyMode Mode { get; }
        bool IsRunning { get; }

        void CreatePreview(IntPtr hwnd);
        Task StartAsync(bool isFadeIn);
        Task StopAsync();

        event EventHandler Stopped;
    }

    public enum ScreensaverApplyMode
    {
        /// <summary>
        /// Create a new running wallpaper process.<bt>
        /// This is the only mode with support for running screensaver different from wallpaper.</bt>
        /// </summary>
        process,
        /// <summary>
        /// Use DwmThumnail API.<br>
        /// Does not work in Windows Screensaver mode.</br>
        /// </summary>
        dwmThumbnail,
    }
}