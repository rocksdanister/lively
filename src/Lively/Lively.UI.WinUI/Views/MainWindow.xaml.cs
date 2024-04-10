using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Models;
using Lively.Gallery.Client;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Services;
using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.LivelyProperty;
using Lively.UI.WinUI.Views.Pages;
using Lively.UI.WinUI.Views.Pages.ControlPanel;
using Lively.UI.WinUI.Views.Pages.Gallery;
using Lively.UI.WinUI.Views.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using WinRT.Interop;
using WinUIEx;
using WinUICommunity;
using Lively.Common.Helpers.Files;
using static Lively.Common.Errors;
using Lively.Common.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<(Type Page, NavPages NavPage)> _pages =
        [
            (typeof(LibraryView), NavPages.library),
            (typeof(GalleryView), NavPages.gallery),
            (typeof(AppUpdateView), NavPages.appUpdate),
            (typeof(SettingsGeneralView), NavPages.settingsGeneral),
            (typeof(SettingsPerformanceView), NavPages.settingsPerformance),
            (typeof(SettingsWallpaperView), NavPages.settingsWallpaper),
            (typeof(SettingsSystemView), NavPages.settingsSystem),
        ];

        private readonly SettingsViewModel settingsVm;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IUserSettingsClient userSettings;
        private readonly LibraryViewModel libraryVm;
        private readonly GalleryClient galleryClient;
        private readonly IAppUpdaterClient appUpdater;
        private readonly AppUpdateViewModel appUpdateVm;
        private readonly IDialogService dialogService;
        private readonly ICommandsClient commands;
        private readonly ResourceLoader i18n;

        public MainWindow(IDesktopCoreClient desktopCore,
            MainViewModel mainViewModel,
            IDialogService dialogService,
            ICommandsClient commands,
            IUserSettingsClient userSettings,
            SettingsViewModel settingsVm,
            LibraryViewModel libraryVm,
            IAppUpdaterClient appUpdater,
            AppUpdateViewModel appUpdateVm,
            GalleryClient galleryClient)
        {
            this.settingsVm = settingsVm;
            this.desktopCore = desktopCore;
            this.libraryVm = libraryVm;
            this.galleryClient = galleryClient;
            this.userSettings = userSettings;
            this.dialogService = dialogService;
            this.commands = commands;
            this.appUpdater = appUpdater;
            this.appUpdateVm = appUpdateVm;

            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            Root.DataContext = mainViewModel;
            i18n = ResourceLoader.GetForViewIndependentUse();
            this.audioSlider.Value = settingsVm.GlobalWallpaperVolume;
            UpdateAudioSliderIcon(settingsVm.GlobalWallpaperVolume);
            this.controlPanelLabel.Label = $"{desktopCore.Wallpapers.Count} {i18n.GetString("ActiveWallpapers/Label")}";
            controlPanelMonitor.Glyph = monitorGlyphs[desktopCore.Wallpapers.Count >= monitorGlyphs.Length ? monitorGlyphs.Length - 1 : desktopCore.Wallpapers.Count];
            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            desktopCore.WallpaperError += DesktopCore_WallpaperError;
            appUpdater.UpdateChecked += AppUpdater_UpdateChecked;

            //App startup is slower if done in NavView_Loaded..
            CreateMainMenu();
            CreateSettingsMenu();
            ShowMainMenu();
            NavViewNavigate(NavPages.library);

            if (!userSettings.Settings.IsFirstRun)
                CompactLabels();

            //ref: https://learn.microsoft.com/en-us/windows/apps/develop/title-bar?tabs=wasdk
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = this.AppWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = ((SolidColorBrush)App.Current.Resources["WindowCaptionForeground"]).Color;

                AppTitleBar.Loaded += AppTitleBar_Loaded;
                AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
                this.Activated += MainWindow_Activated;
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
                this.UseImmersiveDarkModeEx(userSettings.Settings.ApplicationTheme == AppTheme.Dark);
            }

            if (!desktopCore.IsCoreInitialized)
                ShowError(new WorkerWException(i18n.GetString("LivelyExceptionWorkerWSetupFail")));

            //Gallery
            InitializeGallery();
            galleryClient.LoggedIn += (_, _) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateAuthState();
                });
            };
            galleryClient.LoggedOut += (_, _) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (contentFrame.CurrentSourcePageType == typeof(GalleryView))
                    {
                        NavViewNavigate(NavPages.library);
                    }
                    UpdateAuthState();
                    authorizedBtn.Flyout.Hide();
                });
            };

            _ = StdInListener();
        }

        private void CompactLabels()
        {
            separatorLabel1.Visibility = Visibility.Collapsed;
            separatorLabel2.Visibility = Visibility.Collapsed;
            separatorLabel3.Visibility = Visibility.Collapsed;
            controlPanelLabel.LabelPosition = CommandBarLabelPosition.Collapsed;
            addWallpaperLabel.LabelPosition = CommandBarLabelPosition.Collapsed;
            controlPanelLabel.MaxWidth = 50;
            addWallpaperLabel.MaxWidth = 50;
        }

        private void DesktopCore_WallpaperError(object sender, Exception e)
        {
            _ = this.DispatcherQueue.TryEnqueue(() =>
            {
                ShowError(e);
            });
        }

        private void ShowError(Exception e)
        {
            infoBar.IsOpen = true;
            infoBar.ActionButton = new HyperlinkButton
            {
                Content = i18n.GetString("Help/Label"),
                NavigateUri = new Uri("https://github.com/rocksdanister/lively/wiki/Common-Problems"),
            };
            infoBar.Title = i18n.GetString("TextError");
            infoBar.Message = $"{e.Message}\n\nException:\n{e.GetType().Name}";
            infoBar.Severity = InfoBarSeverity.Error;
        }

        private void AppUpdater_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = this.DispatcherQueue.TryEnqueue(() =>
            {
                // If in settings page
                if (navView.MenuItems.FirstOrDefault(x => ((NavigationViewItem)x).Tag.ToString() == NavPages.appUpdate.GetAttrValue()) is not NavigationViewItem navViewItem)
                    return;

                navViewItem.InfoBadge.Opacity = e.UpdateStatus == AppUpdateStatus.available ? 1 : 0;
            });
        }

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            _ = this.DispatcherQueue.TryEnqueue(() =>
            {
                //wallpaper focus steal fix.
                if (this.Visible)
                {
                    if (!userSettings.Settings.ControlPanelOpened)
                    {
                        toggleTeachingTipControlPanel.IsOpen = true;
                        userSettings.Settings.ControlPanelOpened = true;
                        userSettings.Save<SettingsModel>();
                    }
                    //NativeMethods.SetForegroundWindow(this.GetWindowHandleEx());
                    //If its duplicate mode fire the animation more than once.
                    if (userSettings.Settings.WallpaperArrangement != WallpaperArrangement.duplicate || desktopCore.Wallpapers.Count < 2)
                    {
                        activeWallpaperOffsetAnimation.Start();
                    }
                }
                controlPanelLabel.Label = $"{desktopCore.Wallpapers.Count} {i18n.GetString("ActiveWallpapers/Label")}";
                controlPanelMonitor.Glyph = monitorGlyphs[desktopCore.Wallpapers.Count >= monitorGlyphs.Length ? monitorGlyphs.Length - 1 : desktopCore.Wallpapers.Count];
            });
        }

        public void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ShowSettingsMenu();
                NavViewNavigate(NavPages.settingsGeneral);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigatePage(navItemTag);
            }
        }

        public void NavViewNavigate(NavPages item)
        {
            var tag = item.GetAttrValue();
            navView.SelectedItem = navView.MenuItems.First(x => ((NavigationViewItem)x).Tag.ToString() == tag);
            NavigatePage(tag);
        }

        private void NavigatePage(string navItemTag)
        {
            var item = _pages.FirstOrDefault(p => p.NavPage.GetAttrValue().Equals(navItemTag));
            Type _page = item.Page;
            // Get the page type before navigation so you can prevent duplicate entries in the backstack.
            var preNavPageType = contentFrame.CurrentSourcePageType;
            // Only navigate if the selected page isn't currently loaded.
            if (!(_page is null) && !Type.Equals(preNavPageType, _page))
            {
                contentFrame.Navigate(_page, null, new DrillInNavigationTransitionInfo());
                UpdateSuggestBoxState();
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            ShowMainMenu();
            NavViewNavigate(NavPages.library);

            //If audio changed in settings page..
            this.audioSlider.Value = settingsVm.GlobalWallpaperVolume;
        }

        private async void AddWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            var addVm = App.Services.GetRequiredService<AddWallpaperViewModel>();
            var addDialog = new ContentDialog()
            {
                Title = i18n.GetString("AddWallpaper/Label"),
                Content = new AddWallpaperView(addVm),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot,
            };
            addVm.OnRequestAddUrl += async(_, e) =>
            {
                addDialog.Hide();
                var libItem = libraryVm.AddWallpaperLink(e);
                NavViewNavigate(NavPages.library);
                await desktopCore.SetWallpaper(libItem, userSettings.Settings.SelectedDisplay);
            };
            addVm.OnRequestAddFile += async(_, e) =>
            {
                addDialog.Hide();
                if (e.Count == 1)
                {
                    NavViewNavigate(NavPages.library);
                    await CreateWallpaper(e[0]);
                }
                else if (e.Count > 1)
                {
                    NavViewNavigate(NavPages.library);
                    await AddWallpapers(e);
                }
            };
            addVm.OnRequestOpenCreate += async(_, _) =>
            {
                addDialog.Hide();
                await CreateWallpaper(null);
            };
            await addDialog.ShowAsyncQueue();
        }

        public async Task CreateWallpaper(string filePath)
        {
            var creationType = await dialogService.ShowWallpaperCreateDialogAsync(filePath);
            if (creationType is null)
                return;

            switch (creationType)
            {
                case WallpaperCreateType.none:
                    {
                        var item = await AddWallpaper(filePath);
                        if (item is not null && item.DataType == LibraryItemType.processing)
                            await desktopCore.SetWallpaper(item, userSettings.Settings.SelectedDisplay);
                    }
                    break;
                case WallpaperCreateType.depthmap:
                    {
                        filePath ??= await FilePickerUtil.PickSingleFile(WallpaperType.picture);
                        if (filePath is not null)
                        {
                            var result = await dialogService.ShowDepthWallpaperDialogAsync(filePath);
                            if (result is not null)
                                await desktopCore.SetWallpaper(result, userSettings.Settings.SelectedDisplay);
                        }
                    }
                    break;
            }
        }

        public async Task<LibraryModel> AddWallpaper(string filePath)
        {
            LibraryModel result = null;
            try
            {
                if (Path.GetExtension(filePath) == ".zip" && FileUtil.IsFileGreater(filePath, 10485760))
                {
                    importBar.IsOpen = true;
                    importBar.Message = Path.GetFileName(filePath);
                    importBarProgress.IsIndeterminate = true;
                }
                result = await libraryVm.AddWallpaperFile(filePath);
            }
            catch (Exception ex)
            {
                await new ContentDialog()
                {
                    Title = i18n.GetString("TextError"),
                    Content = ex.Message,
                    PrimaryButtonText = i18n.GetString("TextOk"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot,
                }.ShowAsyncQueue();
            }
            finally
            {
                importBar.IsOpen = false;
                importBarProgress.IsIndeterminate = false;
            }
            return result;
        }

        public async Task AddWallpapers(List<string> files)
        {
            try
            {
                importBar.IsOpen = true;
                importBar.Message = "0%";
                importBar.Title = i18n.GetString("TextProcessingWallpaper");
                importBar.ActionButton = new Button
                {
                    Content = i18n.GetString("Cancel/Content"),
                };
                var ct = new CancellationTokenSource();
                importBar.ActionButton.Click += (_, _) =>
                {
                    importBar.ActionButton.Visibility = Visibility.Collapsed;
                    importBar.Title = i18n.GetString("PleaseWait/Text");
                    importBar.Message = "100%";
                    ct.Cancel();
                };
                await libraryVm.AddWallpapers(files, ct.Token, new Progress<int>(percent => { 
                    importBar.Message = $"{percent}%"; 
                    importBarProgress.Value = percent; 
                }));
            }
            finally
            {
                importBar.IsOpen = false;
                importBarProgress.Value = 0;
            }
        }

        private void ControlPanelButton_Click(object sender, RoutedEventArgs e)
        {
            _ = dialogService.ShowControlPanelDialogAsync();
        }

        private void UpdatedButton_Click(object sender, RoutedEventArgs e)
        {
            NavViewNavigate(NavPages.appUpdate);
        }

        private void AppBarCoffeeBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = dialogService.ShowPatreonSupportersDialogAsync();
        }
         

        private void AppBarThemeButton_Click(object sender, RoutedEventArgs e)
        {
            dialogService.ShowThemeDialogAsync();
        }

        private void AppBarHelpButton_Click(object sender, RoutedEventArgs e)
        {
            _ = dialogService.ShowHelpDialogAsync();
        }

        private void AppBarAboutButton_Click(object sender, RoutedEventArgs e)
        {
            _ = dialogService.ShowAboutDialogAsync();
        }

        private void SliderAudio_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            settingsVm.GlobalWallpaperVolume = (int)e.NewValue;
            UpdateAudioSliderIcon(settingsVm.GlobalWallpaperVolume);
        }

        private void CreateMainMenu()
        {
            navView.MenuItems.Add(CreateMenu(i18n.GetString("TitleLibrary"), NavPages.library.GetAttrValue(), "\uE8A9"));
            navView.MenuItems.Add(CreateMenu(i18n.GetString("TitleGallery"), NavPages.gallery.GetAttrValue(), "\uE719"));
            navView.MenuItems.Add(CreateMenu(i18n.GetString("TitleUpdates"), NavPages.appUpdate.GetAttrValue(), "\uE777", new InfoBadge() { Value = 1, Opacity = appUpdater.Status == AppUpdateStatus.available ? 1 : 0 }));
        }

        private void CreateSettingsMenu()
        {
            navView.MenuItems.Add(CreateMenu(i18n.GetString("TitleGeneral"), NavPages.settingsGeneral.GetAttrValue()));
            navView.MenuItems.Add(CreateMenu(i18n.GetString("TitlePerformance"), NavPages.settingsPerformance.GetAttrValue()));
            navView.MenuItems.Add(CreateMenu(i18n.GetString("TitleWallpaper"), NavPages.settingsWallpaper.GetAttrValue()));
            navView.MenuItems.Add(CreateMenu(i18n.GetString("System/Header"), NavPages.settingsSystem.GetAttrValue()));
        }

        //When items change selection not showing, ref: https://github.com/microsoft/microsoft-ui-xaml/issues/7216
        private void ShowMainMenu()
        {
            navView.IsSettingsVisible = true;
            navCommandBar.Visibility = Visibility.Visible;
            navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
            foreach (NavigationViewItem item in navView.MenuItems)
            {
                item.Visibility = !item.Tag.ToString().StartsWith("settings_") ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ShowSettingsMenu()
        {
            navView.IsSettingsVisible = false;
            navCommandBar.Visibility = Visibility.Collapsed;
            navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
            foreach (NavigationViewItem item in navView.MenuItems)
            {
                item.Visibility = item.Tag.ToString().StartsWith("settings_") ? Visibility.Visible : Visibility.Collapsed;

            }
        }

        private void UpdateAudioSliderIcon(double volume) =>
            audioBtn.Icon = audioIcons[(int)Math.Ceiling((audioIcons.Length - 1) * volume / 100)];

        //Actually called before window closed!
        //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/5454
        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            if (userSettings.Settings.IsFirstRun)
            {
                args.Handled = true;
                var dlg = new ContentDialog()
                {
                    Title = i18n.GetString("PleaseWait/Text"),
                    Content = new TrayMenuHelp(),
                    PrimaryButtonText = "4s",
                    IsPrimaryButtonEnabled = false,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot,
                };
                dlg.Opened += async (s, e) =>
                {
                    for (int i = 4; i > 0; i--)
                    {
                        dlg.PrimaryButtonText = $"{i}s";
                        await Task.Delay(1000);
                    }
                    dlg.PrimaryButtonText = i18n.GetString("TextOK");
                    dlg.IsPrimaryButtonEnabled = true;
                };
                await dlg.ShowAsyncQueue();
                userSettings.Settings.IsFirstRun = false;
                userSettings.Save<SettingsModel>();
                this.Close();
            }

            if (userSettings.Settings.IsUpdated)
            {
                args.Handled = true;
                userSettings.Settings.IsUpdated = false;
                userSettings.Save<SettingsModel>();
                this.Close();
            }

            /*
            if (userSettings.Settings.KeepAwakeUI)
            {
                args.Handled = true;
                contentFrame.Visibility = Visibility.Collapsed; //drop resource usage.
                NativeMethods.ShowWindow(this.GetWindowHandleEx(), (uint)NativeMethods.SHOWWINDOW.SW_HIDE);
            }
            */
            if (libraryVm.IsWorking || appUpdateVm.IsUpdateDownloading)
            {
                args.Handled = true;

                //Option 1: Show user prompt with choice to cancel.
                var result = await dialogService.ShowDialogAsync(i18n.GetString("TextConfirmCancel/Text"),
                                                            i18n.GetString("TitleDownloadProgress/Text"),
                                                            i18n.GetString("TextYes"),
                                                            i18n.GetString("TextWait/Text"),
                                                            false);
                if (result == IDialogService.DialogResult.primary)
                {
                    appUpdateVm.CancelDownload();
                    libraryVm.CancelAllDownloads();
                    libraryVm.IsBusy = true;
                    await Task.Delay(1500);
                    this.Close();
                }

                //Option 2: Keep UI client running and close after work completed.
                //contentFrame.Visibility = Visibility.Collapsed; //drop resource usage.
                //NativeMethods.ShowWindow(this.GetWindowHandleEx(), (uint)NativeMethods.SHOWWINDOW.SW_HIDE);
            }
            else
            {
                await commands.SaveRectUIAsync();
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
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            if (args[0].Equals("WM", StringComparison.OrdinalIgnoreCase))
                            {
                                if (args[1].Equals("SHOW", StringComparison.OrdinalIgnoreCase))
                                {
                                    contentFrame.Visibility = Visibility.Visible; //undo drop resource usage.
                                    NativeMethods.ShowWindow(this.GetWindowHandleEx(), (uint)NativeMethods.SHOWWINDOW.SW_SHOW);
                                }
                            }
                            else if (args[0].Equals("LM", StringComparison.OrdinalIgnoreCase))
                            {
                                if (args[1].Equals("SHOWBUSY", StringComparison.OrdinalIgnoreCase))
                                {
                                    libraryVm.IsBusy = true;
                                }
                                else if (args[1].Equals("HIDEBUSY", StringComparison.OrdinalIgnoreCase))
                                {
                                    libraryVm.IsBusy = false;
                                }
                                else if (args[1].Equals("SHOWCUSTOMISEPANEL", StringComparison.OrdinalIgnoreCase))
                                {
                                    _ = dialogService.ShowControlPanelDialogAsync();
                                }
                                else if (args[1].Equals("SHOWAPPUPDATEPAGE", StringComparison.OrdinalIgnoreCase))
                                {
                                    NavViewNavigate(NavPages.appUpdate);
                                }
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        #region titlebar
        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                SetDragRegionForCustomTitleBar(this.AppWindow);
            }
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported()
                && this.AppWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                // Update drag region if the size of the title bar changes.
                SetDragRegionForCustomTitleBar(this.AppWindow);
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                TitleTextBlock.Foreground =
                    (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];
            }
            else
            {
                TitleTextBlock.Foreground =
                    (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
            }
        }

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
        {
            if (AppWindowTitleBar.IsCustomizationSupported()
                && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = GetScaleAdjustment();

                RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
                LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

                List<Windows.Graphics.RectInt32> dragRectsList = new();

                Windows.Graphics.RectInt32 dragRectL;
                dragRectL.X = (int)((LeftPaddingColumn.ActualWidth) * scaleAdjustment);
                dragRectL.Y = 0;
                dragRectL.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                dragRectL.Width = (int)((IconColumn.ActualWidth
                                        + TitleColumn.ActualWidth
                                        + LeftDragColumn.ActualWidth) * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                Windows.Graphics.RectInt32 dragRectR;
                dragRectR.X = (int)((LeftPaddingColumn.ActualWidth
                                    + IconColumn.ActualWidth
                                    + TitleTextBlock.ActualWidth
                                    + LeftDragColumn.ActualWidth
                                    + SearchColumn.ActualWidth) * scaleAdjustment);
                dragRectR.Y = 0;
                dragRectR.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                dragRectR.Width = (int)(RightDragColumn.ActualWidth * scaleAdjustment);
                dragRectsList.Add(dragRectR);

                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }

        //ref: https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.autosuggestbox?view=winrt-22621
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            switch (args.Reason)
            {
                case AutoSuggestionBoxTextChangeReason.UserInput:
                    {
                        if (string.IsNullOrWhiteSpace(SearchBox.Text))
                        {
                            sender.ItemsSource = null;
                            libraryVm.LibraryItemsFiltered.Filter = _ => true;
                        }
                        else
                        {
                            sender.ItemsSource = libraryVm.LibraryItems.Where(x => x.Title.Contains(SearchBox.Text, StringComparison.InvariantCultureIgnoreCase))
                                .Select(x => x.Title)
                                .Distinct();
                        }
                    }
                    break;
                case AutoSuggestionBoxTextChangeReason.ProgrammaticChange:
                case AutoSuggestionBoxTextChangeReason.SuggestionChosen:
                    {
                        Search();
                    }
                    break;
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => Search();

        private void Search()
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                libraryVm.LibraryItemsFiltered.Filter = _ => true;
            }
            else
            {
                libraryVm.LibraryItemsFiltered.Filter = _ => true; //reset
                libraryVm.LibraryItemsFiltered.Filter = x => ((LibraryModel)x).Title.Contains(SearchBox.Text, StringComparison.InvariantCultureIgnoreCase);
            }
            libraryVm.UpdateSelectedWallpaper();
        }

        private void UpdateSuggestBoxState()
        {
            SearchBox.IsEnabled = contentFrame.CurrentSourcePageType == typeof(LibraryView);
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                SearchBox.Text = string.Empty;
        }

        #endregion //titlebar

        #region gallery

        private async void InitializeGallery()
        {
            try
            {
                await galleryClient.InitializeAsync();
            }
            catch (UnauthorizedAccessException ex1)
            {
                Logger.Info($"Skipping login: {ex1?.Message}");
            }
            catch (Exception ex2)
            {
                Logger.Error($"Failed to login: {ex2}");
            }
        }

        private void UpdateAuthState()
        {
            if (galleryClient.IsLoggedIn)
            {
                notAuthorizedBtn.Visibility = Visibility.Collapsed;
                authorizedBtn.Visibility = Visibility.Visible;

                try
                {
                    var img = new BitmapImage(new Uri(galleryClient.CurrentUser.AvatarUrl));
                    avatarBtn.Source = avatarPage.Source = img;
                }
                catch
                {
                    //sad
                }

                nameText.Text = galleryClient.CurrentUser.DisplayName;
            }
            else
            {
                authorizedBtn.Visibility = Visibility.Collapsed;
                notAuthorizedBtn.Visibility = Visibility.Visible;
            }
        }

        private void AuthClick(object sender, RoutedEventArgs e)
        {
            NavViewNavigate(NavPages.gallery);
            notAuthorizedBtn.Flyout.Hide();
        }

        private async void Logout(object sender, RoutedEventArgs e)
        {
            await galleryClient.LogoutAsync();
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            authorizedBtn.Flyout.Hide();
            _ = new ContentDialog()
            {
                Title = "Account",
                Content = new ManageAccountView(),
                PrimaryButtonText = i18n.GetString("TextOK"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot,
            }.ShowAsyncQueue();
        }

        #endregion //gallery

        #region helpers

        public enum NavPages
        {
            [EnumMember(Value = "library")]
            library,
            [EnumMember(Value = "gallery")]
            gallery,
            [EnumMember(Value = "appUpdate")]
            appUpdate,
            [EnumMember(Value = "settings_general")]
            settingsGeneral,
            [EnumMember(Value = "settings_performance")]
            settingsPerformance,
            [EnumMember(Value = "settings_wallpaper")]
            settingsWallpaper,
            [EnumMember(Value = "settings_system")]
            settingsSystem,
        }

        private readonly FontIcon[] audioIcons =
        {
            new FontIcon(){ Glyph = "\uE74F" },
            new FontIcon(){ Glyph = "\uE992" },
            new FontIcon(){ Glyph = "\uE993" },
            new FontIcon(){ Glyph = "\uE994" },
            new FontIcon(){ Glyph = "\uE995" },
        };

        private readonly string[] monitorGlyphs =
        {
            "\uE900",
            "\uE901",
            "\uE902",
            "\uE903",
            "\uE904",
            "\uE905",
        };

        private static NavigationViewItem CreateMenu(string menuName, string tag, string glyph = "", InfoBadge badge = null)
        {
            var item = new NavigationViewItem
            {
                Name = menuName,
                Content = menuName,
                Tag = tag,
                InfoBadge = badge
            };
            if (!string.IsNullOrEmpty(glyph))
            {
                item.Icon = new FontIcon()
                {
                    Glyph = glyph
                };
            }
            return item;
        }

        #endregion //helpers
    }
}
