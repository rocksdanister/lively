using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models.Gallery.API;

namespace Lively.Models
{
    public partial class GalleryModel : ObservableObject
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

        [ObservableProperty]
        private LivelyInfoModel livelyInfo;

        [ObservableProperty]
        private string image;

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private float downloadingProgress;

        [ObservableProperty]
        private string downloadingProgressText = "-/- MB";

        [ObservableProperty]
        private bool isInstalled;

        [ObservableProperty]
        private bool isSelected;
    }
}
