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

namespace Lively.UI.WinUI.ViewModels
{
    public partial class AddWallpaperCreateViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<AddWallpaperCreateModel> wallpaperCategories = new();
        [ObservableProperty]
        private AdvancedCollectionView wallpaperCategoriesFiltered;
        [ObservableProperty]
        private AddWallpaperCreateModel selectedItem;

        public AddWallpaperCreateViewModel()
        {
            WallpaperCategoriesFiltered = new AdvancedCollectionView(WallpaperCategories, true);

            WallpaperCategories.Add(new AddWallpaperCreateModel()
            {
                Title = "Open",
                Description = "Create a simple wallpaper",
                TypeSupported = WallpaperType.picture,
                CreateType = WallpaperCreateType.none,
                Icon = "ms-appx:///Assets/icons8-wallpaper-96.png"
            });
            WallpaperCategories.Add(new AddWallpaperCreateModel()
            {
                Title = "Depth Wallpaper",
                Description = "Using AI transform photographs into 3D",
                CreateType = WallpaperCreateType.depthmap,
                TypeSupported = WallpaperType.picture,
                Icon = "ms-appx:///Assets/icons8-artificial-intelligence-100.png"
            });
            //WallpaperCategories.Add(new AddWallpaperCreateModel()
            //{
            //    Title = "Edit Video",
            //    Description = "Transcode, trim or optimize video",
            //    CreateType = WallpaperCreateType.none,
            //    TypeSupported = WallpaperType.video,
            //    Icon = null
            //});

            SelectedItem = WallpaperCategories.FirstOrDefault();
        }
    }
}
