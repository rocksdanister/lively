using livelywpf.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : System.Windows.Controls.Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        livelygrid.LivelyGridView LivelyGridControl { get; set; }

        public LibraryView()
        {
            InitializeComponent();
            //uwp control also gets binded..
            this.DataContext = Program.LibraryVM; 
        }

        private void LivelyGridView_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            LivelyGridControl = windowsXamlHost.GetUwpInternalObject() as global::livelygrid.LivelyGridView;

            if (LivelyGridControl != null)
            {
                //Don't know if there is an easier way to chang UserControl language, tried setting framework language to no effect.
                //todo: find better way to do this.
                LivelyGridControl.UIText = new livelygrid.LocalizeTextGridView()
                {
                    TextAddWallpaper = Properties.Resources.TitleAddWallpaper,
                    TextConvertVideo = Properties.Resources.TextConvertVideo,
                    TextCustomise = Properties.Resources.TextCustomise,
                    TextDelete = Properties.Resources.TextDeleteWallpaper,
                    TextExportZip = Properties.Resources.TextExportWallpaperZip,
                    TextInformation = Properties.Resources.TitleAbout,
                    TextSetWallpaper = Properties.Resources.TextSetWallpaper,
                    TextShowDisk = Properties.Resources.TextShowOnDisk,
                    TextPreviewWallpaper = Properties.Resources.TextPreviewWallpaper,
                    TextEditWallpaper = Properties.Resources.TextEditWallpaper,
                };
                LivelyGridControl.GridElementSize((livelygrid.GridSize)Program.SettingsVM.SelectedTileSizeIndex);
                LivelyGridControl.ContextMenuClick += LivelyGridControl_ContextMenuClick;
                LivelyGridControl.FileDroppedEvent += LivelyGridControl_FileDroppedEvent;
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
        private async void LivelyGridControl_ContextMenuClick(object sender, object e)
        {
            var s = sender as MenuFlyoutItem;
            LibraryModel obj;
            try
            {
                obj = (LibraryModel)e;
            }
            catch { return; }

            await this.Dispatcher.InvokeAsync(new Action(async () => {
                switch (s.Name)
                {
                    case "previewWallpaper":
                        var prev = new WallpaperPreviewWindow((LibraryModel)e)
                        {
                            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                            Owner = App.AppWindow
                        };
                        prev.Show();
                        break;
                    case "showOnDisk":
                        Program.LibraryVM.WallpaperShowOnDisk(e);
                        break;
                    case "setWallpaper":
                        SetupDesktop.SetWallpaper((LibraryModel)e, Program.SettingsVM.Settings.SelectedDisplay);
                        break;
                    case "exportWallpaper":
                        string savePath = "";
                        var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                        {
                            Title = "Select location to save the file",
                            Filter = "Lively/zip file|*.zip",
                            //title ending with '.' can have diff extension (example: parallax.js)
                            FileName = Path.ChangeExtension(((LibraryModel)e).Title, ".zip"),
                        };
                        if (saveFileDialog1.ShowDialog() == true)
                        {
                            savePath = saveFileDialog1.FileName;
                        }
                        if (String.IsNullOrEmpty(savePath))
                        {
                            break;
                        }
                        Program.LibraryVM.WallpaperExport(e, savePath);
                        break;
                    case "deleteWallpaper":
                        var deleteView = new livelygrid.InfoPage
                        {
                            DataContext = ((LibraryModel)e),
                            UIText = new livelygrid.LocalizeTextInfoPage()
                            {
                                Author = Properties.Resources.TextAuthor,
                                Website = Properties.Resources.TextWebsite,
                                Type = Properties.Resources.TextWallpaperType,
                            },
                        };
                        var result = await Helpers.DialogService.ShowConfirmationDialog(
                            ((LibraryModel)e).LivelyInfo.IsAbsolutePath ?
                                Properties.Resources.DescriptionDeleteConfirmationLibrary : Properties.Resources.DescriptionDeleteConfirmation,
                            deleteView,
                            ((UIElement)sender).XamlRoot,
                            Properties.Resources.TextYes,
                            Properties.Resources.TextNo);
                        if (result == ContentDialogResult.Primary)
                        {
                            Program.LibraryVM.WallpaperDelete(e);
                        }
                        break;
                    case "customiseWallpaper":
                        //In app customise dialogue; 
                        //Can't use contentdialogue since the window object is not uwp.
                        //modernwpf contentdialogue does not have xamlroot so can't draw over livelygrid.
                        LivelyGridControl?.DimBackground(true);
                        var overlay =
                            new Cef.LivelyPropertiesWindow(obj)
                            {
                                Owner = App.AppWindow,
                                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                                Width = 350,//App.AppWindow.Width / 3.0,
                                Height = App.AppWindow.Height / 1.2,
                                Title = obj.Title.Length > 40 ? obj.Title.Substring(0, 40) + "..." : obj.Title
                            };
                        overlay.ShowDialog();
                        LivelyGridControl?.DimBackground(false);
                        break;
                    case "convertVideo":
                        Program.LibraryVM.WallpaperVideoConvert(obj);
                        break;
                    case "editWallpaper":
                        Program.LibraryVM.EditWallpaper(obj);
                        break;
                    case "moreInformation":
                        var infoView = new livelygrid.InfoPage
                        {
                            DataContext = ((LibraryModel)e),
                            UIText = new livelygrid.LocalizeTextInfoPage()
                            {
                                Author = Properties.Resources.TextAuthor,
                                Website = Properties.Resources.TextWebsite,
                                Type = Properties.Resources.TextWallpaperType,
                            },
                        };
                        await Helpers.DialogService.ShowConfirmationDialog(
                            Properties.Resources.TitleAbout,
                            infoView,
                            ((UIElement)sender).XamlRoot,
                            Properties.Resources.TextOK);
                        break;
                }
            }));
        }

        private async void LivelyGridControl_FileDroppedEvent(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(new Action(async () => {
                if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await e.DataView.GetWebLinkAsync();
                    Logger.Info("Dropped url=>" + uri.ToString());
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
                else if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count == 1)
                    {
                        var item = items[0].Path;
                        Logger.Info("Dropped file=>" + item);
                        try
                        {
                            if (String.IsNullOrWhiteSpace(Path.GetExtension(item)))
                                return;
                        }
                        catch (ArgumentException)
                        {
                            Logger.Info("Invalid character, skipping dropped file=>" + item);
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
                                         ((UIElement)sender).XamlRoot,
                                         Properties.Resources.TextYes,
                                         Properties.Resources.TextNo);

                                    if (result == ContentDialogResult.Primary)
                                    {
                                        //xaml island textbox input issue..
                                        //ref: https://github.com/microsoft/microsoft-ui-xaml/issues/3804
                                        //var cmdArgs = await Helpers.DialogService.ShowTextInputDialog(
                                        //    "Command line arguments",
                                        //    ((UIElement)sender).XamlRoot,
                                        //    Properties.Resources.TextOK);

                                        var w = new TextInputDialog(Properties.Resources.TextWallpaperCommandlineArgs, 
                                            Properties.Resources.TitlePleaseWait,
                                            Properties.Resources.TextOK,
                                            Properties.Resources.TextCancel)
                                        { 
                                            Owner = App.AppWindow,
                                            Width = 350,
                                            Height = 200
                                        };
                                        if (w.ShowDialog() == true)
                                        {
                                            Program.LibraryVM.AddWallpaper(item,
                                                WallpaperType.app,
                                                LibraryTileType.processing,
                                                Program.SettingsVM.Settings.SelectedDisplay,
                                                string.IsNullOrWhiteSpace(w.Result) ? null : w.Result);
                                        }
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
                                        await Helpers.DialogService.ShowConfirmationDialog(
                                          Properties.Resources.TextError,
                                          Properties.Resources.LivelyExceptionNotLivelyZip,
                                          ((UIElement)sender).XamlRoot,
                                          Properties.Resources.TextClose);
                                    }
                                }
                                break;
                            case (WallpaperType)(-1):
                                {
                                    await Helpers.DialogService.ShowConfirmationDialog(
                                        Properties.Resources.TextError,
                                        Properties.Resources.TextUnsupportedFile + " (" + Path.GetExtension(item) + ")",
                                        ((UIElement)sender).XamlRoot,
                                        Properties.Resources.TextClose);
                                }
                                break;
                            default:
                                Logger.Info("No wallpaper type recognised.");
                                break;
                        }
                    }
                    else if (items.Count > 1)
                    {
                        var miw = new MultiWallpaperImport(items.Select(x => x.Path).ToList())
                        {
                            //This dialog on right-topmost like position and librarypreview window left-topmost.
                            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                            Left = App.AppWindow.Left + App.AppWindow.Width - (App.AppWindow.Width/1.5),
                            Top = App.AppWindow.Top + (App.AppWindow.Height/15),
                            Owner = App.AppWindow,
                            Width = App.AppWindow.Width/1.5,
                            Height = App.AppWindow.Height/1.3,
                        };
                        miw.ShowDialog();
                    }
                }
            }));
        }

        private void Page_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (LivelyGridControl != null)
            {
                LivelyGridControl.ContextMenuClick -= LivelyGridControl_ContextMenuClick;
                LivelyGridControl.FileDroppedEvent -= LivelyGridControl_FileDroppedEvent;
                //stop rendering previews... this should be automatic(?), but its not for some reason.
                LivelyGridControl.GridElementSize(livelygrid.GridSize.NoPreview);
            }
        }
    }
}
