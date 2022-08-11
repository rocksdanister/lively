using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.LivelyProperty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages.ControlPanel
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ControlPanelView : Page
    {
        private readonly List<(Type Page, string tag)> pages = new List<(Type Page, string tag)>
        {
            (typeof(WallpaperLayoutView), "wallpaper"),
            (typeof(ScreensaverLayoutView), "screensaver"),
            (typeof(WallpaperLayoutCustomiseView), "customiseWallpaper"),
        };

        private class Localization
        {
            public string TitleScreenSaver { get; set; }
            public string TitleWallpaper { get; set; }
        }
        private readonly Localization I18n = new Localization();

        public ControlPanelView()
        {
            this.InitializeComponent();
            var vm = App.Services.GetRequiredService<ControlPanelViewModel>();
            this.DataContext = vm;
            vm.NavigatePage += Vm_NavigatePage;

            var i18n = ResourceLoader.GetForViewIndependentUse();
            I18n.TitleWallpaper = i18n.GetString("TitleWallpaper");
            I18n.TitleScreenSaver = i18n.GetString("TitleScreensaver");

            NavigatePage("wallpaper");
        }

        private void Vm_NavigatePage(object sender, ControlPanelViewModel.NavigatePageEventArgs e) => 
            NavigatePage(e.Tag, e.Arg);

        private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
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
                var effect =  pages.FindIndex(p => p.Page.Equals(nextNavPageType)) < pages.FindIndex(p => p.Page.Equals(preNavPageType)) ? 
                    SlideNavigationTransitionEffect.FromLeft : SlideNavigationTransitionEffect.FromRight;
                contentFrame.Navigate(nextNavPageType, arg, new SlideNavigationTransitionInfo() { Effect = effect });

                var item = navView.MenuItems.First(x => ((NavigationViewItem)x).Tag.ToString() == tag);
                //Show selection only if visible.
                navView.SelectedItem = ((UIElement)item).Visibility != Visibility.Collapsed ? item : navView.SelectedItem;
            }
        }
    }
}
