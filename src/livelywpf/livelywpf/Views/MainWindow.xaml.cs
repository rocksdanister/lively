using livelywpf.Core;
using livelywpf.Models;
using livelywpf.Services;
using livelywpf.ViewModels;
using livelywpf.Views;
using livelywpf.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Wpf.UI.XamlHost;
//using ModernWpf.Controls;
using ModernWpf.Media.Animation;
using NLog;
using Octokit;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Design.Behavior;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Networking.NetworkOperators;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Windows.UI.Xaml.Controls.NavigationViewItem debugMenu;
        public static bool IsExit { get; set; } = false;
        private NavigationView navView;
        private readonly IDesktopCore desktopCore;
        private readonly IUserSettingsService userSettings;
        private readonly SettingsViewModel settingsVm;

        public MainWindow(IUserSettingsService userSettings, IDesktopCore desktopCore, SettingsViewModel settingsVm)
        {
            this.desktopCore = desktopCore;
            this.userSettings = userSettings;
            this.settingsVm = settingsVm;

            InitializeComponent();
            wallpaperStatusText.Text = $"{desktopCore.Wallpapers.Count} Active Wallpaper(s)";
            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;
            Logger.Debug("MainWindow ctor initialized..");
        }

        #region navigation

        private void MyNavView_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;
            navView = (Windows.UI.Xaml.Controls.NavigationView)windowsXamlHost.Child;
            if (navView != null)
            {
                navView.OpenPaneLength = 50;
                navView.IsPaneToggleButtonVisible = false;
                navView.IsBackEnabled = false;
                navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
                navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                navView.MenuItems.Add(CreateMenu(Properties.Resources.TitleLibrary, "library", "\uE8F1"));
                navView.MenuItems.Add(CreateMenu(Properties.Resources.TitleAddWallpaper, "add", "\uE710"));
                navView.MenuItems.Add(CreateMenu(Properties.Resources.TitleHelp, "help", "\uE897"));
                navView.MenuItems.Add(CreateMenu(Properties.Resources.TitleAbout, "about", "\uE90A"));
                navView.MenuItems.Add(debugMenu = CreateMenu(Properties.Resources.TitleDebug, "debug", "\uEBE8", userSettings.Settings.DebugMenu));
                settingsVm.DebugMenuVisibilityChange += SettingsVM_DebugMenuVisibilityChange;
                navView.ItemInvoked += NavView_ItemInvoked;
                NavViewNavigate("library");
            }
        }

        private void SettingsVM_DebugMenuVisibilityChange(object sender, bool visibility)
        {
            if (debugMenu != null)
            {
                debugMenu.Visibility = visibility ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            Windows.UI.Xaml.Controls.NavigationView navView =
                (Windows.UI.Xaml.Controls.NavigationView)sender;

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
            if (navView != null)
            {
                navView.SelectedItem = navView.MenuItems.First(x => ((NavigationViewItem)x).Tag.ToString() == tag);
                NavigatePage(tag);
            }
        }

        private void NavigatePage(string tag)
        {
            switch (tag)
            {
                case "library":
                    ContentFrame.Navigate(typeof(LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "add":
                    ContentFrame.Navigate(typeof(AddWallpaperView), new Uri("Views/AddWallpaperView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "about":
                    ContentFrame.Navigate(typeof(AboutView), new Uri("Views/AboutView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "help":
                    ContentFrame.Navigate(typeof(HelpView), new Uri("Views/HelpView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "debug":
                    ContentFrame.Navigate(typeof(DebugView), new Uri("Views/DebugView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                default:
                    ContentFrame.Navigate(typeof(LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo()); 
                    break;
            }
        }

        //Glyph code: https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font
        private Windows.UI.Xaml.Controls.NavigationViewItem CreateMenu(string menuName, string tag, string glyph, bool visibility = true)
        {
            Windows.UI.Xaml.Controls.NavigationViewItem item = new NavigationViewItem
            {
                Name = menuName,
                Content = menuName,
                Tag = tag,
                Icon = new FontIcon()
                {
                    FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                    Glyph = glyph
                },
                Visibility = visibility ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed,
            };
            return item;
        }

        private void ContentFrame_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            //fix: https://github.com/rocksdanister/lively/issues/114
            //When backspace is pressed while focused on frame hosting uwp usercontrol textbox, the key is passed to frame also.
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                e.Cancel = true;
            }
        }

        #endregion //navigation

        #region window mgmnt

        /// <summary>
        /// Makes content frame null and GC call.<br>
        /// Xaml Island gif image playback not pausing fix.</br>
        /// </summary>
        public void HideWindow()
        {
            ContentFrame.Content = null;
            this.Hide();
            GC.Collect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsExit)
            {
                e.Cancel = true;
                HideWindow();
            }
            else
            {
                //todo
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (navView != null && (bool)e.NewValue)
            {
                if (ContentFrame.Content == null)
                {
                    navView.SelectedItem = navView.MenuItems.ElementAt(0);
                    ContentFrame.Navigate(typeof(LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new SuppressNavigationTransitionInfo());
                }
            }
        }

        #endregion //window mdmnt

        #region wallpaper statusbar

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
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
                wallpaperStatusText.Text =  $"{desktopCore.Wallpapers.Count} Active Wallpaper(s)";
            }));
        }

        private Screen.ScreenLayoutView layoutWindow = null;
        public void ShowControlPanelDialog()
        {
            if (layoutWindow == null)
            {
                var appWindow = App.Services.GetRequiredService<MainWindow>();
                layoutWindow = new Screen.ScreenLayoutView();
                if (appWindow.IsVisible)
                {
                    layoutWindow.Owner = appWindow;
                    layoutWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    layoutWindow.Width = appWindow.Width / 1.5;
                    layoutWindow.Height = appWindow.Height / 1.5;
                }
                else
                {
                    layoutWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
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
    }
}
