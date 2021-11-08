using livelywpf.Core;
using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.NetStream;
using livelywpf.Helpers.Storage;
using livelywpf.Views;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using YoutubeExplode;
using livelywpf.Models;
using livelywpf.Views.Dialogues;
using livelywpf.Services;
using Microsoft.Extensions.DependencyInjection;
using livelywpf.Helpers;

namespace livelywpf.ViewModels
{
    class LibraryPreviewViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ILibraryModel libData;
        private readonly ILibraryPreview Winstance;
        private readonly ILivelyInfoModel livelyInfoCopy;
        private readonly string thumbnailOriginalPath;

        private readonly IUserSettingsService userSettings;
        private readonly LibraryViewModel libraryVm;

        public LibraryPreviewViewModel(ILibraryPreview wInterface, IWallpaper wallpaper)
        {
            this.userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            this.libraryVm = App.Services.GetRequiredService<LibraryViewModel>();

            Winstance = wInterface;
            Winstance.CaptureProgress += WInstance_CaptureProgress;
            Winstance.PreviewUpdated += WInstance_PreviewUpdated;
            Winstance.ThumbnailUpdated += WInstance_ThumbnailUpdated;
            Winstance.WallpaperAttached += WInstance_WallpaperAttached;

            libData = wallpaper.Model;
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
                    if (userSettings.Settings.ExtractStreamMetaData)
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

            GifCheck = userSettings.Settings.GifCapture;
            ZipCheck = userSettings.Settings.LivelyZipGenerate;
        }

        private async Task SetYtMetadata(string url)
        {
            try
            {
                //Library also checks, this is not required..
                if (!StreamHelper.IsYoutubeUrl(url))
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private string _url;
        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                try
                {
                    libData.SrcWebsite = LinkHandler.SanitizeUrl(_url);
                }
                catch
                {
                    libData.SrcWebsite = null;
                }
                libData.LivelyInfo.Contact = _url;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private double _currentProgress;
        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                _currentProgress = value;
                OnPropertyChanged();
            }
        }

        private bool _gifCheck;
        public bool GifCheck
        {
            get { return _gifCheck; }
            set
            {
                _gifCheck = value;
                if (_gifCheck != userSettings.Settings.GifCapture)
                {
                    userSettings.Settings.GifCapture = _gifCheck;
                    userSettings.Save<ISettingsModel>();
                }
                OnPropertyChanged();
            }
        }

        private bool _zipCheck;
        public bool ZipCheck
        {
            get { return _zipCheck; }
            set
            {
                _zipCheck = value;
                if (_zipCheck != userSettings.Settings.LivelyZipGenerate)
                {
                    userSettings.Settings.LivelyZipGenerate = _zipCheck;
                    userSettings.Save<ISettingsModel>();
                }
                OnPropertyChanged();
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
            if (userSettings.Settings.LivelyGUIRendering != LivelyGUIState.lite)
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
                    JsonStorage<LivelyInfoModel>.StoreData(
                        Path.Combine(libData.LivelyInfoFolderPath, "LivelyInfo.json"), libData.LivelyInfo);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }

                //change from pos 0..
                libData.DataType = LibraryTileType.ready;
                libraryVm.SortLibraryItem((LibraryModel)libData);

                //create lively .zip..
                if (userSettings.Settings.LivelyZipGenerate)
                {
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = Properties.Resources.TitleLocation,
                        Filter = "Lively.zip|*.zip",
                        //title ending with '.' can have diff extension (example: parallax.js)
                        FileName = Path.ChangeExtension(libData.Title, ".zip"),
                    };
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                        {
                            libraryVm.WallpaperExport(libData, saveFileDialog.FileName);
                        }
                    }    
                }
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
                    libData.ImagePath = userSettings.Settings.LivelyGUIRendering == LivelyGUIState.normal ?
                        (File.Exists(libData.PreviewClipPath) ? libData.PreviewClipPath : libData.ThumbnailPath) : libData.ThumbnailPath;

                    //change from pos 0..
                    libData.DataType = LibraryTileType.ready;
                    libraryVm.SortLibraryItem((LibraryModel)libData);
                }
                else
                {
                    //nothing, core will terminate and delete the wp folder when LivelyInfo.json not found..
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
