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
            controlPanelLabel.Label = $"{desktopCore.Wallpapers.Count} active wallpaper(s)";
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

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            CreateMainMenu();
            NavViewNavigate(NavPages.library);
        }

        public void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                CreateSettingsMenu();
                NavViewNavigate(NavPages.settingsGeneral);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigatePage(navItemTag);
            }
        }

        public void NavViewNavigate(NavPages item)
        {
            string tag = item switch
            {
                NavPages.library => "library",
                NavPages.gallery => "gallery",
                NavPages.help => "help",
                NavPages.settingsGeneral => "general",
                NavPages.settingsPerformance => "performance",
                NavPages.settingsWallpaper => "wallpaper",
                NavPages.settingsAudio => "audio",
                NavPages.settingsSystem => "system",
                NavPages.settingsMisc => "misc",
                _ => "library"
            };
            navView.SelectedItem = navView.MenuItems.First(x => ((NavigationViewItem)x).Tag.ToString() == tag);
            NavigatePage(tag);
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            CreateMainMenu();
            NavViewNavigate(NavPages.library);
        }

        private void NavigatePage(string tag)
        {
            switch (tag)
            {
                case "library":
                    contentFrame.Navigate(typeof(LibraryView), null, new DrillInNavigationTransitionInfo());
                    break;
                case "general":
                    contentFrame.Navigate(typeof(SettingsGeneralView), null, new DrillInNavigationTransitionInfo());
                    break;
                case "performance":
                    contentFrame.Navigate(typeof(SettingsPerformanceView), null, new DrillInNavigationTransitionInfo());
                    break;
                case "help":
                    contentFrame.Navigate(typeof(AboutView), null, new DrillInNavigationTransitionInfo());
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

        private void CreateMainMenu()
        {
            navView.MenuItems.Clear();
            navView.FooterMenuItems.Clear();
            navView.IsSettingsVisible = true;
            navCommandBar.Visibility = Visibility.Visible;
            navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
            navView.MenuItems.Add(CreateMenu("Library", "library", "\uE8A9"));
            navView.MenuItems.Add(CreateMenu("Gallery", "gallery", "\uE719"));
            navView.FooterMenuItems.Add(CreateMenu(string.Empty, "help", "\uE897"));
        }

        private void CreateSettingsMenu()
        {
            navView.MenuItems.Clear();
            navView.FooterMenuItems.Clear();
            navView.IsSettingsVisible = false;
            navCommandBar.Visibility = Visibility.Collapsed;
            navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
            navView.MenuItems.Add(CreateMenu("General", "general"));
            navView.MenuItems.Add(CreateMenu("Performance", "performance"));
            navView.MenuItems.Add(CreateMenu("Wallpaper", "wallpaper"));
            navView.MenuItems.Add(CreateMenu("Audio", "audio"));
            navView.MenuItems.Add(CreateMenu("System", "system"));
            navView.MenuItems.Add(CreateMenu("Misc", "misc"));
        }

        public enum NavPages
        {
            library,
            gallery,
            help,
            settingsGeneral,
            settingsPerformance,
            settingsWallpaper,
            settingsAudio,
            settingsSystem,
            settingsMisc
        }

        #region helpers

        private NavigationViewItem CreateMenu(string menuName, string tag, string glyph = "")
        {
            var item = new NavigationViewItem
            {
                Name = menuName,
                Content = menuName,
                Tag = tag,
            };
            if (!string.IsNullOrEmpty(glyph))
            {
                item.Icon = new FontIcon()
                {
                    Glyph = glyph
                };
            }
            return item;
        }

        #endregion //helpers
    }
}
