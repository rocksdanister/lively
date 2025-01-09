using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models.Enums;

namespace Lively.Models
{
    public partial class AddWallpaperCreateModel : ObservableObject
    {
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string icon;

        [ObservableProperty]
        private WallpaperType typeSupported;

        [ObservableProperty]
        private WallpaperCreateType createType;
    }

    public enum WallpaperCreateType
    {
        none,
        depthmap,
        //videotranscode,
    }
}
