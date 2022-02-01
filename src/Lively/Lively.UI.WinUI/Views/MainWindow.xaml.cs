using Lively.Grpc.Client;
using Lively.UI.WinUI.Views;
using Lively.UI.WinUI.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SettingsUI.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly IDesktopCoreClient desktopCore;

        public MainWindow(IDesktopCoreClient desktopCore)
        {
            this.desktopCore = desktopCore;

            this.InitializeComponent();
            this.Title = "Lively Wallpaper (WinUI)";
            NavViewNavigate("library");
            controlPanelLabel.Label = $"{desktopCore.Wallpapers.Count} active wallpaper(s)";

            //navView.ItemInvoked += NavView_ItemInvoked;
            navView.SelectionChanged += NavView_SelectionChanged;
            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;

            //ExtendsContentIntoTitleBar = true;
            //SetTitleBar(TitleBar);
        }

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            _ = this.DispatcherQueue.TryEnqueue(() =>
            {
                //wallpaper focus steal fix.
                if (this.Visible)
                {
                    this.Activate();
                }
                controlPanelLabel.Label = $"{desktopCore.Wallpapers.Count} active wallpaper(s)";
            });
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SettingsView), null);
            }
            else if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();
                NavigatePage(navItemTag);
            }
        }

        /*
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                contentFrame.Navigate(typeof(SettingsView), null);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigatePage(navItemTag);
            }
        }
        */

        public void NavViewNavigate(string tag)
        {
            navView.SelectedItem = navView.MenuItems.First(x => ((NavigationViewItem)x).Tag.ToString() == tag);
            NavigatePage(tag);
        }

        private void NavigatePage(string tag)
        {
            switch (tag)
            {
                case "library":
                    contentFrame.Navigate(typeof(LibraryView), null);
                    break;
                default:
                    //TODO
                    break;
            }
        }

        private void AddWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ShowAddWallpaper();
        }

        private void ControlPanelButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ShowControlPanel();
        }

        private async Task ShowControlPanel()
        {
            var dialog = new ContentDialog()
            {
                Title = "Choose display",
                Content = new ScreenLayoutView(),
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot,
            };
            await dialog.ShowAsyncQueue();
        }

        private async Task ShowAddWallpaper()
        {
            var dialog = new ContentDialog()
            {
                Title = "Add wallpaper",
                Content = new AddWallpaperView(),
                PrimaryButtonText = "Continue",
                CloseButtonText = "Cancel",
                IsPrimaryButtonEnabled = false,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot,
            };
            await dialog.ShowAsyncQueue();
        }
    }
}
