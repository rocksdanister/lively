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

namespace livelywpf
{
    class LibraryPreviewViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly LibraryModel libData;
        private readonly ILibraryPreview Winstance;
        private readonly LivelyInfoModel livelyInfoCopy;
        //private readonly string thumbnailPathCopy;

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
                //capture loop is disabled for now..
                //thumbnailPathCopy = libData.ThumbnailPath;
                //if (libData.ThumbnailPath != null)
                //{
                //    try
                //    {
                //        File.Copy(libData.ThumbnailPath, 
                //            Path.Combine(Program.AppDataDir, "temp", Path.GetFileName(libData.ThumbnailPath)));
                //    }
                //    catch (Exception e)
                //    {
                //        Logger.Error(e.ToString());
                //    }
                //}

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
                }
                else if (libData.LivelyInfo.Type == WallpaperType.url
                || libData.LivelyInfo.Type == WallpaperType.web
                || libData.LivelyInfo.Type == WallpaperType.webaudio)
                {
                    if (libData.LivelyInfo.Type == WallpaperType.url)
                        Url = libData.FilePath;

                    try
                    {
                        if (wallpaper.GetProcess() != null)
                        {
                            //wallpaper.GetProcess().Refresh();
                            Title = wallpaper.GetProcess().MainWindowTitle;
                        }
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

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            CleanUp();
        }

        #endregion ui

        #region interface methods

        private void WInstance_WallpaperAttached(object sender, EventArgs e)
        {
            if (libData.DataType == LibraryTileType.cmdImport || 
                libData.DataType == LibraryTileType.multiImport)
            {
                Winstance.StartCapture(libData.LivelyInfoFolderPath);
            }
            else if (libData.DataType == LibraryTileType.edit)
            {
                //no thumbnail capture timer..
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
                    //if (thumbnailPathCopy != null)
                    //{
                    //    try
                    //    {
                    //        File.Delete(libData.LivelyInfo.Thumbnail);
                    //        //temp -> wp folder..
                    //        File.Copy(thumbnailPathCopy, libData.ThumbnailPath, true);

                    //        libData.LivelyInfo.Thumbnail = libData.ThumbnailPath;
                    //        libData.ThumbnailPath = libData.ThumbnailPath;

                    //        libData.ImagePath = null;
                    //        //Use animated gif if exists.
                    //        libData.ImagePath = Program.SettingsVM.Settings.LivelyGUIRendering == LivelyGUIState.normal ?
                    //            (File.Exists(libData.PreviewClipPath) ? libData.PreviewClipPath : libData.ThumbnailPath) : libData.ThumbnailPath;
                    //    }
                    //    catch(Exception e)
                    //    {
                    //        Logger.Error(e.ToString());
                    //    }
                    //}

                    //restore previous data..
                    Title = livelyInfoCopy.Title;
                    Desc = livelyInfoCopy.Desc;
                    Author = livelyInfoCopy.Author;
                    Url = livelyInfoCopy.Contact;

                    //change from pos 0..
                    libData.DataType = LibraryTileType.ready;
                    Program.LibraryVM.SortLibraryItem(libData);
                }
                else
                {
                    //nothing, core will terminate and delete the wp folder when LivelyInfo.json not found..
                }
            }

            //Use animated gif if possible, if user checked create no preview but preview already exists..
            if (libData.DataType == LibraryTileType.edit)
            {
                libData.ImagePath = null;
                libData.ImagePath = Program.SettingsVM.Settings.LivelyGUIRendering == LivelyGUIState.normal ?
                    (File.Exists(libData.PreviewClipPath) ? libData.PreviewClipPath : libData.ThumbnailPath) : libData.ThumbnailPath;
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
