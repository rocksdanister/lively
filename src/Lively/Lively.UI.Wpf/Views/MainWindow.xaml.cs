using Lively.UI.Wpf.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using ModernWpf.Media.Animation;
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

namespace Lively.UI.Wpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool IsExit { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();

            this.ContentRendered += (s, e) => NavViewNavigate("library");
            this.Closing += (s, e) => App.ShutDown();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MyNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navView = (ModernWpf.Controls.NavigationView)sender;

            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsView), new Uri("Views/SettingsView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigatePage(navItemTag);
            }
        }

        public void NavViewNavigate(string tag)
        {
            foreach (var x in navView.MenuItems)
            {
                if (((NavigationViewItem)x).Tag.ToString() == tag)
                {
                    navView.SelectedItem = x;
                    break;
                }
            }
            NavigatePage(tag);
        }

        private void NavigatePage(string tag)
        {
            switch (tag)
            {
                case "library":
                    ContentFrame.Navigate(typeof(LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "add":
                    //ContentFrame.Navigate(typeof(AddWallpaperView), new Uri("Views/AddWallpaperView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "about":
                    //ContentFrame.Navigate(typeof(AboutView), new Uri("Views/AboutView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "help":
                    //ContentFrame.Navigate(typeof(HelpView), new Uri("Views/HelpView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "debug":
                    //ContentFrame.Navigate(typeof(DebugView), new Uri("Views/DebugView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                default:
                    //ContentFrame.Navigate(typeof(LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
            }
        }


        private async void Window_Drop(object sender, DragEventArgs e)
        {
            //TODO
        }

        #region wallpaper statusbar

        private Screen.ScreenLayoutView layoutWindow;
        public void ShowControlPanelDialog()
        {
            if (layoutWindow == null)
            {
                var appWindow = App.Services.GetRequiredService<MainWindow>();
                layoutWindow = new Screen.ScreenLayoutView();
                if (appWindow.IsVisible)
                {
                    layoutWindow.Owner = appWindow;
                    layoutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    layoutWindow.Width = appWindow.Width / 1.5;
                    layoutWindow.Height = appWindow.Height / 1.5;
                }
                else
                {
                    layoutWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                layoutWindow.Closed += (s, e) => {
                    layoutWindow = null;
                    this.Activate();
                };
                layoutWindow.Show();
            }
            else
            {
                layoutWindow.Activate();
            }
        }

        private void statusBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowControlPanelDialog();
        }

        #endregion //wallpaper statusbar

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /*
            if (!IsExit)
            {
                e.Cancel = true;
                HideWindow();
            }
            else
            {
                //todo
            }
            */
        }
    }
}
