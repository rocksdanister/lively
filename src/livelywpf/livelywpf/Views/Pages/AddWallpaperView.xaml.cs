using livelywpf.Helpers;
using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            LinkHandler.OpenBrowser(e.Uri);
        }
    }
}
