using Lively.Common.Helpers.Files;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Services;
using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.LivelyProperty;
using Lively.UI.WinUI.Views.Pages.Gallery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinUICommunity;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibraryView : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ILibraryModel selectedTile;

        private readonly ResourceLoader i18n;
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly LibraryViewModel libraryVm;
        private readonly IDialogService dialogService;

        public LibraryView()
        {
            this.desktopCore = App.Services.GetRequiredService<IDesktopCoreClient>();
            this.libraryVm = App.Services.GetRequiredService<LibraryViewModel>();
            this.userSettings = App.Services.GetRequiredService<IUserSettingsClient>();
            this.dialogService = App.Services.GetRequiredService<IDialogService>();

            this.InitializeComponent();
            i18n = ResourceLoader.GetForViewIndependentUse();
            this.DataContext = libraryVm;
        }

        #region library

        private async void contextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTile == null)
                return;

            var s = sender as MenuFlyoutItem;
            var obj = selectedTile;
            switch (s.Name)
            {
                case "previewWallpaper":
                    await desktopCore.PreviewWallpaper(obj.LivelyInfoFolderPath);
                    break;
                case "showOnDisk":
                    await libraryVm.WallpaperShowOnDisk(obj);
                    break;
                case "setWallpaper":
                    await desktopCore.SetWallpaper(obj, userSettings.Settings.SelectedDisplay);
                    break;
                case "exportWallpaper":
                    {
                        _ = await new ContentDialog()
                        {
                            Title = i18n.GetString("TitleShareWallpaper/Text"),
                            Content = new ShareWallpaperView()
                            {
                                DataContext = new ShareWallpaperViewModel(obj),
                            },
                            PrimaryButtonText = i18n.GetString("TextOK"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                    }
                    break;
                case "deleteWallpaper":
                    {
                        var result = await new ContentDialog()
                        {
                            Title = obj.LivelyInfo.IsAbsolutePath ?
                                i18n.GetString("DescriptionDeleteConfirmationLibrary") : i18n.GetString("DescriptionDeleteConfirmation"),
                            Content = new LibraryAboutView() { DataContext = new LibraryAboutViewModel(obj) },
                            PrimaryButtonText = i18n.GetString("TextYes"),
                            SecondaryButtonText = i18n.GetString("TextNo"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                        if (result == ContentDialogResult.Primary)
                        {
                            await libraryVm.WallpaperDelete(obj);
                        }
                    }
                    break;
                case "customiseWallpaper":
                    {
                        _ = await new ContentDialog()
                        {
                            Title = obj.Title.Length > 35 ? obj.Title.Substring(0, 35) + "..." : obj.Title,
                            Content = new LivelyPropertiesView(obj) { MinWidth = 325 },
                            PrimaryButtonText = i18n.GetString("TextOk"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                    }
                    break;
                case "editWallpaper":
                    {
                        obj.DataType = LibraryItemType.edit;
                        libraryVm.LibraryItems.Move(libraryVm.LibraryItems.IndexOf((LibraryModel)obj), 0);
                        await desktopCore.SetWallpaper(obj, userSettings.Settings.SelectedDisplay);
                    }
                    break;
                case "moreInformation":
                    {
                        _ = await new ContentDialog()
                        {
                            Title = i18n.GetString("About/Label"),
                            Content = new LibraryAboutView()
                            {
                                DataContext = new LibraryAboutViewModel(obj),
                            },
                            PrimaryButtonText = i18n.GetString("TextOK"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                    }
                    break;
                case "reportWallpaper":
                    {
                        _ = await new ContentDialog()
                        {
                            Title = i18n.GetString("TitleReportWallpaper/Text"),
                            Content = new ReportWallpaperView()
                            {
                                DataContext = new ReportWallpaperViewModel(obj),
                            },
                            PrimaryButtonText = i18n.GetString("Send/Content"),
                            SecondaryButtonText = i18n.GetString("Cancel/Content"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                    }
                    break;
            }
        }

        private void libraryGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            try
            {
                var a = ((FrameworkElement)e.OriginalSource).DataContext;
                selectedTile = (ILibraryModel)a;
                if (selectedTile.DataType == LibraryItemType.ready)
                {
                    GridView gridView = (GridView)sender;
                    contextMenu.ShowAt(gridView, e.GetPosition(gridView));
                    customiseWallpaper.IsEnabled = selectedTile.LivelyPropertyPath != null;
                }
            }
            catch
            {
                selectedTile = null;
                customiseWallpaper.IsEnabled = false;
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var a = ((FrameworkElement)e.OriginalSource).DataContext;
                selectedTile = (ILibraryModel)a;
                if (selectedTile.DataType == LibraryItemType.ready)
                {
                    customiseWallpaper.IsEnabled = selectedTile.LivelyPropertyPath != null;
                    contextMenu.ShowAt((UIElement)e.OriginalSource, new Point(0, 0));
                }
            }
            catch
            {
                selectedTile = null;
                customiseWallpaper.IsEnabled = false;
            }
        }

        #endregion //library

        #region file drop

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            this.AddFilePanel.Visibility = Visibility.Collapsed;

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                var uri = await e.DataView.GetWebLinkAsync();
                Logger.Info($"Dropped string {uri}");
                try
                {
                    var libItem = libraryVm.AddWallpaperLink(uri);
                    if (libItem.LivelyInfo.IsAbsolutePath)
                    {
                        libItem.DataType = LibraryItemType.processing;
                        await desktopCore.SetWallpaper(libItem, userSettings.Settings.SelectedDisplay);
                        /*
                        var inputVm = new AddWallpaperDataViewModel(libItem);
                        var inputDialog = new ContentDialog()
                        {
                            Title = i18n.GetString("AddWallpaper/Label"),
                            Content = new AddWallpaperDataView(inputVm),
                            PrimaryButtonText = i18n.GetString("TextOk"),
                            SecondaryButtonText = i18n.GetString("Cancel/Content"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                            SecondaryButtonCommand = inputVm.CancelCommand,
                            PrimaryButtonCommand = inputVm.ProceedCommand,
                        };
                        await inputDialog.ShowAsyncQueue();
                        */
                    }
                }
                catch (Exception ie)
                {
                    await new ContentDialog()
                    {
                        Title = i18n.GetString("TextError"),
                        Content = ie.Message,
                        PrimaryButtonText = i18n.GetString("TextOk"),
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.Content.XamlRoot,
                    }.ShowAsyncQueue();
                }
            }
            else if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count == 1)
                {
                    var item = items[0].Path;
                    Logger.Info($"Dropped file {item}");
                    try
                    {
                        if (string.IsNullOrWhiteSpace(Path.GetExtension(item)))
                            return;
                    }
                    catch (ArgumentException)
                    {
                        Logger.Info($"Invalid character, skipping dropped file {item}");
                        return;
                    }

                    try
                    {
                        var creationType = await dialogService.ShowWallpaperCreateDialog(item);
                        if (creationType is null)
                            return;

                        switch (creationType)
                        {
                            case WallpaperCreateType.none:
                                {
                                    var result = await libraryVm.AddWallpaperFile(item);
                                    if (result.DataType == LibraryItemType.processing)
                                        await desktopCore.SetWallpaper(result, userSettings.Settings.SelectedDisplay);
                                }
                                break;
                            case WallpaperCreateType.depthmap:
                                {
                                    var result = await dialogService.ShowDepthWallpaperDialog(item);
                                    if (result is not null)
                                        await desktopCore.SetWallpaper(result, userSettings.Settings.SelectedDisplay);
                                }
                                break;
                        }
                    }
                    catch (Exception ie)
                    {
                        await new ContentDialog()
                        {
                            Title = i18n.GetString("TextError"),
                            Content = ie.Message,
                            PrimaryButtonText = i18n.GetString("TextOk"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                    }
                }
                else if (items.Count > 1)
                {
                    await App.Services.GetRequiredService<MainWindow>().AddWallpapers(items.Select(x => x.Path).ToList());
                }
            }
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.IsCaptionVisible = false;
                e.DragUIOverride.IsContentVisible = true;
            }
            this.AddFilePanel.Visibility = Visibility.Visible;
        }

        private void Page_DragLeave(object sender, DragEventArgs e)
        {
            this.AddFilePanel.Visibility = Visibility.Collapsed;
        }

        #endregion //file drop
    }
}
