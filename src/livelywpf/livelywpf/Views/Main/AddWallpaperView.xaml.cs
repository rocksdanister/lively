using livelywpf.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for AddWallpaperView.xaml
    /// </summary>
    public partial class AddWallpaperView : Page
    {
        private readonly string fileDialogFilter;
        public AddWallpaperView()
        {
            InitializeComponent();
            UrlText.Text = Program.SettingsVM.Settings.SavedURL;
            fileDialogFilter = FileFilter.GetLivelySupportedFileDialogFilter(true);
        }

        private async void FileBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = Properties.Resources.TitleAddWallpaper,
                CheckFileExists = true,
                CheckPathExists = true,       
            };
            openFileDlg.Filter = fileDialogFilter;
            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true)
            {
                if(openFileDlg.FilterIndex == 1)
                {
                    //Any filetype.
                    WallpaperType type;
                    if ((type = FileFilter.GetLivelyFileType(openFileDlg.FileName)) != (WallpaperType)(-1))
                    {
                        if (type == (WallpaperType)100)
                        {
                            //lively .zip is not a wallpaper type.
                            if (ZipExtract.CheckLivelyZip(openFileDlg.FileName))
                            {
                                Program.LibraryVM.WallpaperInstall(openFileDlg.FileName, false);
                            }
                            else
                            {
                                await Helpers.DialogService.ShowConfirmationDialog(
                                    Properties.Resources.TextError,
                                    Properties.Resources.LivelyExceptionNotLivelyZip,
                                    Properties.Resources.TextOK);
                                return;
                            }
                        }
                        else
                        {
                            Program.LibraryVM.AddWallpaper(openFileDlg.FileName,
                                type,
                                LibraryTileType.processing,
                                Program.SettingsVM.Settings.SelectedDisplay);
                        }
                    }
                    else
                    {
                        await Helpers.DialogService.ShowConfirmationDialog(
                            Properties.Resources.TextError,
                            Properties.Resources.TextUnsupportedFile + " (" + Path.GetExtension(openFileDlg.FileName) + ")",
                            Properties.Resources.TextOK);
                        return;
                    }
                }
                else
                {
                    if(FileFilter.LivelySupportedFormats[openFileDlg.FilterIndex - 2].Type == (WallpaperType)100)
                    {
                        if(ZipExtract.CheckLivelyZip(openFileDlg.FileName))
                        {
                            Program.LibraryVM.WallpaperInstall(openFileDlg.FileName, false);
                        }
                        else
                        {
                            await Helpers.DialogService.ShowConfirmationDialog(
                                Properties.Resources.TextError,
                                Properties.Resources.LivelyExceptionNotLivelyZip,
                                Properties.Resources.TextOK);
                            return;
                        }
                    }
                    else
                    {
                        Program.LibraryVM.AddWallpaper(openFileDlg.FileName,
                            FileFilter.LivelySupportedFormats[openFileDlg.FilterIndex - 2].Type,
                            LibraryTileType.processing,
                            Program.SettingsVM.Settings.SelectedDisplay);
                    }
                }

                App.AppWindow.NavViewNavigate("library");
            }
        }

        private void UrlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlText.Text))
            {
                return;
            }

            Uri uri;
            try
            {
                uri = new Uri(UrlText.Text);
            }
            catch (UriFormatException)
            {
                try
                {
                    //if user did not input https/http assume https connection.
                    uri = new UriBuilder(UrlText.Text)
                    {
                        Scheme = "https",
                        Port = -1,
                    }.Uri;
                    UrlText.Text = uri.ToString();
                }
                catch
                {
                    return;
                }
            }

            if (Program.SettingsVM.Settings.AutoDetectOnlineStreams &&
                 StreamHelper.IsSupportedUri(uri))
            {
                Program.LibraryVM.AddWallpaper(uri.ToString(),
                    WallpaperType.videostream,
                    LibraryTileType.processing,
                    Program.SettingsVM.Settings.SelectedDisplay);
            }
            else
            {
                Program.LibraryVM.AddWallpaper(uri.ToString(),
                    WallpaperType.url,
                    LibraryTileType.processing,
                    Program.SettingsVM.Settings.SelectedDisplay);
            }

            Program.SettingsVM.Settings.SavedURL = UrlText.Text;
            Program.SettingsVM.UpdateConfigFile();

            App.AppWindow.NavViewNavigate("library");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Helpers.LinkHandler.OpenBrowser(e.Uri);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
