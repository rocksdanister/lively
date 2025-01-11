using Lively.UI.Shared.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages.ControlPanel
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ControlPanelView : Page
    {
        private readonly List<(Type Page, string tag)> pages =
        [
            (typeof(WallpaperLayoutView), "wallpaper"),
            (typeof(ScreensaverLayoutView), "screensaver"),
            (typeof(WallpaperLayoutCustomiseView), "customiseWallpaper"),
        ];

        private readonly ControlPanelViewModel viewModel;

        public ControlPanelView(ControlPanelViewModel vm)
        {
            this.InitializeComponent();
            this.viewModel = vm;
            this.DataContext = vm;
            vm.NavigatePage += Vm_NavigatePage;

            NavigatePage("wallpaper");
        }

        private void Vm_NavigatePage(object sender, ControlPanelViewModel.NavigatePageEventArgs e) => 
            NavigatePage(e.Tag, e.Arg);

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigatePage(navItemTag);
            }
        }

        private void NavigatePage(string tag, object arg = null)
        {
            var nextNavPageType = pages.FirstOrDefault(p => p.tag.Equals(tag)).Page;
            // Get the page type before navigation so you can prevent duplicate entries in the backstack.
            var preNavPageType = contentFrame.CurrentSourcePageType;
            // Only navigate if the selected page isn't currently loaded.
            if (!(nextNavPageType is null) && !Type.Equals(preNavPageType, nextNavPageType))
            {
                // ->, <- direction based on order of item on the list. 
                var effect = pages.FindIndex(p => p.Page.Equals(nextNavPageType)) < pages.FindIndex(p => p.Page.Equals(preNavPageType)) ? 
                    SlideNavigationTransitionEffect.FromLeft : SlideNavigationTransitionEffect.FromRight;
                contentFrame.Navigate(nextNavPageType, arg, new SlideNavigationTransitionInfo() { Effect = effect });

                var currentNavViewItem = navView.MenuItems.First(x => ((NavigationViewItem)x).Tag.ToString() == tag) as NavigationViewItem;
                // Show customise page only when in use.
                customiseWallpaperItem.Visibility = currentNavViewItem.Tag.ToString() == customiseWallpaperItem.Tag.ToString() ? Visibility.Visible : Visibility.Collapsed;
                //Show selection only if item is visible.
                navView.SelectedItem = currentNavViewItem .Visibility != Visibility.Collapsed ? currentNavViewItem : navView.SelectedItem;

                // Notify vm to save customisation to disk.
                if (preNavPageType is not null && Type.Equals(preNavPageType, typeof(WallpaperLayoutCustomiseView)))
                    viewModel.WallpaperVm.CustomiseWallpaperPageOnClosed();
            }
        }
    }
}
