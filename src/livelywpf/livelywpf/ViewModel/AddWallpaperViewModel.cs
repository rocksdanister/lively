using livelywpf.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace livelywpf
{
    public class AddWallpaperViewModel : ObservableObject
    {
        private readonly string fileDialogFilter;
        public AddWallpaperViewModel()
        {
            WebUrlText = Program.SettingsVM.Settings.SavedURL;
            fileDialogFilter = FileFilter.GetLivelySupportedFileDialogFilter(true);
        }

        private string _webUrlText;
        public string WebUrlText
        {
            get { return _webUrlText; }
            set
            {
                _webUrlText = value;
                OnPropertyChanged("WebUrlText");
            }
        }

        public bool IsStreamUrlTextVisible => Program.SettingsVM.Settings.DebugMenu;

        private string _streamUrlText;
        public string StreamUrlText
        {
            get { return _streamUrlText; }
            set
            {
                _streamUrlText = value;
                OnPropertyChanged("StreamUrlText");
            }
        }

        private RelayCommand _browseWebCommand;
        public RelayCommand BrowseWebCommand
        {
            get
            {
                if (_browseWebCommand == null)
                {
                    _browseWebCommand = new RelayCommand(
                        param => WebBrowseAction());
                }
                return _browseWebCommand;
            }
        }

        private void WebBrowseAction()
        {
            Uri uri;
            try
            {
                uri = LinkHandler.SanitizeUrl(WebUrlText);
            }
            catch
            {
                return;
            }

            WebUrlText = uri.OriginalString;
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

            Program.SettingsVM.Settings.SavedURL = WebUrlText;
            Program.SettingsVM.UpdateConfigFile();

            App.AppWindow.NavViewNavigate("library");
        }

        private RelayCommand _browseStreamCommand;
        public RelayCommand BrowseStreamCommand
        {
            get
            {
                if (_browseStreamCommand == null)
                {
                    _browseStreamCommand = new RelayCommand(
                        param => StreamBrowseAction());
                }
                return _browseStreamCommand;
            }
        }

        private void StreamBrowseAction()
        {
            Uri uri;
            try
            {
                uri = LinkHandler.SanitizeUrl(StreamUrlText);
            }
            catch
            {
                return;
            }

            StreamUrlText = uri.OriginalString;
            Program.LibraryVM.AddWallpaper(uri.OriginalString,
                  WallpaperType.videostream,
                  LibraryTileType.processing,
                  Program.SettingsVM.Settings.SelectedDisplay);

            App.AppWindow.NavViewNavigate("library");
        }

        private RelayCommand _browseFileCommand;
        public RelayCommand BrowseFileCommand
        {
            get
            {
                if (_browseFileCommand == null)
                {
                    _browseFileCommand = new RelayCommand(
                        param => FileBrowseAction());
                }
                return _browseFileCommand;
            }
        }

        private void FileBrowseAction()
        {
            var openFileDlg = new OpenFileDialog
            {
                Title = Properties.Resources.TitleAddWallpaper,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
                Filter = fileDialogFilter,
            };

            if (openFileDlg.ShowDialog() == true)
            {
                if (openFileDlg.FileNames.Length > 1)
                {
                    _ = new Views.MultiWallpaperImport(openFileDlg.FileNames.ToList())
                    {
                        //This dialog on right-topmost like position and librarypreview window left-topmost.
                        WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                        Left = App.AppWindow.Left + App.AppWindow.Width - (App.AppWindow.Width / 1.5),
                        Top = App.AppWindow.Top + (App.AppWindow.Height / 15),
                        Owner = App.AppWindow,
                        Width = App.AppWindow.Width / 1.5,
                        Height = App.AppWindow.Height / 1.3,
                    }.ShowDialog();
                    return;
                }
                else if (openFileDlg.FilterIndex == 1)
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
                                _ = Program.LibraryVM.WallpaperInstall(openFileDlg.FileName, false);
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
                        /*
                        _ = DialogService.ShowConfirmationDialog(Properties.Resources.TextError, 
                            $"{Properties.Resources.TextUnsupportedFile} ({Path.GetExtension(openFileDlg.FileName)})", 
                            Properties.Resources.TextOK);
                        */
                        System.Windows.MessageBox.Show(
                            $"{Properties.Resources.TextUnsupportedFile} ({Path.GetExtension(openFileDlg.FileName)})",
                            Properties.Resources.TextError);
                        return;
                    }
                }
                else
                {
                    if (FileFilter.LivelySupportedFormats[openFileDlg.FilterIndex - 2].Type == (WallpaperType)100)
                    {
                        if (ZipExtract.CheckLivelyZip(openFileDlg.FileName))
                        {
                            _ = Program.LibraryVM.WallpaperInstall(openFileDlg.FileName, false);
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
                App.AppWindow.NavViewNavigate("library");
            }
        }
    }
}
