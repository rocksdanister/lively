using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.Wpf.ViewModels;
using Lively.UI.Wpf.Views.LivelyProperty.Dialogues;
using Lively.UI.Wpf.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using ModernWpf.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Lively.UI.Wpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly LibraryViewModel libraryVm;

        public MainWindow(IUserSettingsClient userSettings, IDesktopCoreClient desktopCore, LibraryViewModel libraryVm)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.libraryVm = libraryVm;

            InitializeComponent();
            this.ContentRendered += (s, e) => NavViewNavigate("library");
            this.desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            _ = StdInListener();
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
                    ContentFrame.Navigate(typeof(AboutView), new Uri("Views/AboutView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "help":
                    ContentFrame.Navigate(typeof(HelpView), new Uri("Views/HelpView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "debug":
                    //ContentFrame.Navigate(typeof(DebugView), new Uri("Views/DebugView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                default:
                    ContentFrame.Navigate(typeof(LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
            }
        }


        private async void Window_Drop(object sender, DragEventArgs e)
        {
            //TODO
        }

        #region wallpaper statusbar

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            _ = this.Dispatcher.BeginInvoke(new Action(() => {
                //teaching tip - control panel.
                if (!userSettings.Settings.ControlPanelOpened &&
                    this.WindowState != WindowState.Minimized &&
                    this.Visibility == Visibility.Visible)
                {
                    ModernWpf.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(statusBtn);
                    userSettings.Settings.ControlPanelOpened = true;
                    userSettings.Save<ISettingsModel>();
                }
                //wallpaper focus steal fix.
                if (this.IsVisible && (layoutWindow == null || layoutWindow.Visibility != Visibility.Visible))
                {
                    this.Activate();
                }
                wallpaperStatusText.Text = desktopCore.Wallpapers.Count.ToString();
            }));
        }

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
            if (userSettings.Settings.KeepAwakeUI)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                App.ShutDown();
            }
        }

        /// <summary>
        /// std I/O redirect.
        /// </summary>
        private async Task StdInListener()
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        var msg = await Console.In.ReadLineAsync();
                        if (string.IsNullOrEmpty(msg))
                        {
                            //When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        var args = msg.Split(' ');
                        this.Dispatcher.Invoke(() =>
                        {
                            if (args[0].Equals("WM", StringComparison.OrdinalIgnoreCase))
                            {
                                if (args[1].Equals("SHOW", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.Show();
                                }
                            }
                            else if (args[0].Equals("LM", StringComparison.OrdinalIgnoreCase))
                            {
                                if (args[1].Equals("SHOWCONTROLPANEL", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.ShowControlPanelDialog();
                                }
                                else if (args[1].Equals("SHOWCUSTOMISEPANEL", StringComparison.OrdinalIgnoreCase))
                                {
                                    var items = desktopCore.Wallpapers.Where(x => x.LivelyPropertyCopyPath != null);
                                    if (items.Count() != 0)
                                    {
                                        //Usually this msg is sent when span/duplicate layout mode.
                                        var model = libraryVm.LibraryItems.FirstOrDefault(x => items.First().LivelyInfoFolderPath == x.LivelyInfoFolderPath);
                                        if (model != null)
                                        {
                                            var settingsWidget = new LivelyPropertiesTrayWidget(model);
                                            settingsWidget.Show();
                                        }
                                    }
                                }
                            }
                        });
                    }
                });
            }
            catch { }
        }
    }
}
