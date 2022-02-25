using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Core;
using Lively.Helpers;
using Lively.Services;
using Lively.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Lively.Views
{
    public interface ILibraryPreview
    {
        /// <summary>
        /// Exit and detach wallpaper (will abort if Capture is running.)
        /// </summary>
        public void Exit();
        /// <summary>
        /// Capture thumbnail every 3 seconds.
        /// </summary>
        /// <param name="savePath"></param>
        public void StartThumbnailCaptureLoop(string savePath);
        /// <summary>
        /// Create thumbnail and preview gif 
        /// </summary>
        /// <param name="savePath"></param>
        public void StartCapture(string savePath);
        /// <summary>
        /// Wallpaper is attached to window and ready for capture.
        /// </summary>
        event EventHandler WallpaperAttached;
        /// <summary>
        /// New thumbnail file ready.
        /// </summary>
        event EventHandler<string> ThumbnailUpdated;
        /// <summary>
        /// New preview gif ready.
        /// </summary>
        event EventHandler<string> PreviewUpdated;
        /// <summary>
        /// Progress of operation, from 0 - 100.
        /// </summary>
        event EventHandler<double> CaptureProgress;
    }

    /// <summary>
    /// Interaction logic for LibraryPreview.xaml
    /// </summary>
    public partial class LibraryPreview : Window, ILibraryPreview
    {
        public event EventHandler<string> ThumbnailUpdated;
        public event EventHandler<string> PreviewUpdated;
        public event EventHandler<double> CaptureProgress;
        public event EventHandler WallpaperAttached;

        private bool _processing = false;
        private string thumbnailPathTemp;
        private string tmpThumbCaptureLoopPath;
        private readonly WallpaperType wallpaperType;
        private readonly IntPtr wallpaperHwnd;
        private readonly DispatcherTimer thumbnailCaptureTimer = new DispatcherTimer();
        //Good values: 1. 30c,120s 2. 15c, 90s
        private readonly int gifAnimationDelay = 1000 * 1 / 30; //in milliseconds (1/fps)
        private readonly int gifSaveAnimationDelay = 1000 * 1 / 120;
        private readonly int gifTotalFrames = 60;
        private readonly IUserSettingsService userSettings;

        public LibraryPreview(IWallpaper wallpaper)
        {
            userSettings = App.Services.GetRequiredService<IUserSettingsService>();

            var vm = new LibraryPreviewViewModel(this, wallpaper);
            this.DataContext = vm;
            this.Closed += vm.OnWindowClosed;
            wallpaperHwnd = wallpaper.Handle;
            wallpaperType = wallpaper.Category;

            InitializeComponent();
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) this.Close(); };
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_processing)
            {
                e.Cancel = true;
                //ModernWpf.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(PreviewBorder);
                return;
            }

            thumbnailCaptureTimer?.Stop();
            try
            {
                //deleting temporary thumbnail file if any..
                File.Delete(tmpThumbCaptureLoopPath);
            }
            catch { }

            //detach wallpaper window from this dialogue.
            WindowOperations.SetParentSafe(wallpaperHwnd, IntPtr.Zero);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //attach wp hwnd to border ui element.
            WpfUtil.SetProgramToFramework(this, wallpaperHwnd, PreviewBorder);
            //refocus window to allow keyboard input.
            this.Activate();
            WallpaperAttached?.Invoke(this, null);
        }

        private void CaptureLoop(object sender, EventArgs e)
        {
            var currThumbPath = Path.Combine(thumbnailPathTemp, Path.ChangeExtension(Path.GetRandomFileName(), ".jpg"));
            if (File.Exists(tmpThumbCaptureLoopPath))
            {
                if (wallpaperType == WallpaperType.picture)
                    return;

                try
                {
                    File.Delete(tmpThumbCaptureLoopPath);
                }
                catch
                {
                    thumbnailCaptureTimer.Stop();
                }
            }

            Rect previewPanelPos = WpfUtil.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WpfUtil.GetElementPixelSize(PreviewBorder);

            //thumbnail capture
            CaptureScreen.CopyScreen(
                currThumbPath,
               (int)previewPanelPos.Left,
               (int)previewPanelPos.Top,
               (int)previewPanelSize.Width,
               (int)previewPanelSize.Height);

            ThumbnailUpdated?.Invoke(this, currThumbPath);
            tmpThumbCaptureLoopPath = currThumbPath;
        }

        private async void CapturePreview(string saveDirectory)
        {
            _processing = true;
            thumbnailCaptureTimer?.Stop();
            taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            Rect previewPanelPos = WpfUtil.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WpfUtil.GetElementPixelSize(PreviewBorder);

            //wait before capturing thumbnail..incase wallpaper is not loaded yet.
            await Task.Delay(100);

            var thumbFilePath = Path.Combine(saveDirectory, Path.ChangeExtension(Path.GetRandomFileName(), ".jpg"));
            //final thumbnail capture..
            CaptureScreen.CopyScreen(
               thumbFilePath,
               (int)previewPanelPos.Left,
               (int)previewPanelPos.Top,
               (int)previewPanelSize.Width,
               (int)previewPanelSize.Height);
            ThumbnailUpdated?.Invoke(this, thumbFilePath);

            //preview clip (animated gif file).
            if (userSettings.Settings.GifCapture && wallpaperType != WallpaperType.picture)
            {
                var previewFilePath = Path.Combine(saveDirectory, Path.ChangeExtension(Path.GetRandomFileName(), ".gif"));
                previewPanelPos = WpfUtil.GetAbsolutePlacement(PreviewBorder, true);
                await CaptureScreen.CaptureGif(
                       previewFilePath,
                       (int)previewPanelPos.Left,
                       (int)previewPanelPos.Top,
                       (int)previewPanelPos.Width,
                       (int)previewPanelPos.Height,
                       gifAnimationDelay,
                       gifSaveAnimationDelay,
                       gifTotalFrames,
                       new Progress<int>(percent => CaptureProgress?.Invoke(this, percent - 1)));
                PreviewUpdated?.Invoke(this, previewFilePath);
            }
            _processing = false;
            CaptureProgress?.Invoke(this, 100);
        }

        #region interface methods

        public void Exit()
        {
            this.Close();
        }

        public void StartCapture(string savePath)
        {
            if (_processing)
                return;

            CapturePreview(savePath);
        }

        public void StartThumbnailCaptureLoop(string savePath)
        {
            thumbnailPathTemp = savePath;
            //capture thumbnail every few seconds while user is shown wallpaper metadata preview.
            thumbnailCaptureTimer.Tick += new EventHandler(CaptureLoop);
            thumbnailCaptureTimer.Interval = new TimeSpan(0, 0, 0, 0, 3000);
            thumbnailCaptureTimer.Start();
        }

        #endregion //interface methods

        #region window move/resize lock

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        //prevent window resize and move during recording.
        //ref: https://stackoverflow.com/questions/3419909/how-do-i-lock-a-wpf-window-so-it-can-not-be-moved-resized-minimized-maximized
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGING && _processing)
            {
                var wp = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);
                wp.flags |= (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE;
                Marshal.StructureToPtr(wp, lParam, false);
            }
            return IntPtr.Zero;
        }

        #endregion //window move/resize lock
    }
}
