using ImageMagick;
using livelywpf.Lively.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static livelywpf.SaveData;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for PreviewWallpaper.xaml
    /// </summary>
    public partial class PreviewWallpaper : Window
    {
        string saveDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\tmpdata\\wpdata";
        int gifAnimationDelay = (int)Math.Round( (1f /SaveData.config.PreviewGIF.CaptureFps) * 1000f); //in milliseconds
        int gifSaveAnimationDelay = (int)Math.Round((1f / SaveData.config.PreviewGIF.GifFps) * 1000f);
        int gifTotalFrames = SaveData.config.PreviewGIF.CaptureFps * SaveData.config.PreviewGIF.CaptureDuration;

        IntPtr processHWND = IntPtr.Zero;
        SaveData.WallpaperLayout layout = new SaveData.WallpaperLayout();
        public PreviewWallpaper(IntPtr handle, SaveData.WallpaperLayout layout)
        {
            FileOperations.EmptyDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\tmpdata\\wpdata"); //clear previous tempdata if exists.

            InitializeComponent();
            this.layout = layout;
            processHWND = handle;

            if (layout.Type == SetupDesktop.WallpaperType.url)
            {
                try
                {
                    Uri uri = new Uri(layout.FilePath);
                    textboxTitle.Text = uri.Segments.Last();
                    //for some urls, output will be: /
                    if(textboxTitle.Text.Equals("/", StringComparison.OrdinalIgnoreCase) || textboxTitle.Text.Equals("//", StringComparison.OrdinalIgnoreCase))
                    {
                        textboxTitle.Text = layout.FilePath.Replace(@"https://www.", "");
                    }
                    textboxTitle.Text = textboxTitle.Text.Replace("/", "");
                }
                catch
                {
                    textboxTitle.Text = layout.FilePath;
                }
            }
            else
            {
                try
                {
                    textboxTitle.Text = System.IO.Path.GetFileNameWithoutExtension(layout.FilePath);
                }
                catch (ArgumentException)
                {
                    textboxTitle.Text = layout.FilePath;
                }

                if (String.IsNullOrWhiteSpace(textboxTitle.Text))
                {
                    textboxTitle.Text = layout.FilePath;
                }
            }

            if ( layout.Type == SetupDesktop.WallpaperType.url || layout.Type == SetupDesktop.WallpaperType.video_stream)
            {
                textboxContact.Text = layout.FilePath;
            }
            //textboxDesc.Text = "You Son of a Bitch, I'm In.";

            this.Closing += PreviewWallpaper_Closing;
            this.Loaded += PreviewWallpaper_Loaded;

            gifProgressBar.Minimum = 0;
            gifProgressBar.Maximum = gifTotalFrames;
            chkBoxCreatePreview.IsChecked = SaveData.config.PreviewGIF.CaptureGif;
            chkBoxCreateZip.IsChecked = SaveData.config.LivelyZipGenerate;
            chkBoxCreatePreview.Checked += ChkBoxCreatePreview_Checked;
            chkBoxCreatePreview.Unchecked += ChkBoxCreatePreview_Checked;
            chkBoxCreateZip.Checked += ChkBoxCreateZip_Checked;
            chkBoxCreateZip.Unchecked += ChkBoxCreateZip_Checked;
        }

        private void ChkBoxCreateZip_Checked(object sender, RoutedEventArgs e)
        {
            SaveData.config.LivelyZipGenerate = chkBoxCreateZip.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void ChkBoxCreatePreview_Checked(object sender, RoutedEventArgs e)
        {
            SaveData.config.PreviewGIF.CaptureGif = chkBoxCreatePreview.IsChecked.Value;
            SaveData.SaveConfig();
        }

        private void PreviewWallpaper_Loaded(object sender, RoutedEventArgs e)
        {
            WindowOperations.SetProgramToFramework(this, processHWND, PreviewBorder);
        }

        bool _InProgressClosing = false;
        public async void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            _InProgressClosing = true;
            chkBoxCreatePreview.IsEnabled = false;
            chkBoxCreateZip.IsEnabled = false;
            OkBtn.IsEnabled = false;

            GenerateLivelyInfo(layout);
            Rect reviewPanel;

            #region previewgif
            //preview clip (animated gif file).
            if (SaveData.config.PreviewGIF.CaptureGif)
            {
                //generate screen capture images.
                for (int i = 0; i < gifTotalFrames; i++)
                {
                    reviewPanel = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
                    CaptureWindow.CopyScreen(saveDirectory, i.ToString(CultureInfo.InvariantCulture) + ".jpg", 
                                (int)(((int)reviewPanel.Left + (int)(reviewPanel.Right)) /2f) - 192/2, (int)(((int)reviewPanel.Top + (int)(reviewPanel.Bottom ))/2f) - 108/2, 192, 108); //384,216
                    await Task.Delay(gifAnimationDelay);

                    this.Dispatcher.Invoke(() =>
                    {
                        if((i + 1) > gifProgressBar.Maximum)
                            gifProgressBar.Value = gifProgressBar.Maximum;
                        else
                            gifProgressBar.Value = i + 1;
                    });
                }
                //create animated gif from captured images.
                await Task.Run(() => CreateGif());
            }
            else
            {
                await Task.Delay(1000); //wait 1sec before capturing thumbnail..incase wallpaper is not loaded yet.
            }
            #endregion

            #region thumbnail
            // 200x200 thumbnail image capture.
            reviewPanel = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
            CaptureWindow.CopyScreen(saveDirectory, "lively_t.jpg", 
                         (int)(((int)reviewPanel.Left + (int)(reviewPanel.Right)) / 2f) - 200 / 2, (int)(((int)reviewPanel.Top + (int)(reviewPanel.Bottom)) / 2f) - 200 / 2, 200, 200);

            #endregion

            _InProgressClosing = false;
            this.Close();
        }

        private void PreviewWallpaper_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_InProgressClosing)
            {
                e.Cancel = true;
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                App.w.LoadWallpaperFromWpDataFolder();
            }));

            //..detach wp window from this dialogue.
            SetupDesktop.SetParentSafe(processHWND, IntPtr.Zero);
        }

        private void CreateGif()
        {
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                for (int i = 0; i < gifTotalFrames; i++)
                {
                    collection.Add(saveDirectory +"\\"+ i.ToString(CultureInfo.InvariantCulture) + ".jpg");
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
                collection.Write(saveDirectory+ "\\lively_p.gif");
            }
        }

        private void GenerateLivelyInfo(WallpaperLayout wpLayout)
        {
            LivelyInfo tmpInfo = new LivelyInfo()
            {
                IsAbsolutePath = true, //absolute filepath, wp files will be located outside of lively folder.
                FileName = wpLayout.FilePath,
                Title = textboxTitle.Text,
                Desc = textboxDesc.Text,
                Contact = textboxContact.Text,
                Author = textboxAuthor.Text,
                Arguments = wpLayout.Arguments,
                Type = wpLayout.Type,
                Thumbnail = "lively_t.jpg",
                Preview = "lively_p.gif"
            };
            /*
            if (SaveData.config.PreviewGIF.CaptureGif)
            {
                tmpInfo.Preview = "lively_p.gif";
            }
            else
            {
                tmpInfo.Preview = null;
            }
            */
            SaveData.SaveWallpaperMetaData(tmpInfo, saveDirectory);
        }

    }
}
