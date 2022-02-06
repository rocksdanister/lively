using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddWallpaperView : Page
    {
        public AddWallpaperView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<AddWallpaperViewModel>();
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            this.addPanel.Visibility = Visibility.Visible;
            this.addPanelDrop.Visibility = Visibility.Collapsed;
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            this.addPanel.Visibility = Visibility.Collapsed;
            this.addPanelDrop.Visibility = Visibility.Visible;
        }

        private void Page_DragLeave(object sender, DragEventArgs e)
        {
            this.addPanel.Visibility = Visibility.Visible;
            this.addPanelDrop.Visibility = Visibility.Collapsed;
        }

    }
}
