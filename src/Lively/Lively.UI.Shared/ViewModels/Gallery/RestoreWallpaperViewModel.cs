using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Gallery.Client;
using Lively.Models;
using Lively.UI.WinUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class RestoreWallpaperViewModel : ObservableObject
    {
        public List<GalleryModel> SelectedItems => Wallpapers.Where(x => x.IsSelected).ToList();

        private readonly GalleryClient galleryClient;
        private readonly LibraryViewModel libraryVm;

        public RestoreWallpaperViewModel(LibraryViewModel libraryVm, GalleryClient galleryClient)
        {
            this.galleryClient = galleryClient;
            this.libraryVm = libraryVm;

            if (!galleryClient.IsLoggedIn)
                return;

            //Passed to view constructor to avoid fetching multiple times.
            //_ = PopulateWallpapers();
        }

        [ObservableProperty]
        private ObservableCollection<GalleryModel> wallpapers = new();

        [ObservableProperty]
        private bool isLoading = true;

        private async Task PopulateWallpapers()
        {
            var subsDto = await galleryClient.GetWallpaperSubscriptions();
            var existingDto = subsDto.FindAll(x => libraryVm.LibraryItems.Any(y => y.LivelyInfo.Id == x.Id));
            var missingDto = subsDto.Except(existingDto);
            foreach (var item in missingDto)
            {
                Wallpapers.Add(new GalleryModel(item, false));
            }
            IsLoading = false;
        }

        private RelayCommand _selectAllCommand;
        public RelayCommand SelectAllCommand =>
            _selectAllCommand ??= new RelayCommand(() => {
                foreach (var item in Wallpapers)
                {
                    item.IsSelected = true;
                }
            });

        private RelayCommand _selectNoneCommand;
        public RelayCommand SelectNoneCommand =>
            _selectNoneCommand ??= new RelayCommand(() => {
                foreach (var item in Wallpapers)
                {
                  item.IsSelected = false;
                }
            });
    }
}
