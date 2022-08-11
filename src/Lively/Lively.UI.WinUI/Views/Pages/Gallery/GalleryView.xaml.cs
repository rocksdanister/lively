using Lively.Gallery.Client;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages.Gallery
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GalleryView : Page
    {
        private bool isAuthenticated = false;
        private readonly List<(Type Page, string tag)> pages = new List<(Type Page, string tag)>
        {
            (typeof(GalleryLoginView), "login"),
            (typeof(GalleryLibraryView), "library"),
            (typeof(GalleryFeaturedView), "featured"),
            (typeof(GallerySubscriptionView), "subscription"),
            (typeof(ManageAccountView), "profile"),
        };

        public GalleryView()
        {
            this.InitializeComponent();
            var gallery = App.Services.GetRequiredService<GalleryClient>();
            if (gallery.IsLoggedIn)
            {
                NavigatePage("library");
                isAuthenticated = true;
            }
            else
            {
                NavigatePage("login");
                //navView.IsPaneVisible = false;
                navView.Margin = new Thickness(-50, 0, 0, 0);
                gallery.LoggedIn += (_, _) =>
                {
                    isAuthenticated = true;
                    this.DispatcherQueue.TryEnqueue(async () =>
                    {
                        await Task.Delay(2000);
                        NavigatePage("library");
                        navView.Margin = new Thickness(0);
                        //ArgumentException for navView.IsPaneVisible = true
                        //navView.IsPaneVisible = true;
                    });
                };
            }
        }

        private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null && isAuthenticated)
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
                contentFrame.Navigate(nextNavPageType, arg, new DrillInNavigationTransitionInfo());

                var item = navView.MenuItems.FirstOrDefault(x => ((NavigationViewItem)x).Tag.ToString() == tag) ?? 
                    navView.FooterMenuItems.FirstOrDefault(x => ((NavigationViewItem)x).Tag.ToString() == tag);
                //Show selection only if visible.
                navView.SelectedItem = ((UIElement)item).Visibility != Visibility.Collapsed ? item : navView.SelectedItem;
            }
        }
    }
}
