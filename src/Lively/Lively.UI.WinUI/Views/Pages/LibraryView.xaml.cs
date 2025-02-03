using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.UI.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Lively.UI.WinUI.Views.Pages
{
    public sealed partial class LibraryView : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private LibraryModel selectedTile;

        private readonly IResourceService i18n;
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly LibraryViewModel libraryVm;
        private readonly IDialogService dialogService;
        private readonly IDisplayManagerClient displayManager;

        public LibraryView()
        {
            this.desktopCore = App.Services.GetRequiredService<IDesktopCoreClient>();
            this.libraryVm = App.Services.GetRequiredService<LibraryViewModel>();
            this.userSettings = App.Services.GetRequiredService<IUserSettingsClient>();
            this.dialogService = App.Services.GetRequiredService<IDialogService>();
            this.displayManager = App.Services.GetRequiredService<IDisplayManagerClient>();
            this.i18n = App.Services.GetRequiredService<IResourceService>();

            this.InitializeComponent();
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
                    DisplayMonitor monitor;
                    if (userSettings.Settings.RememberSelectedScreen)
                        monitor = userSettings.Settings.SelectedDisplay;
                    else
                        monitor = displayManager.DisplayMonitors.Count == 1 || userSettings.Settings.WallpaperArrangement != WallpaperArrangement.per ?
                           displayManager.DisplayMonitors.FirstOrDefault(x => x.IsPrimary) : await dialogService.ShowDisplayChooseDialogAsync();
                    if (monitor is null)
                        return;

                    await desktopCore.SetWallpaper(obj, monitor);
                    break;
                case "exportWallpaper":
                    await dialogService.ShowShareWallpaperDialogAsync(obj);
                    break;
                case "deleteWallpaper":
                    if (await dialogService.ShowDeleteWallpaperDialogAsync(obj))
                        await libraryVm.WallpaperDelete(obj);
                    break;
                case "customiseWallpaper":
                    await dialogService.ShowCustomiseWallpaperDialogAsync(obj);
                    break;
                case "editWallpaper":
                    obj.DataType = LibraryItemType.edit;
                    libraryVm.LibraryItems.Move(libraryVm.LibraryItems.IndexOf(obj), 0);
                    await desktopCore.SetWallpaper(obj, userSettings.Settings.SelectedDisplay);
                    break;
                case "moreInformation":
                    await dialogService.ShowAboutWallpaperDialogAsync(obj);
                    break;
                case "reportWallpaper":
                    await dialogService.ShowReportWallpaperDialogAsync(obj);
                    break;
            }
        }

        private void GridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            try
            {
                var a = ((FrameworkElement)e.OriginalSource).DataContext;
                selectedTile = (LibraryModel)a;
                if (selectedTile.DataType == LibraryItemType.ready)
                {
                    var item = sender as GridView;
                    contextMenu.ShowAt(item, e.GetPosition(item));
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
                selectedTile = (LibraryModel)a;
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

                        //var inputVm = App.Services.GetRequiredService<AddWallpaperDataViewModel>();
                        //inputVm.Model = libItem;
                        //await dialogService.ShowDialogAsync(new AddWallpaperDataView(inputVm),
                        //    i18n.GetString("AddWallpaper/Label"),
                        //    i18n.GetString("TextOk"),
                        //    i18n.GetString("Cancel/Content"));
                    }
                }
                catch (Exception ie)
                {
                    await dialogService.ShowDialogAsync(ie.Message,
                        i18n.GetString("TextError"),
                        i18n.GetString("TextOk"));
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
                        var creationType = await dialogService.ShowWallpaperCreateDialogAsync(item);
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
                                    var result = await dialogService.ShowDepthWallpaperDialogAsync(item);
                                    if (result is not null)
                                        await desktopCore.SetWallpaper(result, userSettings.Settings.SelectedDisplay);
                                }
                                break;
                        }
                    }
                    catch (Exception ie)
                    {
                        await dialogService.ShowDialogAsync(ie.Message,
                            i18n.GetString("TextError"),
                            i18n.GetString("TextOk"));
                    }
                }
                else if (items.Count > 1)
                {
                    await App.Services.GetRequiredService<MainViewModel>().AddWallpapers(items.Select(x => x.Path).ToList());
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
