using livelywpf.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace livelywpf
{
    class LibraryPreviewViewModel : ObservableObject
    {
        //private readonly LivelyInfoModel Data = new LivelyInfoModel();
        readonly private LibraryModel libData;
        public LibraryPreviewViewModel(IWallpaper wallpaper)
        {
            libData = wallpaper.GetWallpaperData();
            //Data.Type = libData.LivelyInfo.Type;
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

        #region data

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
