using ImageMagick;
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
        readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();
        readonly int gifAnimationDelay = (int)Math.Round((1f / 15f * 1000f)); //in milliseconds
        readonly int gifSaveAnimationDelay = (int)Math.Round((1f / 90f) * 1000f);
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
            HWND = wp.GetHWND();
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

            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
            }
            //detach wallpaper window from this dialogue.
            WindowOperations.SetParentSafe(HWND, IntPtr.Zero);
        }

        private void CaptureLoop(object sender, EventArgs e)
        {
            Rect previewPanelPos = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WindowOperations.GetElementPixelSize(PreviewBorder);

            if(File.Exists(Path.Combine(thumbnailPathTemp, "lively_t.jpg")))
            {
                try
                {
                    File.Delete(Path.Combine(thumbnailPathTemp, "lively_t.jpg"));
                }
                catch
                {
                    dispatcherTimer.Stop();
                }
            }

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
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
            }
            _processing = true;
            Rect previewPanelPos = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WindowOperations.GetElementPixelSize(PreviewBorder);

            //wait before capturing thumbnail..incase wallpaper is not loaded yet.
            await Task.Delay(100);
            try
            {
                //delete CaptureLoop() thumbnail if any.
                File.Delete(Path.Combine(saveDirectory, "lively_t.jpg"));
            }
            catch { }

            //thumbnail capture
            CaptureScreen.CopyScreen(
               saveDirectory,
               "lively_t.jpg",
               (int)previewPanelPos.Left,
               (int)previewPanelPos.Top,
               (int)previewPanelSize.Width,
               (int)previewPanelSize.Height);
            //wallpaper.GetWallpaperData().LivelyInfo.Thumbnail = Path.Combine(saveDirectory, "lively_t.jpg");
            ThumbnailUpdated?.Invoke(this, Path.Combine(saveDirectory, "lively_t.jpg"));

            double progress = 0;
            //preview clip (animated gif file).
            if (Program.SettingsVM.Settings.GifCapture)
            {
                //generate screen capture images.
                for (int i = 0; i < gifTotalFrames; i++)
                {
                    //updating the position incase window is moved.
                    previewPanelPos = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
                    CaptureScreen.CopyScreen(
                                saveDirectory,
                                i.ToString(CultureInfo.InvariantCulture) + ".jpg",
                                (int)previewPanelPos.Left,
                                (int)previewPanelPos.Top,
                                (int)previewPanelSize.Width,
                                (int)previewPanelSize.Height);

                    await Task.Delay(gifAnimationDelay);
                    //upto 99% 
                    progress = ((i + 1f) / (gifTotalFrames + 1f))*100f;
                    CaptureProgress?.Invoke(this, progress);
                }

                //create animated gif from captured images.
                await Task.Run(() => CreateGif(saveDirectory));
                PreviewUpdated?.Invoke(this, Path.Combine(saveDirectory, "lively_p.gif"));

                //deleting the capture frames.
                for (int i = 0; i < gifTotalFrames; i++)
                {
                    try
                    {
                        File.Delete(saveDirectory + "\\" + i.ToString(CultureInfo.InvariantCulture) + ".jpg");
                    }
                    catch { }
                }

            }

            _processing = false;
            CaptureProgress?.Invoke(this, 100);
        }

        private void CreateGif(string saveDirectory)
        {
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
            dispatcherTimer.Tick += new EventHandler(CaptureLoop);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 3000);
            dispatcherTimer.Start();
        }

        #endregion
    }
}
