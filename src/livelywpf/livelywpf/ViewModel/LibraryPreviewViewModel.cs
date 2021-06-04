using livelywpf.Core;
using livelywpf.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using YoutubeExplode;

namespace livelywpf
{
    class LibraryPreviewViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly LibraryModel libData;
        private readonly ILibraryPreview Winstance;
        private readonly LivelyInfoModel livelyInfoCopy;
        private readonly string thumbnailOriginalPath;

        public LibraryPreviewViewModel( ILibraryPreview wInterface, IWallpaper wallpaper)
        {
            Winstance = wInterface;
            Winstance.CaptureProgress += WInstance_CaptureProgress;
            Winstance.PreviewUpdated += WInstance_PreviewUpdated;
            Winstance.ThumbnailUpdated += WInstance_ThumbnailUpdated;
            Winstance.WallpaperAttached += WInstance_WallpaperAttached;

            libData = wallpaper.GetWallpaperData();
            if (libData.DataType == LibraryTileType.edit)
            {
                //taking backup to restore original data if user cancel..
                livelyInfoCopy = new LivelyInfoModel(libData.LivelyInfo);
                thumbnailOriginalPath = libData.ThumbnailPath;

                //use existing data for editing already imported wallpaper..
                Title = libData.LivelyInfo.Title;
                Desc = libData.LivelyInfo.Desc;
                Url = libData.LivelyInfo.Contact;
                Author = libData.LivelyInfo.Author;

                //consistency..
                libData.ImagePath = libData.ThumbnailPath;
            }
            else
            {
                //guess data based on filename, window title etc..
                if (libData.LivelyInfo.Type == WallpaperType.videostream)
                {
                    Url = libData.FilePath;
                    Title = GetLastSegmentUrl(libData.FilePath);
                    if (Program.SettingsVM.Settings.ExtractStreamMetaData)
                    {
                        _ = SetYtMetadata(libData.FilePath);
                    }
                }
                else if (libData.LivelyInfo.Type == WallpaperType.url
                    || libData.LivelyInfo.Type == WallpaperType.web
                    || libData.LivelyInfo.Type == WallpaperType.webaudio)
                {
                    if (libData.LivelyInfo.Type == WallpaperType.url)
                        Url = libData.FilePath;

                    Title = GetLastSegmentUrl(libData.FilePath);
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

                if (libData.DataType == LibraryTileType.cmdImport ||
                    libData.DataType == LibraryTileType.multiImport)
                {
                    //skip black-transition/intro clip of video clips if any..
                    wallpaper.SetPlaybackPos(35, PlaybackPosType.absolutePercent);
                }
            }

            GifCheck = Program.SettingsVM.Settings.GifCapture;
            ZipCheck = Program.SettingsVM.Settings.LivelyZipGenerate;
        }

        private async Task SetYtMetadata(string url)
        {
            try
            {
                //Library also checks, this is not required..
                if (!Helpers.StreamHelper.IsYoutubeUrl(url))
                    return;

                IsUserEditable = false;
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(url);
                //set data
                Title = video.Title;
                Desc = video.Description;
                Author = video.Author.Title;
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
            finally
            {
                IsUserEditable = true;
            }
        }

        #region data

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = (value?.Length > 100 ? value.Substring(0, 100) : value);
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
                _desc = (value?.Length > 5000 ? value.Substring(0, 5000) : value);
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
                _author = (value?.Length > 100 ? value.Substring(0, 100) : value);
                libData.Author = _author;
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

        private bool _isUserEditable = true;
        public bool IsUserEditable
        {
            get { return _isUserEditable; } 
            set
            {
                _isUserEditable = value;
                OnPropertyChanged("IsUserEditable");
            }
        }

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
                if (_gifCheck != Program.SettingsVM.Settings.GifCapture)
                {
                    Program.SettingsVM.Settings.GifCapture = _gifCheck;
                    Program.SettingsVM.UpdateConfigFile();
                }
                OnPropertyChanged("GifCheck");
            }
        }

        private bool _zipCheck;
        public bool ZipCheck
        {
            get { return _zipCheck; }
            set
            {
                _zipCheck = value;
                if (_zipCheck != Program.SettingsVM.Settings.LivelyZipGenerate)
                {
                    Program.SettingsVM.Settings.LivelyZipGenerate = _zipCheck;
                    Program.SettingsVM.UpdateConfigFile();
                }
                OnPropertyChanged("ZipCheck");
            }
        }

        public void OnWindowClosed(object sender, EventArgs e)
        {
            CleanUp();
        }

        #endregion ui

        #region interface methods

