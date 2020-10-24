using livelywpf.Helpers;
using Microsoft.Toolkit.Wpf.UI.XamlHost;
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

        private void FileBtn_Click(object sender, RoutedEventArgs e)
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
                                System.Windows.MessageBox.Show(
                                   Properties.Resources.LivelyExceptionNotLivelyZip,
                                   Properties.Resources.TextError);
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
                        System.Windows.MessageBox.Show(
                            Properties.Resources.TextUnsupportedFile + " (" + Path.GetExtension(openFileDlg.FileName) + ")",
                            Properties.Resources.TextError);
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
                            System.Windows.MessageBox.Show(
                               Properties.Resources.LivelyExceptionNotLivelyZip,
                               Properties.Resources.TextError);
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

                //fix, xalmhost element takes time to disappear.
                PreviewGif.Visibility = Visibility.Collapsed;
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
                 Program.SettingsVM.Settings.StreamVideoPlayer == LivelyMediaPlayer.libmpvExt ?
                 libMPVStreams.CheckStream(uri) : libVLCStreams.CheckStream(uri))
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

            //fix, xalmhost element takes time to disappear.
            PreviewGif.Visibility = Visibility.Collapsed;
            App.AppWindow.NavViewNavigate("library");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                var ps = new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch { }
            e.Handled = true;
        }

        Windows.UI.Xaml.Controls.Image img;
        private void PreviewGif_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;
            img = (Windows.UI.Xaml.Controls.Image)windowsXamlHost.Child;
            if (img != null)
            {
                var imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "drag_drop_animation.gif");
                if(File.Exists(imgPath))
                {
                    var bmi = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imgPath))
                    {
                        DecodePixelWidth = 384,
                        DecodePixelHeight = 216
                    };
                    img.Source = bmi;
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //Still rendering otherwise?!
            if (img != null)
            {
                img.Source = null;
            }
        }
    }
}
