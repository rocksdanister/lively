using Microsoft.Toolkit.Wpf.UI.XamlHost;
//using ModernWpf.Controls;
using ModernWpf.Media.Animation;
using NLog;
using Octokit;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool _isExit = false;
        private NavigationView navView;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MyNavView_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;
            navView = (Windows.UI.Xaml.Controls.NavigationView)windowsXamlHost.Child;
            if (navView != null)
            {
                navView.OpenPaneLength = 50;
                navView.IsPaneToggleButtonVisible = false;
                navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
                navView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                navView.MenuItems.Add(CreateMenu(Properties.Resources.TitleLibrary, "library", Symbol.Library));
                navView.MenuItems.Add(CreateMenu(Properties.Resources.TitleScreenLayout, "layout", Symbol.ViewAll));
                //navView.MenuItems.Add(CreateMenu("Playlist", "playlist", Symbol.SlideShow));
                navView.MenuItems.Add(CreateMenu(Properties.Resources.TitleAbout, "about", Symbol.Comment));
                navView.ItemInvoked += NavView_ItemInvoked;

                navView.SelectedItem = navView.MenuItems.ElementAt(0);
                ContentFrame.Navigate(typeof(livelywpf.Views.LibraryView), new Uri("Views/LibraryView.xaml", UriKind.Relative), new SuppressNavigationTransitionInfo());
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
                    case "about":
                        ContentFrame.Navigate(typeof(livelywpf.Views.AboutView), new Uri("Views/AboutView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExit)
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
                    Glyph = "\uE7F4"
                });
                wallpaperStatusText = new TextBlock()
                {
                    Text = SetupDesktop.Wallpapers.Count.ToString(),
                    FontSize = 16,
                    Margin = new Windows.UI.Xaml.Thickness(5, -3.5, 0, 0)
                };
                stackPanel.Children.Add(wallpaperStatusText);

                btn.Content = stackPanel;
                btn.Click += Btn_Click;
                SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
            }
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                wallpaperStatusText.Text = SetupDesktop.Wallpapers.Count.ToString();
            }));
        }

        private void Btn_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            MessageBox.Show("hi");
        }

        #endregion //wallpaper statusbar

        #region single instance

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

        #endregion //single instance
    }
}
