//using ImageMagick;
using livelywpf.Core;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace livelywpf.Views
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
    /// Interaction logic for LibraryPreviewView.xaml
    /// </summary>
    public partial class LibraryPreviewView : Window, ILibraryPreview
    {
        private bool _processing = false;
        private string thumbnailPathTemp;
        private readonly WallpaperType wallpaperType;
        readonly DispatcherTimer thumbnailCaptureTimer = new DispatcherTimer();
        //Good values: 1. 30c,120s 2. 15c, 90s
        readonly int gifAnimationDelay = 1000 * 1 / 30; //in milliseconds (1/fps)
        readonly int gifSaveAnimationDelay = 1000 * 1 / 120;
        readonly int gifTotalFrames = 60;
        private readonly IntPtr HWND;
        public event EventHandler<string> ThumbnailUpdated;
        public event EventHandler<string> PreviewUpdated;
        public event EventHandler<double> CaptureProgress;
        public event EventHandler WallpaperAttached;

        public LibraryPreviewView(IWallpaper wp)
        {
            LibraryPreviewViewModel vm = new LibraryPreviewViewModel(this, wp);
            this.DataContext = vm;
            this.Closed += vm.OnWindowClosed;
            HWND = wp.GetHWND();
            wallpaperType = wp.GetWallpaperType();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //attach wp hwnd to border ui element.
            WindowOperations.SetProgramToFramework(this, HWND, PreviewBorder);
            WallpaperAttached?.Invoke(this, null);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_processing)
            {
                e.Cancel = true;
                return;
            }

            thumbnailCaptureTimer?.Stop();
            //detach wallpaper window from this dialogue.
            WindowOperations.SetParentSafe(HWND, IntPtr.Zero);
        }

        private void CaptureLoop(object sender, EventArgs e)
        {
            if (File.Exists(Path.Combine(thumbnailPathTemp, "lively_t.jpg")))
            {
                if (wallpaperType == WallpaperType.picture)
                    return;

                try
                {
                    File.Delete(Path.Combine(thumbnailPathTemp, "lively_t.jpg"));
                }
                catch
                {
                    thumbnailCaptureTimer.Stop();
                }
            }

            Rect previewPanelPos = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WindowOperations.GetElementPixelSize(PreviewBorder);

            //thumbnail capture
            CaptureScreen.CopyScreen(
               thumbnailPathTemp,
               "lively_t.jpg",
               (int)previewPanelPos.Left,
               (int)previewPanelPos.Top,
               (int)previewPanelSize.Width,
               (int)previewPanelSize.Height);

            ThumbnailUpdated?.Invoke(this, Path.Combine(thumbnailPathTemp, "lively_t.jpg"));
        }

        private async void CapturePreview(string saveDirectory)
        {
            _processing = true;
            thumbnailCaptureTimer?.Stop();
            taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            Rect previewPanelPos = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WindowOperations.GetElementPixelSize(PreviewBorder);

            //no gif capture wpf only version..
            CaptureProgress?.Invoke(this, 50);
            //wait before capturing thumbnail..incase wallpaper is not loaded yet.
            await Task.Delay(100);
            try
            {
                //try deleting existing files if any..
                File.Delete(Path.Combine(saveDirectory, "lively_t.jpg"));
                File.Delete(Path.Combine(saveDirectory, "lively_p.gif"));
            }
            catch { }

            //final thumbnail capture..
            CaptureScreen.CopyScreen(
               saveDirectory,
               "lively_t.jpg",
               (int)previewPanelPos.Left,
               (int)previewPanelPos.Top,
               (int)previewPanelSize.Width,
               (int)previewPanelSize.Height);
            ThumbnailUpdated?.Invoke(this, Path.Combine(saveDirectory, "lively_t.jpg"));

            _processing = false;
            taskbarItemInfo.ProgressValue = 100f;
            CaptureProgress?.Invoke(this, 100);
        }


        private void CreateGif(string saveDirectory)
        {
            throw new NotImplementedException();
            /*
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                for (int i = 0; i < gifTotalFrames; i++)
                {
                    collection.Add(saveDirectory + "\\" + i.ToString(CultureInfo.InvariantCulture) + ".jpg");
                    collection[i].AnimationDelay = gifSaveAnimationDelay;
                }

                // Optionally reduce colors
                QuantizeSettings settings = new QuantizeSettings
                {
                    Colors = 256,
                };
                collection.Quantize(settings);

                // Optionally optimize the images (images should have the same size).
                collection.Optimize();

                collection.Write(Path.Combine(saveDirectory, "lively_p.gif"));
            }
            */
        }

        #region interface methods

        public void Exit()
        {
            this.Close();
        }

        public void StartCapture(string savePath)
        {
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

        #endregion
    }
}
