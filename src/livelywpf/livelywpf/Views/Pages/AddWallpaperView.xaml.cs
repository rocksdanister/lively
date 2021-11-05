using livelywpf.Helpers;
using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Wpf.UI.XamlHost;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace livelywpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for AddWallpaperView.xaml
    /// </summary>
    public partial class AddWallpaperView : Page
    {
        public AddWallpaperView()
        {
            InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<AddWallpaperViewModel>();
        }

        private Windows.UI.Xaml.Controls.Image img;
        private void PreviewGif_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;
            img = (Windows.UI.Xaml.Controls.Image)windowsXamlHost.Child;
            if (img != null)
            {
                var imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "drag_drop_animation.gif");
                if (File.Exists(imgPath))
                {
                    var bmi = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imgPath))
                    {
                        DecodePixelWidth = 384,
                        DecodePixelHeight = 216
                    };
                    img.Source = bmi;
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            LinkHandler.OpenBrowser(e.Uri);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //still rendering otherwise!
            if (img != null)
            {
                img.Source = null;
            }
        }
    }
}
