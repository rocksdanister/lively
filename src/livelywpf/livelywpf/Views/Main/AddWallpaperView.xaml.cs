using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

            public FileFilter(WallpaperType type, string filterText)
            {
                this.Type = type;
                this.Extentions = filterText;
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
        }

        readonly FileFilter[] wallpaperFilter = new FileFilter[] {
            new FileFilter(WallpaperType.video, "*.dat; *.wmv; *.3g2; *.3gp; *.3gp2;" +
                " *.3gpp; *.amv; *.asf;  *.avi; *.bin; *.cue; *.divx; *.dv; *.flv; *.gxf; *.iso; *.m1v; *.m2v; *.m2t; *.m2ts; *.m4v; " +
                " *.mkv; *.mov; *.mp2; *.mp2v; *.mp4; *.mp4v; *.mpa; *.mpe; *.mpeg; *.mpeg1; *.mpeg2; *.mpeg4; *.mpg; *.mpv2; *.mts; " +
                "*.nsv; *.nuv; *.ogg; *.ogm; *.ogv; *.ogx; *.ps; *.rec; *.rm; *.rmvb; *.tod; *.ts; *.tts; *.vob; *.vro; *.webm"),
            new FileFilter(WallpaperType.gif,"*.gif"),
            new FileFilter(WallpaperType.web, "*.html"),
            new FileFilter(WallpaperType.webaudio, "*.html"), 
            /*
            new FileFilter(WallpaperType.unity,"Unity Game Executable |*.exe"),
            new FileFilter(WallpaperType.unityaudio,"Unity Audio Visualiser |*.exe"),
            new FileFilter(WallpaperType.app,"Application |*.exe"),
            new FileFilter(WallpaperType.godot,"Godot Game Executable |*.exe")
            */
        };

        public AddWallpaperView()
        {
            InitializeComponent();
            UrlText.Text = Program.SettingsVM.Settings.SavedURL;
        }

        private void UrlBtn_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(UrlText.Text))
            {
                return;
            }

            Program.LibraryVM.AddWallpaper(UrlText.Text,
                          WallpaperType.videostream,
                          LibraryTileType.processing,
                          Program.SettingsVM.Settings.SelectedDisplay);
            App.AppWindow.NavViewNavigate("library");
        }

        private void UrlText_TextChanged(object sender, TextChangedEventArgs e)
        {
            Program.SettingsVM.Settings.SavedURL = UrlText.Text;
            Program.SettingsVM.UpdateConfigFile();
        }

        private void FileBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = Properties.Resources.TitleAddWallpaper,
                CheckFileExists = true,
                CheckPathExists = true,       
            };
            StringBuilder filterString = new StringBuilder();
            foreach (var item in wallpaperFilter)
            {
                filterString.Append(item.LocalisedTypeText);
                filterString.Append("|");
                filterString.Append(item.Extentions);
                filterString.Append("|");
            }
            filterString.Remove(filterString.Length - 1, 1);
            openFileDlg.Filter = filterString.ToString();
            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true)
            {
                Program.LibraryVM.AddWallpaper(openFileDlg.FileName,
                                wallpaperFilter[openFileDlg.FilterIndex - 1].Type,
                                LibraryTileType.processing,
                                Program.SettingsVM.Settings.SelectedDisplay);
                App.AppWindow.NavViewNavigate("library");
            }
            filterString.Clear();
        }
    }
}
