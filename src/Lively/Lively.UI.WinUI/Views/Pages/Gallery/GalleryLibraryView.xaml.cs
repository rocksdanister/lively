using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Lively.UI.WinUI.ViewModels;
using System.Diagnostics;
using CommunityToolkit.WinUI.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages.Gallery
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GalleryLibraryView : Page
    {
        public GalleryLibraryView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<GalleryViewModel>();
            this.Loaded += (_, _) =>
            {
                var scrollViewer = galleryGridView.FindDescendant<ScrollViewer>();
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            };
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var sv = sender as ScrollViewer;
            var verticalOffset = sv.VerticalOffset;
            var maxVerticalOffset = sv.ScrollableHeight;

            if (maxVerticalOffset < 0 ||
                verticalOffset == maxVerticalOffset)
            {
                // Scrolled to bottom
                MoreMessage.Visibility = Visibility.Visible;
            }
            /*
            else if (maxVerticalOffset < 0 ||
                verticalOffset == 0)
            {
                // Scrolled to top
            }
            */
            else
            {
                // Other
                MoreMessage.Visibility = Visibility.Collapsed;
            }
        }
    }
}