        private async void WInstance_WallpaperAttached(object sender, EventArgs e)
        {
            if (libData.DataType == LibraryTileType.cmdImport || 
                libData.DataType == LibraryTileType.multiImport)
            {
                //warm up time/seek delay artifact fix for mpv..
                await Task.Delay(1000);
                Winstance.StartCapture(libData.LivelyInfoFolderPath);
            }
            else
            {
                Winstance.StartThumbnailCaptureLoop(libData.LivelyInfoFolderPath);
            }
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
                        param => UserActionStart(), param => _canCancelOperation);
                }
                return _captureCommand;
            }
            private set
            {
                _captureCommand = value;
            }
        }

        private void UserActionStart()
        {
            if (libData.DataType == LibraryTileType.edit)
            {
                try
                {
                    //deleting existing file(s) if any..
                    File.Delete(thumbnailOriginalPath);
                    File.Delete(libData.PreviewClipPath);
                }
                catch { }

                //resetting..
                libData.ImagePath = null;
                libData.ThumbnailPath = null;
                libData.LivelyInfo.Thumbnail = null;
                libData.PreviewClipPath = null;
                libData.LivelyInfo.Preview = null;
            }
            Winstance.StartCapture(libData.LivelyInfoFolderPath);
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
            libData.LivelyInfo.Thumbnail = libData.LivelyInfo.IsAbsolutePath ? path : Path.GetFileName(path);
            libData.ThumbnailPath = path;
        }

        private void WInstance_PreviewUpdated(object sender, string path)
        {
            if (Program.SettingsVM.Settings.LivelyGUIRendering != LivelyGUIState.lite)
            {
                libData.ImagePath = null;
                libData.ImagePath = path;
            }
            libData.LivelyInfo.Preview = libData.LivelyInfo.IsAbsolutePath ?  path : Path.GetFileName(path);
            libData.PreviewClipPath = path;
        }

        private void CleanUp()
        {
            if (CurrentProgress == 100)
            {
                //user pressed ok..everything went well :)
                try
                {
                    Helpers.JsonStorage<LivelyInfoModel>.StoreData(
                        Path.Combine(libData.LivelyInfoFolderPath, "LivelyInfo.json"), libData.LivelyInfo);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }

                //change from pos 0..
                libData.DataType = LibraryTileType.ready;
                Program.LibraryVM.SortLibraryItem(libData);
            }
            else
            {
                //user close or 'x' btn press..
                if (libData.DataType == LibraryTileType.edit)
                {
                    //restore previous data..
                    Title = livelyInfoCopy.Title;
                    Desc = livelyInfoCopy.Desc;
                    Author = livelyInfoCopy.Author;
                    Url = livelyInfoCopy.Contact;

                    //restoring original thumbnail img..
                    libData.ThumbnailPath = thumbnailOriginalPath;
                    libData.LivelyInfo.Thumbnail = libData.LivelyInfo.IsAbsolutePath ? thumbnailOriginalPath : Path.GetFileName(thumbnailOriginalPath);
                    //restore tile img..
                    libData.ImagePath = null;
                    libData.ImagePath = Program.SettingsVM.Settings.LivelyGUIRendering == LivelyGUIState.normal ?
                        (File.Exists(libData.PreviewClipPath) ? libData.PreviewClipPath : libData.ThumbnailPath) : libData.ThumbnailPath;

                    //change from pos 0..
                    libData.DataType = LibraryTileType.ready;
                    Program.LibraryVM.SortLibraryItem(libData);
                }
                else
                {
                    //nothing, core will terminate and delete the wp folder when LivelyInfo.json not found..
                }
            }

            if (Program.SettingsVM.Settings.LivelyZipGenerate)
            {
                string savePath = "";
                var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "Select location to save the file",
                    Filter = "Lively/zip file|*.zip",
                    //title ending with '.' can have diff extension (example: parallax.js)
                    FileName = Path.ChangeExtension(libData.Title, ".zip"),
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

            Winstance.CaptureProgress -= WInstance_CaptureProgress;
            Winstance.PreviewUpdated -= WInstance_PreviewUpdated;
            Winstance.ThumbnailUpdated -= WInstance_ThumbnailUpdated;
            Winstance.WallpaperAttached -= WInstance_WallpaperAttached;
        }

        #endregion interface methods

        #region helpers

        private string GetLastSegmentUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segment = uri.Segments.Last();
                return (segment == "/" || segment == "//") ? uri.Host.Replace("www.", string.Empty) : segment.Replace("/", string.Empty);
            }
            catch
            {
                return url;
            }
        }

        #endregion helpers
    }
}
