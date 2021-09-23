using livelywpf.Helpers;
using livelywpf.Views;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;
using ModernWpf.Media.Animation;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            wallpaperStatusText.Text = SetupDesktop.Wallpapers.Count.ToString();
            SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
            this.DataContext = Program.SettingsVM;
            Logger.Debug("MainWindow ctor initialized..");
        }

        #region navigation

        private void MyNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navView = (ModernWpf.Controls.NavigationView)sender;

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
                //teaching tip - control panel.
                if (!Program.SettingsVM.Settings.ControlPanelOpened &&
                    this.WindowState != WindowState.Minimized &&
                    this.Visibility == Visibility.Visible)
                {
                    ModernWpf.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(statusBtn);
                    Program.SettingsVM.Settings.ControlPanelOpened = true;
                    Program.SettingsVM.UpdateConfigFile();
                }
                //wallpaper focus steal fix.
                if (this.IsVisible && (layoutWindow == null || layoutWindow.Visibility != Visibility.Visible))
                {
                    this.Activate();
                }
                wallpaperStatusText.Text = SetupDesktop.Wallpapers.Count.ToString();
            }));
        }

        ScreenLayoutView layoutWindow = null;
        public void ShowControlPanelDialog()
        {
            if (layoutWindow == null)
            {
                layoutWindow = new ScreenLayoutView();
                if (App.AppWindow.IsVisible)
                {
                    layoutWindow.Owner = App.AppWindow;
                    layoutWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    layoutWindow.Width = App.AppWindow.Width / 1.5;
                    layoutWindow.Height = App.AppWindow.Height / 1.5;
                }
                else
                {
                    layoutWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                }
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

        private void statusBtn_Click(object sender, RoutedEventArgs e)
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

                if (droppedFiles.Count() == 1)
                {
                    var item = droppedFiles[0];
                    Logger.Info("Dropped File, Selecting first file:" + item);
                    try
                    {
                        if (string.IsNullOrWhiteSpace(Path.GetExtension(item)))
                            return;
                    }
                    catch (ArgumentException)
                    {
                        Logger.Info("Invalid character, skipping dropped file.");
                        return;
                    }

                    WallpaperType type = FileFilter.GetLivelyFileType(item);
                    switch (type)
                    {
                        case WallpaperType.web:
                        case WallpaperType.webaudio:
                        case WallpaperType.url:
                        case WallpaperType.video:
                        case WallpaperType.gif:
                        case WallpaperType.videostream:
                        case WallpaperType.picture:
                            {
                                Program.LibraryVM.AddWallpaper(item,
                                    type,
                                    LibraryTileType.processing,
                                    Program.SettingsVM.Settings.SelectedDisplay);
                            }
                            break;
                        case WallpaperType.app:
                        case WallpaperType.bizhawk:
                        case WallpaperType.unity:
                        case WallpaperType.godot:
                        case WallpaperType.unityaudio:
                            {
                                //Show warning before proceeding..
                                var result = await Helpers.DialogService.ShowConfirmationDialog(
                                     Properties.Resources.TitlePleaseWait,
                                     Properties.Resources.DescriptionExternalAppWarning,
                                     Properties.Resources.TextYes,
                                     Properties.Resources.TextNo);

                                if (result == ContentDialogResult.Primary)
                                {
                                    var txtInput = await Helpers.DialogService.ShowTextInputDialog(
                                        Properties.Resources.TextWallpaperCommandlineArgs,
                                        Properties.Resources.TextOK);

                                    Program.LibraryVM.AddWallpaper(item,
                                        WallpaperType.app,
                                        LibraryTileType.processing,
                                        Program.SettingsVM.Settings.SelectedDisplay,
                                        string.IsNullOrWhiteSpace(txtInput) ? null : txtInput);
                                }
                            }
                            break;
                        case (WallpaperType)100:
                            {
                                //lively wallpaper .zip
                                if (ZipExtract.CheckLivelyZip(item))
                                {
                                    _ = Program.LibraryVM.WallpaperInstall(item, false);
                                }
                                else
                                {
                                    await DialogService.ShowConfirmationDialog(Properties.Resources.TextError,
                                        Properties.Resources.LivelyExceptionNotLivelyZip,
                                        Properties.Resources.TextOK);
                                }
                            }
                            break;
                        case (WallpaperType)(-1):
                            {
                                await Helpers.DialogService.ShowConfirmationDialog(
                                    Properties.Resources.TextError,
                                    Properties.Resources.TextUnsupportedFile + " (" + Path.GetExtension(item) + ")",
                                    Properties.Resources.TextClose);
                            }
                            break;
                        default:
                            Logger.Info("No wallpaper type recognised.");
                            break;
                    }
                }
                else if (droppedFiles.Count() > 1)
                {
                    var miw = new MultiWallpaperImport(droppedFiles.ToList())
                    {
                        //This dialog on right-topmost like position and librarypreview window left-topmost.
                        WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                        Left = App.AppWindow.Left + App.AppWindow.Width - (App.AppWindow.Width / 1.5),
                        Top = App.AppWindow.Top + (App.AppWindow.Height / 15),
                        Owner = App.AppWindow,
                        Width = App.AppWindow.Width / 1.5,
                        Height = App.AppWindow.Height / 1.3,
                    };
                    //hanging file explorer..
                    //miw.ShowDialog();
                    miw.Show();
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

        private DateTime prevCrashTime = DateTime.MinValue;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SHOWLIVELY)
            {
                Logger.Info("WM_SHOWLIVELY msg received.");
                Program.ShowMainWindow();
            }
            else if (msg == NativeMethods.WM_TASKBARCREATED)
            {
                //explorer crash detection, new taskbar is created everytime explorer is started..
                Logger.Info("WM_TASKBARCREATED: New taskbar created.");
                if ((DateTime.Now - prevCrashTime).TotalSeconds > 30)
                {
                    SetupDesktop.ResetWorkerW();
                }
                else
                {
                    //todo: move this to core.
                    Logger.Warn("Explorer restarted multiple times in the last 30s.");
                    _ = Task.Run(() => MessageBox.Show(Properties.Resources.DescExplorerCrash,
                        $"{Properties.Resources.TitleAppName} - {Properties.Resources.TextError}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
                    SetupDesktop.TerminateAllWallpapers();
                    SetupDesktop.ResetWorkerW();
                }
                prevCrashTime = DateTime.Now;
            }
            else if (msg == (uint)NativeMethods.WM.QUERYENDSESSION && Program.IsMSIX)
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