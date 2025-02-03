using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.UI.Shared.ViewModels;
using Lively.UI.WinUI.Extensions;
using Lively.UI.WinUI.Views.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;
using WinUIEx;

namespace Lively.UI.WinUI
{
    public sealed partial class MainWindow : WindowEx
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IDesktopCoreClient desktopCore;
        private readonly IUserSettingsClient userSettings;
        private readonly LibraryViewModel libraryVm;
        private readonly AppUpdateViewModel appUpdateVm;
        private readonly IDialogService dialogService;
        private readonly ICommandsClient commands;
        private readonly INavigator navigator;
        private readonly IResourceService i18n;

        public MainWindow(IDesktopCoreClient desktopCore,
            MainViewModel mainVm,
            IDialogService dialogService,
            ICommandsClient commands,
            IUserSettingsClient userSettings,
            LibraryViewModel libraryVm,
            AppUpdateViewModel appUpdateVm,
            IResourceService i18n,
            INavigator navigator)
        {
            this.desktopCore = desktopCore;
            this.libraryVm = libraryVm;
            this.userSettings = userSettings;
            this.dialogService = dialogService;
            this.commands = commands;
            this.appUpdateVm = appUpdateVm;
            this.navigator = navigator;
            this.i18n = i18n;

            this.InitializeComponent();
            Root.DataContext = mainVm;
            navigator.RootFrame = Root;
            navigator.Frame = contentFrame;
            this.SystemBackdrop = new MicaBackdrop();

            // Ref: https://learn.microsoft.com/en-us/windows/apps/develop/title-bar?tabs=wasdk
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

            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            _ = StdInListener();
        }

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            _ = this.DispatcherQueue.TryEnqueue(() =>
            {
                // If its duplicate mode fire the animation more than once.
                if (userSettings.Settings.WallpaperArrangement != WallpaperArrangement.duplicate || desktopCore.Wallpapers.Count < 2)
                    activeWallpaperOffsetAnimation.Start();
            });
        }

        // This is actually WindowClosing, called before window closed.
        // Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/5454
        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            if (userSettings.Settings.IsFirstRun)
            {
                args.Handled = true;

                await dialogService.ShowWaitDialogAsync(new TrayMenuHelp(), 4);
                userSettings.Settings.IsFirstRun = false;
                userSettings.Save<SettingsModel>();

                this.Close();
            }
            else if (userSettings.Settings.IsUpdatedNotify)
            {
                args.Handled = true;

                userSettings.Settings.IsUpdatedNotify = false;
                userSettings.Save<SettingsModel>();

                this.Close();
            }
            else if (libraryVm.IsWorking || appUpdateVm.IsUpdateDownloading)
            {
                args.Handled = true;

                //Option 1: Show user prompt with choice to cancel.
                var result = await dialogService.ShowDialogAsync(i18n.GetString("TextConfirmCancel/Text"),
                                                            i18n.GetString("TitleDownloadProgress/Text"),
                                                            i18n.GetString("TextYes"),
                                                            i18n.GetString("TextWait/Text"),
                                                            false);
                if (result == DialogResult.primary)
                {
                    appUpdateVm.CancelCommand.Execute(null);
                    libraryVm.CancelAllDownloads();
                    libraryVm.IsBusy = true;

                    await Task.Delay(1500);
                    this.Close();
                }

                //Option 2: Keep UI client running and close after work completed.
                //contentFrame.Visibility = Visibility.Collapsed; //drop resource usage.
                //NativeMethods.ShowWindow(this.GetWindowHandleEx(), (uint)NativeMethods.SHOWWINDOW.SW_HIDE);
            }
            else if (dialogService.IsWorking)
            {
                // Wait for user to close the dialog and try again manually.
                args.Handled = true;
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
                                    navigator.NavigateTo(ContentPageType.appupdate);
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

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetDragRegionForCustomTitleBar(RightPaddingColumn,
                LeftPaddingColumn,
                IconColumn,
                TitleColumn,
                LeftDragColumn,
                RightDragColumn,
                SearchColumn,
                TitleTextBlock,
                AppTitleBar);
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.AppWindow.TitleBar.ExtendsContentIntoTitleBar)
                this.SetDragRegionForCustomTitleBar(RightPaddingColumn,
                         LeftPaddingColumn,
                         IconColumn,
                         TitleColumn,
                         LeftDragColumn,
                         RightDragColumn,
                         SearchColumn,
                         TitleTextBlock,
                         AppTitleBar);
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            TitleTextBlock.Foreground = args.WindowActivationState == WindowActivationState.Deactivated ? 
                (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"] : (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
        }

        // Ref: https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.autosuggestbox?view=winrt-22621
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
    }
}
