using livelywpf.Core;
using livelywpf.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace livelywpf
{
    class LibraryPreviewViewModel : ObservableObject
    {
        readonly private LibraryModel libData;
        readonly ILibraryPreview Winstance;
        public LibraryPreviewViewModel( ILibraryPreview wInterface, IWallpaper wallpaper)
        {
            Winstance = wInterface;
            Winstance.CaptureProgress += WInstance_CaptureProgress;
            Winstance.PreviewUpdated += WInstance_PreviewUpdated;
            Winstance.ThumbnailUpdated += WInstance_ThumbnailUpdated;
            Winstance.WallpaperAttached += WInstance_WallpaperAttached;

            libData = wallpaper.GetWallpaperData();
            if (libData.LivelyInfo.Type == WallpaperType.url
            || libData.LivelyInfo.Type == WallpaperType.web
            || libData.LivelyInfo.Type == WallpaperType.webaudio)
            {
                if (libData.LivelyInfo.Type == WallpaperType.url)
                    Url = libData.FilePath;

                try
                {
                    Title = wallpaper.GetProcess().MainWindowTitle;
                }
                catch { }

                if (String.IsNullOrWhiteSpace(Title))
                {
                    Title = GetLastSegmentUrl(libData.FilePath);
                }
            }
            else
            {
                try
                {
                    Title = Path.GetFileNameWithoutExtension(libData.FilePath);
                }
                catch (ArgumentException)
                {
                    Title = libData.FilePath;
                }

                if (String.IsNullOrWhiteSpace(Title))
                {
                    Title = libData.FilePath;
                }
            }

            GifCheck = Program.SettingsVM.Settings.GifCapture;
            ZipCheck = Program.SettingsVM.Settings.LivelyZipGenerate;
        }

        #region data

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                libData.Title = _title;
                libData.LivelyInfo.Title = _title;
                OnPropertyChanged("Title");
            }
        }

        private string _desc;
        public string Desc
        {
            get { return _desc; }
            set
            {
                _desc = value;
                libData.Desc = _desc;
                libData.LivelyInfo.Desc = _desc;
                OnPropertyChanged("Desc");
            }
        }

        private string _author;
        public string Author
        {
            get { return _author; }
            set
            {
                _author = value;
                libData.LivelyInfo.Author = _author;
                OnPropertyChanged("Author");
            }
        }

        private string _url;
        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                libData.SrcWebsite = libData.GetUri(_url, "https");
                libData.LivelyInfo.Contact = _url;
                OnPropertyChanged("Url");
            }
        }

        #endregion data

        #region ui 

        private double _currentProgress;
        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                _currentProgress = value;
                OnPropertyChanged("CurrentProgress");
            }
        }

        private bool _gifCheck;
        public bool GifCheck
        {
            get { return _gifCheck; }
            set
            {
                _gifCheck = value;
                Program.SettingsVM.Settings.GifCapture = _gifCheck;
                Program.SettingsVM.UpdateConfigFile();
            }
        }

        private bool _zipCheck;
        public bool ZipCheck
        {
            get { return _zipCheck; }
            set
            {
                _zipCheck = value;
                Program.SettingsVM.Settings.LivelyZipGenerate = _zipCheck;
                Program.SettingsVM.UpdateConfigFile();
            }
        }

        #endregion ui

        #region interface methods

        private void WInstance_WallpaperAttached(object sender, EventArgs e)
        {
            Winstance.StartThumbnailCaptureLoop(libData.LivelyInfoFolderPath);
        }

        private bool _canCancelOperation = true;
        private void WInstance_CaptureProgress(object sender, double value)
        {
            if(_canCancelOperation)
            {
                _canCancelOperation = false;
                CaptureCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }

            CurrentProgress = value;
            if (CurrentProgress == 100)
            {
                libData.DataType = LibraryTileType.ready;
                LivelyInfoJSON.SaveWallpaperMetaData(libData.LivelyInfo, Path.Combine(libData.LivelyInfoFolderPath, "LivelyInfo.json"));
                Program.LibraryVM.SortExistingWallpaper(libData);

                if (Program.SettingsVM.Settings.LivelyZipGenerate)
                {
                    string savePath = "";
                    var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = "Select location to save the file",
                        Filter = "Lively/zip file|*.zip",
                        FileName = libData.Title,
                    };
                    if (saveFileDialog1.ShowDialog() == true)
                    {
                        savePath = saveFileDialog1.FileName;
                    }
                    if (!String.IsNullOrEmpty(savePath))
                    {
                        Program.LibraryVM.WallpaperExport(libData, savePath);
                    }
                }
                Winstance.Exit();
            }
        }

        private RelayCommand _captureCommand;
        public RelayCommand CaptureCommand
        {
            get
            {
                if (_captureCommand == null)
                {
                    _captureCommand = new RelayCommand(
                        param => Winstance.StartCapture(libData.LivelyInfoFolderPath), param => _canCancelOperation);
                }
                return _captureCommand;
            }
            private set
            {
                _captureCommand = value;
            }
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    _cancelCommand = new RelayCommand(
                        param => Winstance.Exit(), param => _canCancelOperation);
                }
                return _cancelCommand;
            }
            private set
            {
                _cancelCommand = value;
            }
        }

        private void WInstance_ThumbnailUpdated(object sender, string path)
        {
            libData.ImagePath = null;
            libData.ImagePath = path;
            libData.LivelyInfo.Thumbnail = path;
            libData.ThumbnailPath = path;
        }

        private void WInstance_PreviewUpdated(object sender, string path)
        {
            libData.ImagePath = null;
            libData.ImagePath = path;
            libData.LivelyInfo.Preview = path;
            libData.PreviewClipPath = path;
        }

        #endregion interface methods

        #region helpers

        private string GetLastSegmentUrl(string url)
        {
            string result;
            try
            {
                Uri uri = new Uri(url);
                result = uri.Segments.Last();
                //for some urls, output will be: /
                if (result.Equals("/", StringComparison.OrdinalIgnoreCase) || result.Equals("//", StringComparison.OrdinalIgnoreCase))
                {
                    result = url.Replace(@"https://www.", "");
                }
                result = result.Replace("/", "");
            }
            catch
            {
                result = url;
            }
            return result;
        }

        #endregion helpers
    }
}
