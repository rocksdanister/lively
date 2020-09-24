using livelywpf.Helpers;
using Microsoft.Toolkit.Wpf.UI.XamlHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private class FileFilter
        {
            public WallpaperType Type { get; set; }
            public string Extentions { get; set; }
            public string LocalisedTypeText { get; set; }

            public FileFilter(WallpaperType type, string filterText, string localisedText = null)
            {
                this.Type = type;
                this.Extentions = filterText;
                if(localisedText == null)
                {
                    LocalisedTypeText = this.Type switch
                    {
                        WallpaperType.app => Properties.Resources.TextApplication,
                        WallpaperType.unity => Properties.Resources.TextApplication + " Unity",
                        WallpaperType.godot => Properties.Resources.TextApplication + " Godot",
                        WallpaperType.unityaudio => Properties.Resources.TextApplication + " Unity " + Properties.Resources.TitleAudio,
                        WallpaperType.bizhawk => Properties.Resources.TextApplication + " Bizhawk",
                        WallpaperType.web => Properties.Resources.TextWebsite,
                        WallpaperType.webaudio => Properties.Resources.TextWebsite + " " + Properties.Resources.TitleAudio,
                        WallpaperType.url => Properties.Resources.TextOnline,
                        WallpaperType.video => Properties.Resources.TextVideo,
                        WallpaperType.gif => Properties.Resources.TextGIF,
                        WallpaperType.videostream => Properties.Resources.TextWebStream,
                        _ => "Nil",
                    };
                }
                else
                {
                    this.LocalisedTypeText = localisedText;
                }
            }
        }

        readonly FileFilter[] wallpaperFilter = new FileFilter[] {
            new FileFilter(WallpaperType.video, 
                "*.wmv; *.avi; *.bin; *.divx; *.flv; *.m4v; " +
                "*.mkv; *.mov; *.mp4; *.mp4v; *.mpeg4; *.mpg; *.webm" +
                "*.ogm; *.ogv; *.ogx; *.ts"),
            new FileFilter(WallpaperType.gif, "*.gif"),
            new FileFilter(WallpaperType.web, "*.html"),
            new FileFilter(WallpaperType.webaudio, "*.html"),
            new FileFilter(WallpaperType.app,"*.exe"),
            new FileFilter(WallpaperType.unity,"*.exe"),
            //new FileFilter(WallpaperType.unityaudio,"Unity Audio Visualiser |*.exe"),
            new FileFilter(WallpaperType.godot,"*.exe"),
            //note: lively .zip is not a wallpapertype, its a filetype.
            new FileFilter((WallpaperType)(100), "*.zip", Properties.Resources.TitleAppName),
        };

        private readonly StringBuilder filterString;
        public AddWallpaperView()
        {
            InitializeComponent();
            UrlText.Text = Program.SettingsVM.Settings.SavedURL;
            filterString = new StringBuilder();
            filterString.Append(Properties.Resources.TextAllFiles + "|*.*|");
            foreach (var item in wallpaperFilter)
            {
                filterString.Append(item.LocalisedTypeText);
                filterString.Append("|");
                filterString.Append(item.Extentions);
                filterString.Append("|");
            }
            filterString.Remove(filterString.Length - 1, 1);
        }

        private void FileBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = Properties.Resources.TitleAddWallpaper,
                CheckFileExists = true,
                CheckPathExists = true,       
            };
            openFileDlg.Filter = filterString.ToString();
            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true)
            {
                if (Path.GetExtension(openFileDlg.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase)) 
                {
                    Program.LibraryVM.WallpaperInstall(openFileDlg.FileName);
                }
                else if(openFileDlg.FilterIndex == 1)
                {
                    //All Files
                    var item = wallpaperFilter.FirstOrDefault(x => x.Extentions.Contains(Path.GetExtension(openFileDlg.FileName), StringComparison.OrdinalIgnoreCase));
                    if(item != null)
                    {
                        Program.LibraryVM.AddWallpaper(openFileDlg.FileName,
                            item.Type,
                            LibraryTileType.processing,
                            Program.SettingsVM.Settings.SelectedDisplay);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    Program.LibraryVM.AddWallpaper(openFileDlg.FileName,
                    wallpaperFilter[openFileDlg.FilterIndex - 2].Type,
                    LibraryTileType.processing,
                    Program.SettingsVM.Settings.SelectedDisplay);
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
