using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Lively.Common.Helpers.Storage;
using Lively.Core;
using Lively.Models;
using Lively.Services;
using Lively.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.ViewModels
{
    public class LibraryPreviewViewModel : ObservableObject
    {
        public event EventHandler<WallpaperUpdateArgs> DetailsUpdated;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ILibraryModel libData;
        private readonly ILibraryPreview Winstance;
        private readonly ILivelyInfoModel livelyInfoCopy;
        private readonly string thumbnailOriginalPath;

        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        //private readonly LibraryViewModel libraryVm;

        public LibraryPreviewViewModel(ILibraryPreview wInterface, IWallpaper wallpaper)
        {
            this.userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            this.desktopCore = App.Services.GetRequiredService<IDesktopCore>();
            //this.libraryVm = App.Services.GetRequiredService<LibraryViewModel>();

            Winstance = wInterface;
            Winstance.CaptureProgress += WInstance_CaptureProgress;
            Winstance.PreviewUpdated += WInstance_PreviewUpdated;
            Winstance.ThumbnailUpdated += WInstance_ThumbnailUpdated;
            Winstance.WallpaperAttached += WInstance_WallpaperAttached;

            libData = wallpaper.Model;
            if (libData.DataType == LibraryItemType.edit)
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
                    Title = LinkHandler.GetLastSegmentUrl(libData.FilePath);
                    if (userSettings.Settings.ExtractStreamMetaData)
                    {
                        //_ = SetYtMetadata(libData.FilePath);
                    }
                }
                else if (libData.LivelyInfo.Type == WallpaperType.url
                    || libData.LivelyInfo.Type == WallpaperType.web
                    || libData.LivelyInfo.Type == WallpaperType.webaudio)
                {
                    if (libData.LivelyInfo.Type == WallpaperType.url)
                        Url = libData.FilePath;

                    Title = LinkHandler.GetLastSegmentUrl(libData.FilePath);
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

                if (libData.DataType == LibraryItemType.cmdImport ||
                    libData.DataType == LibraryItemType.multiImport)
                {
                    //skip black-transition/intro clip of video clips if any..
                    wallpaper.SetPlaybackPos(35, PlaybackPosType.absolutePercent);
                }
            }
        }

        /*
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
        */

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
                DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.changed, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
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
                DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.changed, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
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
                DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.changed, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
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
                DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.changed, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
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

        public void OnWindowClosed(object sender, EventArgs e)
        {
            CleanUp();
        }

        #endregion ui

        #region interface methods

        private async void WInstance_WallpaperAttached(object sender, EventArgs e)
        {
            if (libData.DataType == LibraryItemType.cmdImport ||
                libData.DataType == LibraryItemType.multiImport)
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
            if (_canCancelOperation)
            {
                _canCancelOperation = false;
                CaptureCommand.NotifyCanExecuteChanged();
                CancelCommand.NotifyCanExecuteChanged();
            }

            CurrentProgress = value;
            if (CurrentProgress == 100)
            {
                Winstance.Exit();
            }
        }

        private RelayCommand _captureCommand;
        public RelayCommand CaptureCommand => _captureCommand ??= new RelayCommand(() => UserActionStart(), () => _canCancelOperation);

        private void UserActionStart()
        {
            if (libData.DataType == LibraryItemType.edit)
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
                DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.changed, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
            }
            Winstance.StartCapture(libData.LivelyInfoFolderPath);
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??= new RelayCommand(() => Winstance.Exit(), () => _canCancelOperation);

        private void WInstance_ThumbnailUpdated(object sender, string path)
        {
            libData.ImagePath = null;
            libData.ImagePath = path;
            libData.LivelyInfo.Thumbnail = libData.LivelyInfo.IsAbsolutePath ? path : Path.GetFileName(path);
            libData.ThumbnailPath = path;
            DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.changed, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
        }

        private void WInstance_PreviewUpdated(object sender, string path)
        {
            libData.ImagePath = null;
            libData.ImagePath = path;
            libData.LivelyInfo.Preview = libData.LivelyInfo.IsAbsolutePath ? path : Path.GetFileName(path);
            libData.PreviewClipPath = path;
            DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.changed, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
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
                libData.DataType = LibraryItemType.ready;
                //libraryVm.SortLibraryItem((LibraryModel)libData);
                DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.done, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
            }
            else
            {
                //user close or 'x' btn press..
                if (libData.DataType == LibraryItemType.edit)
                {
                    //Not required to restore data from memory since "done" just reloads from disk anyway ignoring "Info"..
                    DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.done, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
                }
                else
                {
                    DetailsUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.remove, Info = libData.LivelyInfo, InfoPath = libData.LivelyInfoFolderPath });
                }
            }

            Winstance.CaptureProgress -= WInstance_CaptureProgress;
            Winstance.PreviewUpdated -= WInstance_PreviewUpdated;
            Winstance.ThumbnailUpdated -= WInstance_ThumbnailUpdated;
            Winstance.WallpaperAttached -= WInstance_WallpaperAttached;
        }

        #endregion interface methods
    }
}
