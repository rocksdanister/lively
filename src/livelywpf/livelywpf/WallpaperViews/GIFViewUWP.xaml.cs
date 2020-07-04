using Microsoft.Toolkit.Wpf.UI.XamlHost;
using System;
using System.Windows;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Media.Imaging;


namespace livelywpf
{
    /// <summary>
    /// References:
    /// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.bitmapimage.play?view=winrt-19041
    /// </summary>
    public partial class GIFViewUWP : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        BitmapImage BitMapImg { get; set; }
        private string FilePath { get; set; }

        public GIFViewUWP(string filePath)
        {
            FilePath = filePath;
            InitializeComponent();
        }

        private void ImageUWP_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;

            Windows.UI.Xaml.Controls.Image imgElement =
                (Windows.UI.Xaml.Controls.Image)windowsXamlHost.Child;

            if (imgElement != null)
            {
                BitMapImg = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                imgElement.Stretch = Windows.UI.Xaml.Media.Stretch.Fill;
                BitMapImg.UriSource = new Uri(FilePath);
                //BitMapImg.ImageFailed
                imgElement.Source = BitMapImg;
            }
        }

        public void Play()
        {
            BitMapImg.Play();
        }

        /// <summary>
        /// Will appear dark!
        /// </summary>
        public void Stop()
        {
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Media.Imaging.BitmapImage", "IsPlaying")
             && BitMapImg.IsPlaying == true)
            {
                BitMapImg.Stop();
            }
        }

        /// <summary>
        /// todo: adjust playbackrate or show static picture.
        /// </summary>
        public void Pause()
        {
            throw new NotImplementedException();
        }
    }
}
