using Lively.Common;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.Wpf.Helpers.MVVM.Dialogs;
using Lively.UI.Wpf.ViewModels;
using Lively.UI.Wpf.Views.Dialogues;
using Lively.UI.Wpf.Views.LivelyProperty;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Lively.UI.Wpf.Helpers;

namespace Lively.UI.Wpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : System.Windows.Controls.Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ILibraryModel selectedTile;

        private readonly LibraryViewModel libraryVm;
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly LibraryUtil libraryUtil;
        //private readonly MainWindow appWindow;

        public LibraryView()
        {
            libraryVm = App.Services.GetRequiredService<LibraryViewModel>();
            userSettings = App.Services.GetRequiredService<IUserSettingsClient>();
            desktopCore = App.Services.GetRequiredService<IDesktopCoreClient>();
            libraryUtil = App.Services.GetRequiredService<LibraryUtil>();
            //appWindow = App.Services.GetRequiredService<MainWindow>();

            InitializeComponent();
            this.DataContext = libraryVm;
        }

        private async void LivelyGridControl_ContextMenuClick(object sender, ILibraryModel obj)
        {
            var s = sender as MenuItem;
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
                        string savePath = string.Empty;
                        var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                        {
                            Title = "Select location to save the file",
                            Filter = "Lively/zip file|*.zip",
                            //title ending with '.' can have diff extension (example: parallax.js)
                            FileName = Path.ChangeExtension(obj.Title, ".zip"),
                        };
                        if (saveFileDialog1.ShowDialog() == true)
                        {
                            savePath = saveFileDialog1.FileName;
                        }
                        if (string.IsNullOrEmpty(savePath))
                        {
                            break;
                        }

                        try
                        {
                            await libraryUtil.WallpaperExport(obj, savePath);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
                        }
                    }
                    break;
                case "deleteWallpaper":
                    {
                        var deleteView = new LibraryAboutView
                        {
                            DataContext = obj
                        };
                        var deleteFrame = new ModernWpf.Controls.Frame();
                        deleteFrame.Navigate(deleteView);
                        var result = await Dialogs.ShowConfirmationDialog(
                            (obj.LivelyInfo.IsAbsolutePath ?
                                Properties.Resources.DescriptionDeleteConfirmationLibrary : Properties.Resources.DescriptionDeleteConfirmation),
                            deleteFrame,
                            Properties.Resources.TextYes,
                            Properties.Resources.TextNo);
                        if (result == ContentDialogResult.Primary)
                        {
                            await libraryUtil.WallpaperDelete(obj);
                        }
                    }
                    break;
                case "customiseWallpaper":
                    {
                        var customiseFrame = new ModernWpf.Controls.Frame()
                        {
                            Margin = new Thickness(25, 0, 25, 25)
                        };
                        customiseFrame.Navigate(new LivelyPropertiesView(obj));
                        ScrollViewer scv = new ScrollViewer()
                        {
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Margin = new Thickness(0, 5, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        scv.Content = customiseFrame;
                        await Dialogs.ShowConfirmationDialog(
                            obj.Title.Length > 35 ? obj.Title.Substring(0, 35) + "..." : obj.Title,
                            scv,
                            Properties.Resources.TextClose);
                    }
                    break;
                case "editWallpaper":
                    //TODO
                    break;
                case "moreInformation":
                    {
                        var aboutView = new LibraryAboutView
                        {
                            DataContext = obj
                        };
                        var aboutFrame = new ModernWpf.Controls.Frame();
                        aboutFrame.Navigate(aboutView);
                        await Dialogs.ShowConfirmationDialog(
                            Properties.Resources.TitleAbout,
                            aboutFrame,
                            Properties.Resources.TextOK);
                    }
                    break;
            }
        }

        private void GridControl_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            try
            {
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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTile != null)
            {
                LivelyGridControl_ContextMenuClick(sender, selectedTile);
            }
        }

        private void GridControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //keyboard disable.
            e.Handled = true;
        }
    }
}
