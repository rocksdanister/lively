using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using Lively.Common.Services;
using Lively.Models;
using Lively.Models.Enums;
using System.Collections.ObjectModel;

namespace Lively.UI.Shared.ViewModels
{
    public partial class AddWallpaperCreateViewModel : ObservableObject
    {
        private readonly IResourceService i18n;

        [ObservableProperty]
        private ObservableCollection<AddWallpaperCreateModel> wallpaperCategories = new();
        [ObservableProperty]
        private AdvancedCollectionView wallpaperCategoriesFiltered;
        [ObservableProperty]
        private AddWallpaperCreateModel selectedItem;

        public AddWallpaperCreateViewModel(IResourceService i18n)
        {
            this.i18n = i18n;

            WallpaperCategoriesFiltered = new AdvancedCollectionView(WallpaperCategories, true);

            WallpaperCategories.Add(new AddWallpaperCreateModel()
            {
                Title = i18n.GetString("TextOpen/Content"),
                Description = i18n.GetString("TitleCreateWallpaperOpenItem/Description"),
                TypeSupported = WallpaperType.picture,
                CreateType = WallpaperCreateType.none,
                Icon = "ms-appx:///Assets/icons8-wallpaper-96.png"
            });
            WallpaperCategories.Add(new AddWallpaperCreateModel()
            {
                Title = i18n.GetString("TitleDepthWallpaperItem/Content"),
                Description =  i18n.GetString("DescriptionDepthWallpaperItem/Content"),
                CreateType = WallpaperCreateType.depthmap,
                TypeSupported = WallpaperType.picture,
                Icon = "ms-appx:///Assets/icons8-landscape-64.png"
            });
            //WallpaperCategories.Add(new AddWallpaperCreateModel()
            //{
            //    Title = "Edit Video",
            //    Description = "Transcode, trim or optimize video",
            //    CreateType = WallpaperCreateType.videotranscode,
            //    TypeSupported = WallpaperType.video,
            //    Icon = null
            //});

            //SelectedItem = WallpaperCategories.FirstOrDefault();
        }
    }
}
