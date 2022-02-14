using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.LivelyProperty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SettingsUI.Extensions;
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
        private readonly LibraryUtil libraryUtil;

        public LibraryView()
        {
            this.desktopCore = App.Services.GetRequiredService<IDesktopCoreClient>();
            this.libraryVm = App.Services.GetRequiredService<LibraryViewModel>();
            this.userSettings = App.Services.GetRequiredService<IUserSettingsClient>();
            this.libraryUtil = App.Services.GetRequiredService<LibraryUtil>();

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
                    libraryUtil.WallpaperShowOnDisk(obj);
                    break;
                case "setWallpaper":
                    await desktopCore.SetWallpaper(obj, userSettings.Settings.SelectedDisplay);
                    break;
                case "exportWallpaper":
                    {
                        var filePicker = new FileSavePicker();
                        filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
                        filePicker.FileTypeChoices.Add("Compressed archive", new List<string>() { ".zip" });
                        filePicker.SuggestedFileName = obj.Title;
                        var file = await filePicker.PickSaveFileAsync();
                        if (file != null)
                        {
                            try
                            {
                                await libraryUtil.WallpaperExport(obj, file.Path);
                            }
                            catch (Exception)
                            {
                                //TODO
                            }
                        }
                    }
                    break;
                case "deleteWallpaper":
                    {
                        var result = await new ContentDialog()
                        {
                            Title = obj.LivelyInfo.IsAbsolutePath ?
                                i18n.GetString("DescriptionDeleteConfirmationLibrary") : i18n.GetString("DescriptionDeleteConfirmation"),
                            Content = new LibraryAboutView() { DataContext = obj },
                            PrimaryButtonText = i18n.GetString("TextYes"),
                            SecondaryButtonText = i18n.GetString("TextNo"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                        if (result == ContentDialogResult.Primary)
                        {
                            await libraryUtil.WallpaperDelete(obj);
                        }
                    }
                    break;
                case "customiseWallpaper":
                    {
                        _ = await new ContentDialog()
                        {
                            Title = obj.Title.Length > 35 ? obj.Title.Substring(0, 35) + "..." : obj.Title,
                            Content = new LivelyPropertiesView(obj),
                            PrimaryButtonText = i18n.GetString("TextClose"),
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot,
                        }.ShowAsyncQueue();
                    }
                    break;
                case "editWallpaper":
                    {
                        //TODO
                    }
                    break;
                case "moreInformation":
                    {
                        _ = await new ContentDialog()
                        {
                            Title = i18n.GetString("About/Label"),
                            Content = new LibraryAboutView()
                            {
                                DataContext = obj,
                            },
                            PrimaryButtonText = i18n.GetString("TextOK"),
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
                GridView gridView = (GridView)sender;
                contextMenu.ShowAt(gridView, e.GetPosition(gridView));
                var a = ((FrameworkElement)e.OriginalSource).DataContext;
                selectedTile = (ILibraryModel)a;
                customiseWallpaper.IsEnabled = selectedTile.LivelyPropertyPath != null;
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
                customiseWallpaper.IsEnabled = selectedTile.LivelyPropertyPath != null;
                contextMenu.ShowAt((UIElement)e.OriginalSource, new Point(0, 0));
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
                    var libItem = libraryUtil.AddWallpaperLink(uri);
                    if (libItem.LivelyInfo.IsAbsolutePath)
                    {
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
                        var libItem = await libraryUtil.AddWallpaperFile(item);
                        if (libItem.LivelyInfo.IsAbsolutePath)
                        {
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
                if (items.Count > 1)
                {
                    //TODO
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
