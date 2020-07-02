using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage.Provider;

namespace livelywpf
{
    [Serializable]
    public class LibraryModel : ObservableObject
    {
        public LibraryModel(LivelyInfoModel data, string folderPath)
        {
            LivelyInfo = new LivelyInfoModel(data);
            Title = data.Title;
            Desc = data.Desc;
            WpType = data.Type.ToString(); //todo: this is just for testing, final sent the translated text.
            SrcWebsite = GetUri(data.Contact, "https");

            if (data.IsAbsolutePath)
            {
                FilePath = data.FileName;

                if (File.Exists(data.Preview))
                {
                    ImagePath = data.Preview;
                }
                else
                {
                    ImagePath = data.Thumbnail;
                }
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

                try
                {
                    if (data.Preview != null)
                    {
                        var imgPath = Path.Combine(folderPath, data.Preview);
                        if(File.Exists(imgPath))
                        {
                            ImagePath = imgPath;
                        }
                    }
                    else
                    {
                        ImagePath = Path.Combine(folderPath, data.Thumbnail);
                    }
                }
                catch
                {
                    ImagePath = null;
                }
            }
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

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (File.Exists(value))
                {
                    _filePath = value;
                }
                else
                {
                    _filePath = null;
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
        private Uri GetUri(string s, string scheme)
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
