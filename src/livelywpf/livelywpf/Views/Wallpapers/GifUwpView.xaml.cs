using livelywpf.Helpers;
using Microsoft.Toolkit.Wpf.UI.XamlHost;
using System;
using System.Windows;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Media.Imaging;

namespace livelywpf.Views.Wallpapers
{
    /// <summary>
    /// References:
    /// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.bitmapimage.play?view=winrt-19041
    /// </summary>
    public partial class GifUwpView : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        BitmapImage BitMapImg { get; set; }
        private string FilePath { get; set; }
        private readonly WallpaperScaler stretch;
        public GifUwpView(string filePath, WallpaperScaler stretch = WallpaperScaler.fill)
        {
            FilePath = filePath;
            this.stretch = stretch;
            InitializeComponent();
            this.Loaded += GIFViewUWP_Loaded;
        }

        private void GIFViewUWP_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }

        private void ImageUWP_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;

            Windows.UI.Xaml.Controls.Image imgElement =
                (Windows.UI.Xaml.Controls.Image)windowsXamlHost.Child;

            if (imgElement != null)
            {
                BitMapImg = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                BitMapImg.ImageFailed += BitMapImg_ImageFailed;
                imgElement.Stretch = (Windows.UI.Xaml.Media.Stretch)stretch;
                try
                {
                    BitMapImg.UriSource = new Uri(FilePath);
                }
                catch(Exception ex)
                {
                    Logger.Error(ex.ToString());
                    return;
                }
                imgElement.Source = BitMapImg;
            }
        }

        private void BitMapImg_ImageFailed(object sender, Windows.UI.Xaml.ExceptionRoutedEventArgs e)
        {
            Logger.Error(e.ErrorMessage);
        }

        public void Play()
        {
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Media.Imaging.BitmapImage", "IsPlaying") && 
                !BitMapImg.IsPlaying)
            {
                BitMapImg.Play();
            }
        }

        public void Stop()
        {
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Media.Imaging.BitmapImage", "IsPlaying") && 
                BitMapImg.IsPlaying)
            {
                BitMapImg.Stop();
            }
        }

        public void Pause()
        {
            Stop();
        }
    }
}
