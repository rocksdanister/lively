using livelywpf.Views;
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

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool IsExit { get; set; } = false;
        private NavigationView navView;
        private Windows.UI.Xaml.Controls.NavigationViewItem debugMenu;
        public MainWindow()
        {
            InitializeComponent();
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
                navView.MenuItems.Add(debugMenu = CreateMenu(Properties.Resources.TitleDebug, "debug", "\uEBE8", Program.SettingsVM.Settings.DebugMenu));
                Program.SettingsVM.DebugMenuVisibilityChange += SettingsVM_DebugMenuVisibilityChange;
                navView.ItemInvoked += NavView_ItemInvoked;
                NavViewNavigate("library");
            }
        }

        private void SettingsVM_DebugMenuVisibilityChange(object sender, bool visibility)
        {
            if(debugMenu != null)
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
                ContentFrame.Navigate(typeof(livelywpf.Views.SettingsView), new Uri("Views/SettingsView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
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
                    ContentFrame.Navigate(typeof(livelywpf.Views.LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "add":
                    ContentFrame.Navigate(typeof(livelywpf.Views.AddWallpaperView), new Uri("Views/AddWallpaperView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "about":
                    ContentFrame.Navigate(typeof(livelywpf.Views.AboutView), new Uri("Views/AboutView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "help":
                    ContentFrame.Navigate(typeof(livelywpf.Views.HelpView), new Uri("Views/HelpView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                case "debug":
                    ContentFrame.Navigate(typeof(livelywpf.Views.DebugView), new Uri("Views/DebugView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
                    break;
                default:
                    ContentFrame.Navigate(typeof(livelywpf.Views.LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo()); 
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsExit)
            {
                e.Cancel = true;
                ContentFrame.Content = null;
                this.Hide();
                GC.Collect();
            }
            else
            {
                //todo
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(navView != null && (bool)e.NewValue)
            {
                if(ContentFrame.Content == null)
                {
                    navView.SelectedItem = navView.MenuItems.ElementAt(0);
                    ContentFrame.Navigate(typeof(livelywpf.Views.LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new SuppressNavigationTransitionInfo());
                }
            }
        }

        #endregion //window mdmnt

        #region wallpaper statusbar

        TextBlock wallpaperStatusText;
        private void ScreenLayoutBar_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;
            var btn = (Windows.UI.Xaml.Controls.Button)windowsXamlHost.Child;
            if (btn != null)
            {
                var toolTip = new ToolTip
                {
                    Content = Properties.Resources.TitleScreenLayout
                };
                ToolTipService.SetToolTip(btn, toolTip);

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                };
                stackPanel.Children.Add(new FontIcon()
                {
                    FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                    //Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    Glyph = "\uE7F4"
                });
                wallpaperStatusText = new TextBlock()
                {
                    Text = SetupDesktop.Wallpapers.Count.ToString(),
                    FontSize = 16,
                    //Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    Margin = new Windows.UI.Xaml.Thickness(5, -3.5, 0, 0)
                };
                stackPanel.Children.Add(wallpaperStatusText);

                //btn.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 25, 25));
                btn.Content = stackPanel;
                btn.Click += Btn_Click;
                SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
            }
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            _ = this.Dispatcher.BeginInvoke(new Action(() => { wallpaperStatusText.Text = SetupDesktop.Wallpapers.Count.ToString(); }));
        }

        ScreenLayoutView layoutWindow = null;
        private void Btn_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ShowControlPanelDialog();
        }

        public void ShowControlPanelDialog()
        {
            if (layoutWindow == null)
            {
                layoutWindow = new ScreenLayoutView()
                {
                    Owner = App.AppWindow,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                    Width = App.AppWindow.Width / 1.5,
                    Height = App.AppWindow.Height / 1.5,
                };
                layoutWindow.Closed += LayoutWindow_Closed;
                layoutWindow.Show();
            }
            else
            {
                layoutWindow.Activate();
            }

        }

        private void LayoutWindow_Closed(object sender, EventArgs e)
        {
            layoutWindow = null;
            this.Activate();
        }

        #endregion //wallpaper statusbar

        #region window msg

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SHOWLIVELY)
            {
                Program.ShowMainWindow();   
            }
            return IntPtr.Zero;
        }

        #endregion //window msg
    }
}
