using ImageMagick;
using livelywpf.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Storage;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for LibraryPreviewView.xaml
    /// </summary>
    public partial class LibraryPreviewView : Window
    {
        bool _processing = false;
        readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();
        readonly string saveDirectory;
        readonly int gifAnimationDelay = (int)Math.Round((1f / 15f * 1000f)); //in milliseconds
        readonly int gifSaveAnimationDelay = (int)Math.Round((1f / 90f) * 1000f);
        readonly int gifTotalFrames = 60;
        readonly IWallpaper wallpaper;

        public LibraryPreviewView(IWallpaper wp)
        {
            wallpaper = wp;
            saveDirectory = wallpaper.GetWallpaperData().LivelyInfoFolderPath;
            FileOperations.EmptyDirectory(saveDirectory);
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowOperations.SetProgramToFramework(this, wallpaper.GetHWND(), PreviewBorder);
            //capture thumbnail every few seconds while user is shown wallpaper metadata preview.
            dispatcherTimer.Tick += new EventHandler(CaptureLoop);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 3000);
            dispatcherTimer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_processing)
            {
                e.Cancel = true;
                return;
            }
            //detach wallpaper window from this dialogue.
            WindowOperations.SetParentSafe(wallpaper.GetHWND(), IntPtr.Zero);
        }

        private void CaptureLoop(object sender, EventArgs e)
        {
            Rect previewPanelPos = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
            Size previewPanelSize = WindowOperations.GetElementPixelSize(PreviewBorder);

            if(File.Exists(Path.Combine(saveDirectory, "lively_t.jpg")))
            {
                try
                {
                    File.Delete(Path.Combine(saveDirectory, "lively_t.jpg"));
                    wallpaper.GetWallpaperData().ImagePath = null;
                }
                catch
                {
                    dispatcherTimer.Stop();
                }
            }

            //thumbnail capture
            CaptureScreen.CopyScreen(
               saveDirectory,
               "lively_t.jpg",
               (int)previewPanelPos.Left,
               (int)previewPanelPos.Top,
               (int)previewPanelSize.Width,
               (int)previewPanelSize.Height);
            wallpaper.GetWallpaperData().ImagePath = Path.Combine(saveDirectory, "lively_t.jpg");
        }

        private async void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            _processing = true;
            OkBtn.IsEnabled = false;
            gifProgressBar.Maximum = gifTotalFrames;
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
            wallpaper.GetWallpaperData().LivelyInfo.Thumbnail = Path.Combine(saveDirectory, "lively_t.jpg");

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

                    if ((i + 1) > gifProgressBar.Maximum)
                        gifProgressBar.Value = gifProgressBar.Maximum;
                    else
                        gifProgressBar.Value = i + 1;
                }

                //create animated gif from captured images.
                await Task.Run(() => CreateGif());
                //deleting the capture frames.
                for (int i = 0; i < gifTotalFrames; i++)
                {
                    try
                    {
                        File.Delete(saveDirectory + "\\" + i.ToString(CultureInfo.InvariantCulture) + ".jpg");
                    }
                    catch { }
                }

                wallpaper.GetWallpaperData().LivelyInfo.Preview = Path.Combine(saveDirectory, "lively_p.gif");
                wallpaper.GetWallpaperData().ImagePath = Path.Combine(saveDirectory, "lively_p.gif");
            }
            else
            {
                wallpaper.GetWallpaperData().ImagePath = Path.Combine(saveDirectory, "lively_t.jpg");
            }
            wallpaper.GetWallpaperData().DataType = LibraryTileType.ready;
            LivelyInfoJSON.SaveWallpaperMetaData(wallpaper.GetWallpaperData().LivelyInfo, Path.Combine(wallpaper.GetWallpaperData().LivelyInfoFolderPath, "LivelyInfo.json"));
            Program.LibraryVM.SortExistingWallpaper(wallpaper.GetWallpaperData());

            if(Program.SettingsVM.Settings.LivelyZipGenerate)
            {
                string savePath = "";
                var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "Select location to save the file",
                    Filter = "Lively/zip file|*.zip",
                    FileName = wallpaper.GetWallpaperData().Title,
                };
                if (saveFileDialog1.ShowDialog() == true)
                {
                    savePath = saveFileDialog1.FileName;
                }
                if (!String.IsNullOrEmpty(savePath))
                {
                    Program.LibraryVM.WallpaperExport(wallpaper.GetWallpaperData(), savePath);
                }
            }

            _processing = false;
            this.Close();
        }

        private void CreateGif()
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
                /*
                #region resize
                collection.Coalesce();

                foreach (MagickImage image in collection)
                {
                    image.Resize(192, 108);
                }
                #endregion resize
                */
                // Save gif
                collection.Write(saveDirectory + "\\lively_p.gif");
            }
        }
    }
}
