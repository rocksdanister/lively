using Microsoft.Toolkit.Wpf.UI.XamlHost;
using ModernWpf.Media.Animation;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Xaml.Controls;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //ContentFrame.Navigate(typeof(livelywpf.Views.LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new SuppressNavigationTransitionInfo());
            SettingsViewModel vm = new SettingsViewModel();
        }


        private void MyNavView_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;

            Windows.UI.Xaml.Controls.NavigationView navView =
                (Windows.UI.Xaml.Controls.NavigationView)windowsXamlHost.Child;

            if (navView != null)
            {
                navView.OpenPaneLength = 50;
                navView.IsPaneToggleButtonVisible = false;
                navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
                navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                navView.MenuItems.Add(CreateMenu("Library", "library", Symbol.Library));
                navView.MenuItems.Add(CreateMenu("Layout", "layout", Symbol.ViewAll));
                navView.MenuItems.Add(CreateMenu("Playlist", "playlist", Symbol.SlideShow));
                navView.MenuItems.Add(CreateMenu("About", "about", Symbol.Comment));
                navView.ItemInvoked += NavView_ItemInvoked;
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            Windows.UI.Xaml.Controls.NavigationView navView =
                (Windows.UI.Xaml.Controls.NavigationView)sender;

            if (args.IsSettingsInvoked)
            {
                //ContentFrame.NavigationService.Navigate(new Uri("Views/SettingsView.xaml", UriKind.Relative));
                ContentFrame.Navigate(typeof(livelywpf.Views.SettingsView), new Uri("Views/SettingsView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();

                switch (navItemTag)
                {
                    case "library":
                        ContentFrame.Navigate(typeof(livelywpf.Views.LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                        break;
                    default:
                        //ContentFrame.Navigate(typeof(livelywpf.Views.LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new DrillInNavigationTransitionInfo()); //new EntranceNavigationTransitionInfo()
                        break;
                }
            }
        }

        private Windows.UI.Xaml.Controls.NavigationViewItem CreateMenu(string menuName, string tag, Symbol icon)
        {
            Windows.UI.Xaml.Controls.NavigationViewItem item = new NavigationViewItem
            {
                Name = menuName,
                Content = menuName,
                Tag = tag,
                Icon = new SymbolIcon(icon)
            };
            return item;
        }

    }
}
