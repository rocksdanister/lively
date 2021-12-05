using livelywpf.Helpers.Archive;
using livelywpf.Helpers.Files;
using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.NetStream;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using livelywpf.Models;
using livelywpf.Core;
using livelywpf.Views.Dialogues;
using Microsoft.Extensions.DependencyInjection;
using livelywpf.ViewModels;
using livelywpf.Services;
using System.Windows;
using System.Windows.Controls;
using ModernWpf.Controls;
using livelywpf.Views.LivelyProperty;

namespace livelywpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : System.Windows.Controls.Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private LibraryModel selectedTile;

        private readonly LibraryViewModel libraryVm;
        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly MainWindow appWindow;

        public LibraryView()
        {
            libraryVm = App.Services.GetRequiredService<LibraryViewModel>();
            userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            desktopCore = App.Services.GetRequiredService<IDesktopCore>();
            appWindow = App.Services.GetRequiredService<MainWindow>();

            InitializeComponent();
            //uwp control also gets binded..
            this.DataContext = libraryVm;
        }

        private void GridControl_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            try
            {
                var a = ((FrameworkElement)e.OriginalSource).DataContext;
                selectedTile = (LibraryModel)a;
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

        /// <summary>
        /// Not possible to do direct mvvm currently, putting the contextmenu inside datatemplate works but.. 
        /// the menu is opening only when right clicking on the DataTemplate content which is not covering completely the GridViewItem.
        /// So the workaround I did is set it outside of template and the datacontext is calculated in code behind.
        /// ref: https://github.com/microsoft/microsoft-ui-xaml/issues/911
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LivelyGridControl_ContextMenuClick(object sender, LibraryModel obj)
        {
            var s = sender as MenuItem;
            switch (s.Name)
            {
                case "previewWallpaper":
                    var prev = new WallpaperPreviewWindow(obj)
                    {
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                        Owner = appWindow
                    };
                    prev.Show();
                    break;
                case "showOnDisk":
                    libraryVm.WallpaperShowOnDisk(obj);
                    break;
                case "setWallpaper":
                    desktopCore.SetWallpaper(obj, userSettings.Settings.SelectedDisplay);
                    break;
                case "exportWallpaper":
                    string savePath = "";
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
                    if (String.IsNullOrEmpty(savePath))
                    {
                        break;
                    }
                    libraryVm.WallpaperExport(obj, savePath);
                    break;
                case "deleteWallpaper":
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
                        libraryVm.WallpaperDelete(obj);
                    }
                    break;
                case "customiseWallpaper":
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
                    break;
                case "convertVideo":
                    libraryVm.WallpaperVideoConvert(obj);
                    break;
                case "editWallpaper":
                    libraryVm.EditWallpaper(obj);
                    break;
                case "moreInformation":
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
                    break;
            }
        }

        private void Page_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
  
        }

        private void GridControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //keyboard disable.
            e.Handled = true;
        }
    }
}
