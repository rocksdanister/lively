using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class AddWallpaperCreateViewModel : ObservableObject
    {
        private readonly ResourceLoader i18n;

        [ObservableProperty]
        private ObservableCollection<AddWallpaperCreateModel> wallpaperCategories = new();
        [ObservableProperty]
        private AdvancedCollectionView wallpaperCategoriesFiltered;
        [ObservableProperty]
        private AddWallpaperCreateModel selectedItem;

        public AddWallpaperCreateViewModel()
        {
            i18n = ResourceLoader.GetForViewIndependentUse();

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
