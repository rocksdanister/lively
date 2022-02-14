using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.MVVM;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.Wpf.Helpers;
using Lively.UI.Wpf.Helpers.MVVM;
using Lively.UI.Wpf.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.Wpf.ViewModels
{
    public class AddWallpaperViewModel : ObservableObject
    {
        public ILibraryModel NewWallpaper { get; private set; }
        public event EventHandler OnRequestClose;
        private readonly string fileDialogFilter;
        private readonly IUserSettingsClient userSettings;
        //private readonly LibraryViewModel libraryVm;
        private readonly MainWindow appWindow;
        private readonly LibraryUtil libraryUtil;

        public AddWallpaperViewModel(
            IUserSettingsClient userSettings,
            LibraryUtil libraryUtil,
            MainWindow appWindow)
        {
            this.userSettings = userSettings;
            //this.libraryVm = libraryVm;
            this.libraryUtil = libraryUtil;
            this.appWindow = appWindow;

            WebUrlText = userSettings.Settings.SavedURL;
            fileDialogFilter = LocalizationUtil.GetLocalizedSupportedFileDialogFilter(true);
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

            try
            {
                NewWallpaper = libraryUtil.AddWallpaperLink(uri.OriginalString);
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                //TODO
            }

            /*
            if (userSettings.Settings.AutoDetectOnlineStreams &&
                    StreamUtil.IsSupportedStream(uri))
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
            */
        }

        private RelayCommand _browseFileCommand;
        public RelayCommand BrowseFileCommand
        {
            get
            {
                if (_browseFileCommand == null)
                {
                    _browseFileCommand = new RelayCommand(
                        async param => await FileBrowseAction());
                }
                return _browseFileCommand;
            }
        }

        private async Task FileBrowseAction()
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
                    /*
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
                    */
                    return;
                }
                else
                {
                    try
                    {
                       var item = await libraryUtil.AddWallpaperFile(openFileDlg.FileName);
                        if (item.LivelyInfo.IsAbsolutePath)
                        {
                            NewWallpaper = item;
                            OnRequestClose?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    catch (Exception e)
                    {
                        System.Windows.MessageBox.Show(
                             e.Message,
                             Properties.Resources.TextError);
                        return;
                    }
                }
                appWindow.NavViewNavigate("library");
            }
        }
    }
}
