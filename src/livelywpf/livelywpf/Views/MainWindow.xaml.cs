using livelywpf.Helpers;
using livelywpf.Views;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;
using ModernWpf.Media.Animation;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool IsExit { get; set; } = false;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            NavViewNavigate("library");
            SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
            this.DataContext = Program.SettingsVM;
        }

        #region navigation

        private void MyNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navView =
           (ModernWpf.Controls.NavigationView)sender;

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
            foreach (var x in MyNavView.MenuItems)
            {
                if (((NavigationViewItem)x).Tag.ToString() == tag)
                {
                    MyNavView.SelectedItem = x;
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

        #endregion //navigation

        #region window mgmnt

        /// <summary>
        /// Makes content frame null and GC call.<br>
        /// Xaml Island gif image playback not pausing fix.</br>
        /// </summary>
        public void HideWindow()
        {
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

        #endregion //window mdmnt

        #region wallpaper statusbar

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            _ = this.Dispatcher.BeginInvoke(new Action(() => {
                if(!Program.SettingsVM.Settings.ControlPanelOpened &&
                    App.AppWindow != null &&
                    App.AppWindow.WindowState != WindowState.Minimized &&
                    App.AppWindow.Visibility == Visibility.Visible)
                {
                    FlyoutBase.ShowAttachedFlyout(statusBtn); 
                    Program.SettingsVM.Settings.ControlPanelOpened = true;
                    Program.SettingsVM.UpdateConfigFile();
                }
                wallpaperStatusText.Text = SetupDesktop.Wallpapers.Count.ToString(); 
            }));
        }

        ScreenLayoutView layoutWindow = null;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ShowControlPanelDialog();
        }

        #endregion //wallpaper statusbar

        #region file drop

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var droppedFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop, true) as string[];
                if ((null == droppedFiles) || (!droppedFiles.Any()))
                    return;
                Logger.Info("Dropped File, Selecting first file:" + droppedFiles[0]);

                if (string.IsNullOrWhiteSpace(Path.GetExtension(droppedFiles[0])))
                    return;

                WallpaperType type;
                if ((type = FileFilter.GetLivelyFileType(droppedFiles[0])) != (WallpaperType)(-1))
                {
                    if (type == (WallpaperType)100)
                    {
                        //lively .zip is not a wallpaper type.
                        if (ZipExtract.CheckLivelyZip(droppedFiles[0]))
                        {
                            Program.LibraryVM.WallpaperInstall(droppedFiles[0], false);
                        }
                        else
                        {
                            await DialogService.ShowConfirmationDialog(Properties.Resources.TextError,
                                Properties.Resources.LivelyExceptionNotLivelyZip,
                                Properties.Resources.TextOK);
                            return;
                        }
                    }
                    else
                    {
                        Program.LibraryVM.AddWallpaper(droppedFiles[0],
                            type,
                            LibraryTileType.processing,
                            Program.SettingsVM.Settings.SelectedDisplay);
                    }
                }
                else
                {
                    await DialogService.ShowConfirmationDialog(Properties.Resources.TextError,
                        Properties.Resources.TextUnsupportedFile + " (" + Path.GetExtension(droppedFiles[0]) + ")",
                        Properties.Resources.TextOK);
                    return;
                }

            }
            else if (e.Data.GetDataPresent(System.Windows.DataFormats.Text))
            {
                string droppedText = (string)e.Data.GetData(System.Windows.DataFormats.Text, true);
                Logger.Info("Dropped Text:" + droppedText);
                if (string.IsNullOrWhiteSpace(droppedText))
                    return;

                Uri uri;
                try
                {
                    uri = Helpers.LinkHandler.SanitizeUrl(droppedText);
                }
                catch
                {
                    return;
                }

                if (Program.SettingsVM.Settings.AutoDetectOnlineStreams &&
                    StreamHelper.IsSupportedStream(uri))
                {
                    Program.LibraryVM.AddWallpaper(uri.OriginalString,
                        WallpaperType.videostream,
                        LibraryTileType.processing,
                        Program.SettingsVM.Settings.SelectedDisplay);
                }
                else
                {
                    Program.LibraryVM.AddWallpaper(uri.OriginalString,
                        WallpaperType.url,
                        LibraryTileType.processing,
                        Program.SettingsVM.Settings.SelectedDisplay);
                }

            }
            App.AppWindow.NavViewNavigate("library");
        }

        #endregion //file drop

        #region window msg

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        //todo: maybe create separate window to handle all window messages globally?
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SHOWLIVELY)
            {
                Program.ShowMainWindow();   
            }
            else if (msg == NativeMethods.WM_TASKBARCREATED)
            {
                //explorer crash detection, new taskbar is created everytime explorer is started..
                Logger.Info("WM_TASKBARCREATED: New taskbar created.");
                SetupDesktop.ResetWorkerW();
            }
            else if (msg == (uint)NativeMethods.WM.QUERYENDSESSION)
            {
                _ = NativeMethods.RegisterApplicationRestart(
                    null,
                    (int)NativeMethods.RestartFlags.RESTART_NO_CRASH |
                    (int)NativeMethods.RestartFlags.RESTART_NO_HANG |
                    (int)NativeMethods.RestartFlags.RESTART_NO_REBOOT);
            }
            //screen message processing...
            _ = Core.DisplayManager.Instance?.OnWndProc(hwnd, (uint)msg, wParam, lParam);

            return IntPtr.Zero;
        }

        #endregion //window msg
    }
}
