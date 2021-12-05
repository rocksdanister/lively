using livelywpf.Core;
using livelywpf.Helpers.Archive;
using livelywpf.Helpers.Files;
using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.NetStream;
using livelywpf.Models;
using livelywpf.Services;
using livelywpf.ViewModels;
using livelywpf.Views;
using livelywpf.Views.Dialogues;
using livelywpf.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using ModernWpf.Media.Animation;
using NLog;
using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static bool IsExit { get; set; } = false;

        private readonly IDesktopCore desktopCore;
        private readonly IUserSettingsService userSettings;
        private readonly SettingsViewModel settingsVm;
        private readonly LibraryViewModel libraryVm;

        public MainWindow(IUserSettingsService userSettings, IDesktopCore desktopCore, SettingsViewModel settingsVm, LibraryViewModel libraryVm)
        {
            this.desktopCore = desktopCore;
            this.userSettings = userSettings;
            this.settingsVm = settingsVm;
            this.libraryVm = libraryVm;

            InitializeComponent();
            wallpaperStatusText.Text = desktopCore.Wallpapers.Count.ToString();
            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;
            this.DataContext = settingsVm;
            Logger.Debug("MainWindow ctor initialized..");

            this.ContentRendered += (s, e) => NavViewNavigate("library");
        }

        #region navigation

        private void MyNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var navView = (ModernWpf.Controls.NavigationView)sender;

            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(livelywpf.Views.Pages.SettingsView), new Uri("Views/SettingsView.xaml", UriKind.Relative), new EntranceNavigationTransitionInfo());
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
                                libraryVm.AddWallpaper(item,
                                    type,
                                    LibraryTileType.processing,
                                    userSettings.Settings.SelectedDisplay);
                            }
                            break;
                        case WallpaperType.app:
                        case WallpaperType.bizhawk:
                        case WallpaperType.unity:
                        case WallpaperType.godot:
                        case WallpaperType.unityaudio:
                            {
                                //Show warning before proceeding..
                                var result = await Dialogs.ShowConfirmationDialog(
                                     Properties.Resources.TitlePleaseWait,
                                     Properties.Resources.DescriptionExternalAppWarning,
                                     Properties.Resources.TextYes,
                                     Properties.Resources.TextNo);

                                if (result == ContentDialogResult.Primary)
                                {
                                    var txtInput = await Dialogs.ShowTextInputDialog(
                                        Properties.Resources.TextWallpaperCommandlineArgs,
                                        Properties.Resources.TextOK);

                                    libraryVm.AddWallpaper(item,
                                        WallpaperType.app,
                                        LibraryTileType.processing,
                                        userSettings.Settings.SelectedDisplay,
                                        string.IsNullOrWhiteSpace(txtInput) ? null : txtInput);
                                }
                            }
                            break;
                        case (WallpaperType)100:
                            {
                                //lively wallpaper .zip
                                if (ZipExtract.CheckLivelyZip(item))
                                {
                                    _ = libraryVm.WallpaperInstall(item, false);
                                }
                                else
                                {
                                    await Dialogs.ShowConfirmationDialog(Properties.Resources.TextError,
                                        Properties.Resources.LivelyExceptionNotLivelyZip,
                                        Properties.Resources.TextOK);
                                }
                            }
                            break;
                        case (WallpaperType)(-1):
                            {
                                await Dialogs.ShowConfirmationDialog(
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
                        Left = this.Left + this.Width - (this.Width / 1.5),
                        Top = this.Top + (this.Height / 15),
                        Owner = this,
                        Width = this.Width / 1.5,
                        Height = this.Height / 1.3,
                    };
                    miw.ShowDialog();
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

                if (userSettings.Settings.AutoDetectOnlineStreams &&
                    StreamHelper.IsSupportedStream(uri))
                {
                    libraryVm.AddWallpaper(uri.OriginalString,
                        WallpaperType.videostream,
                        LibraryTileType.processing,
                        userSettings.Settings.SelectedDisplay);
                }
                else
                {
                    libraryVm.AddWallpaper(uri.OriginalString,
                        WallpaperType.url,
                        LibraryTileType.processing,
                        userSettings.Settings.SelectedDisplay);
                }

            }
            NavViewNavigate("library");
        }

        #endregion //file drop
    }
}
