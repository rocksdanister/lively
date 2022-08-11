using Lively.Common.Helpers.MVVM;
using Lively.Models.Gallery.API;

namespace Lively.Models
{
    public class GalleryModel : ObservableObject
    {
        public GalleryModel(WallpaperDto data, bool isInstalled)
        {
            LivelyInfo = new LivelyInfoModel()
            {
                Title = data.Title,
                Desc = data.Description,
                Thumbnail = data.Thumbnail,
                Preview = data.Preview,
                Contact = data.Contact,
                AppVersion = data.AppVersion,
                Id = data.Id,
            };
            //Image = data.Preview ?? data.Thumbnail;
            this.IsInstalled = isInstalled;
        }

        public GalleryModel(LivelyInfoModel data, bool isInstalled)
        {
            LivelyInfo = new LivelyInfoModel(data);
            //Image = data.Preview ?? data.Thumbnail;
            this.IsInstalled = isInstalled;
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
                OnPropertyChanged();
            }
        }

        private string _image;
        public string Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get { return _isDownloading; }
            set { _isDownloading = value; OnPropertyChanged(); }
        }

        private float _downloadingProgress;
        public float DownloadingProgress
        {
            get { return _downloadingProgress; }
            set { _downloadingProgress = value; OnPropertyChanged(); }
        }

        private string _downloadingProgressText = "-/- MB";
        public string DownloadingProgressText
        {
            get => _downloadingProgressText;
            set
            {
                _downloadingProgressText = value;
                OnPropertyChanged();
            }
        }

        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set { _isInstalled = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
