using livelywpf.Helpers;
using livelywpf.Helpers.Archive;
using livelywpf.Helpers.Files;
using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.NetStream;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using livelywpf.Models;
using livelywpf.Views.Dialogues;
using livelywpf.Services;
using livelywpf.Views;

namespace livelywpf.ViewModels
{
    public class AddWallpaperViewModel : ObservableObject
    {
        private readonly string fileDialogFilter;
        private readonly IUserSettingsService userSettings;
        private readonly LibraryViewModel libraryVm;
        private readonly MainWindow appWindow;

        public AddWallpaperViewModel(IUserSettingsService userSettings, LibraryViewModel libraryVm, MainWindow appWindow)
        {
            this.userSettings = userSettings;
            this.libraryVm = libraryVm;
            this.appWindow = appWindow;

            WebUrlText = userSettings.Settings.SavedURL;
            fileDialogFilter = FileFilter.GetLivelySupportedFileDialogFilter(true);
        }

        private string _webUrlText;
        public string WebUrlText
        {
            get { return _webUrlText; }
            set
            {
                _webUrlText = value;
                OnPropertyChanged();
            }
        }

        public bool IsStreamUrlTextVisible => userSettings.Settings.DebugMenu;

        private string _streamUrlText;
        public string StreamUrlText
        {
            get { return _streamUrlText; }
            set
            {
                _streamUrlText = value;
                OnPropertyChanged();
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
            if (userSettings.Settings.AutoDetectOnlineStreams &&
                 StreamHelper.IsSupportedStream(uri))
            {
                libraryVm.AddWallpaper(uri.OriginalString,
                    WallpaperType.videostream,
                    LibraryTileType.processing,
                    userSettings.Settings.SelectedDisplay);
            }
            else
            {
                libraryVm.AddWallpaper(uri.OriginalString,
                    WallpaperType.url,
                    LibraryTileType.processing,
                    userSettings.Settings.SelectedDisplay);
            }

            userSettings.Settings.SavedURL = WebUrlText;
            userSettings.Save<ISettingsModel>();

            appWindow.NavViewNavigate("library");
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
            libraryVm.AddWallpaper(uri.OriginalString,
                  WallpaperType.videostream,
                  LibraryTileType.processing,
                  userSettings.Settings.SelectedDisplay);

            appWindow.NavViewNavigate("library");
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
                    _ = new MultiWallpaperImport(openFileDlg.FileNames.ToList())
                    {
                        //This dialog on right-topmost like position and librarypreview window left-topmost.
                        WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                        Left = appWindow.Left + appWindow.Width - (appWindow.Width / 1.5),
                        Top = appWindow.Top + (appWindow.Height / 15),
                        Owner = appWindow,
                        Width = appWindow.Width / 1.5,
                        Height = appWindow.Height / 1.3,
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
                                _ = libraryVm.WallpaperInstall(openFileDlg.FileName, false);
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
                            libraryVm.AddWallpaper(openFileDlg.FileName,
                                type,
                                LibraryTileType.processing,
                                userSettings.Settings.SelectedDisplay);
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
                            _ = libraryVm.WallpaperInstall(openFileDlg.FileName, false);
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
                        libraryVm.AddWallpaper(openFileDlg.FileName,
                            FileFilter.LivelySupportedFormats[openFileDlg.FilterIndex - 2].Type,
                            LibraryTileType.processing,
                            userSettings.Settings.SelectedDisplay);
                    }
                }
                appWindow.NavViewNavigate("library");
            }
        }
    }
}
