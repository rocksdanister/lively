using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage.Provider;

namespace livelywpf
{
    public enum LibraryTileType
    {
        [Description("Converting to mp4")]
        videoConvert,
        [Description("To be added to library")]
        processing,
        installing,
        downloading,
        [Description("Ready to be used")]
        ready
    }

    [Serializable]
    public class LibraryModel : ObservableObject
    {
        public LibraryModel(LivelyInfoModel data, string folderPath, LibraryTileType tileType = LibraryTileType.ready)
        {
            DataType = tileType;
            LivelyInfo = new LivelyInfoModel(data);
            Title = data.Title;
            Desc = data.Desc;
            WpType = data.Type.ToString(); //todo: this is just for testing, final sent the translated text.
            SrcWebsite = GetUri(data.Contact, "https");

            if (data.IsAbsolutePath)
            {
                FilePath = data.FileName;
                PreviewClipPath = data.Preview;
                ThumbnailPath = data.Thumbnail;
            }
            else
            {
                if (data.Type == WallpaperType.url 
                || data.Type == WallpaperType.videostream)
                {
                    //no file.
                    FilePath = data.FileName;
                }
                else
                {
                    try
                    {
                        FilePath = Path.Combine(folderPath, data.FileName);
                    }
                    catch
                    {
                        FilePath = null;
                    }
                }

                try
                {
                    PreviewClipPath = Path.Combine(folderPath, data.Preview);
                }
                catch
                {
                    PreviewClipPath = null;
                }

                try
                {
                    ThumbnailPath = Path.Combine(folderPath, data.Thumbnail);
                }
                catch
                {
                    ThumbnailPath = null;
                }
            }
            ImagePath = File.Exists(PreviewClipPath) ? PreviewClipPath : ThumbnailPath;
            LivelyInfoFolderPath = folderPath;
        }

        private LivelyInfoModel _livelyInfo;
        public LivelyInfoModel LivelyInfo
        {
            get
            {
                return _livelyInfo;
            }
            set
            {
                _livelyInfo = value;
                OnPropertyChanged("LivelyInfo");
            }
        }

        private LibraryTileType _dataType;
        public LibraryTileType DataType
        {
            get { return _dataType; }
            set
            {
                _dataType = value;
                OnPropertyChanged("DataType");
            }
        }

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (LivelyInfo.Type == WallpaperType.url 
                || LivelyInfo.Type == WallpaperType.videostream)
                {
                    _filePath = value;
                }
                else
                {
                    _filePath = File.Exists(value) ? value : null;
                }
                OnPropertyChanged("FilePath");
            }
        }

        private string _livelyInfoFolderPath;
        public string LivelyInfoFolderPath
        {
            get { return _livelyInfoFolderPath; }
            set
            {
                _livelyInfoFolderPath = value;
                OnPropertyChanged("LivelyInfoFolderPath");
            }
        }

        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }   
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        private string _desc;
        public string Desc
        {
            get
            {
                return _desc;
            }
            set
            {
                _desc = value;
                OnPropertyChanged("Desc");
            }
        }

        private string _imagePath;
        public string ImagePath
        {
            get
            {
                return _imagePath;
            }
            set
            {
                _imagePath = value;
                OnPropertyChanged("ImagePath");
            }
        }

        private string _previewClipPath;
        public string PreviewClipPath
        {
            get
            {
                return _previewClipPath;
            }
            set
            {
                _previewClipPath = File.Exists(value) ? value : null;
                OnPropertyChanged("PreviewClipPath");
            }
        }

        private string _thumbnailPath;
        public string ThumbnailPath
        {
            get
            {
                return _thumbnailPath;
            }
            set
            {
                _thumbnailPath = File.Exists(value) ? value : null;
                OnPropertyChanged("ThumbnailPath");
            }
        }

        private Uri _srcWebsite;
        public Uri SrcWebsite
        {
            get
            {
                return _srcWebsite;
            }
            set
            {
                _srcWebsite = value;
                OnPropertyChanged("SrcWebsite");
            }
        }

        private string _wpType;
        public string WpType
        {
            get
            {
                return _wpType;
            }
            set
            {
                _wpType = value;
                OnPropertyChanged("WpType");
            }
        }

        #region helpers
        public Uri GetUri(string s, string scheme)
        {
            try
            {
                return new UriBuilder(s)
                {
                    Scheme = scheme,
                    Port = -1,
                }.Uri;
            }
            catch (ArgumentNullException)
            {
                return null;
            }
            catch (UriFormatException)
            {
                return null;
            }
        }
        #endregion helpers
    }
}
