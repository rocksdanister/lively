using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using Lively.Common.Services;
using Lively.Gallery.Client;
using Lively.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class GallerySubscriptionViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<GalleryModel> wallpapers = new();
        [ObservableProperty]
        private AdvancedCollectionView wallpapersFiltered;

        private readonly GalleryClient galleryClient;
        private readonly GalleryViewModel galleryVm;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcher;
        private readonly LibraryViewModel libraryVm;

        public GallerySubscriptionViewModel(GalleryClient galleryClient,
            LibraryViewModel libraryVm,
            GalleryViewModel galleryVm,
            IDispatcherService dispatcher,
            IDialogService dialogService)
        {
            this.galleryClient = galleryClient;
            this.dialogService = dialogService;
            this.libraryVm = libraryVm;
            this.dispatcher = dispatcher;
            this.galleryVm = galleryVm;

            if (!galleryClient.IsLoggedIn)
                return;

            WallpapersFiltered = new AdvancedCollectionView(Wallpapers, true);
            WallpapersFiltered.SortDescriptions.Add(new SortDescription("IsInstalled", SortDirection.Ascending));

            galleryClient.LoggedIn += (s, id) =>
            {
                dispatcher.TryEnqueue(async () =>
                {
                    Wallpapers.Clear();
                    await PopulateSubbedWallpapers();
                });
            };

            galleryClient.WallpaperUnsubscribed += (s, id) =>
            {
                var item = Wallpapers.FirstOrDefault(x => x.LivelyInfo.Id == id);
                if (item != null)
                {
                    Wallpapers.Remove(item);
                }
            };

            galleryClient.WallpaperSubscribed += async(s, id) =>
            {
                if (!Wallpapers.Any(x => x.LivelyInfo.Id == id))
                {
                    var item = galleryVm.Wallpapers.FirstOrDefault(x => x.LivelyInfo.Id == id);
                    if (item != null)
                    {
                        var clone = new GalleryModel(item.LivelyInfo, item.IsInstalled);
                        Wallpapers.Add(clone);
                    }
                    else
                    {
                        //Gallerymay did not fetched the item yet
                        var wpDto = await galleryClient.GetWallpaperInfoAsync(id);
                        Wallpapers.Add(new GalleryModel(wpDto, libraryVm.LibraryItems.Any(x => id == x.LivelyInfo.Id)));
                    }
                }
            };

            libraryVm.WallpaperDeleted += (s, id) =>
            {
                var item = Wallpapers.FirstOrDefault(x => x.LivelyInfo.Id == id);
                if (item != null)
                {
                    item.IsInstalled = false;
                }
            };

            libraryVm.WallpaperDownloadCompleted += (s, id) =>
            {
                var item = Wallpapers.FirstOrDefault(x => x.LivelyInfo.Id == id);
                if (item != null)
                {
                    item.DownloadingProgressText = "-/- MB";
                    item.DownloadingProgress = 0;
                    item.IsDownloading = false;
                    item.IsInstalled = true;
                }
            };

            libraryVm.WallpaperDownloadFailed += (s, id) =>
            {
                var item = Wallpapers.FirstOrDefault(x => x.LivelyInfo.Id == id);
                if (item != null)
                {
                    item.DownloadingProgressText = "-/- MB";
                    item.DownloadingProgress = 0;
                    item.IsDownloading = false;
                }
            };

            libraryVm.WallpaperDownloadProgress += (s, e) =>
            {
                var item = Wallpapers.FirstOrDefault(x => x.LivelyInfo.Id == e.Item1);
                if (item != null)
                {
                    item.DownloadingProgressText = $"{e.Item3:0.##}/{e.Item4:0.##} MB";
                    item.DownloadingProgress = e.Item2;
                    item.IsDownloading = true;
                }
            };

            dispatcher.TryEnqueue(async () =>
            {
                await PopulateSubbedWallpapers();
            });
        }

        public async Task PopulateSubbedWallpapers()
        {
            var subsDto = await galleryClient.GetWallpaperSubscriptions();
            foreach (var item in subsDto)
            {
                var libItem = libraryVm.LibraryItems.FirstOrDefault(x => item.Id == x.LivelyInfo.Id);
                Wallpapers.Add(new GalleryModel(item, libItem?.DataType == LibraryItemType.ready) { 
                    IsDownloading = libItem?.IsDownloading ?? false 
                });
            }
        }

        private RelayCommand<GalleryModel> _deleteCommand;
        public RelayCommand<GalleryModel> DeleteCommand =>
            _deleteCommand ??= new RelayCommand<GalleryModel>(async (obj) => {
                var item = libraryVm.LibraryItems.FirstOrDefault(x => x.LivelyInfo.Id == obj.LivelyInfo.Id);
                if (item != null)
                {
                    await libraryVm.WallpaperDelete(item, false);
                }
            });

        private RelayCommand<GalleryModel> _downloadCommand;
        public RelayCommand<GalleryModel> DownloadCommand =>
            _downloadCommand ??= new RelayCommand<GalleryModel>(async (obj) => {
                obj.IsDownloading = true;
                var item = galleryVm.Wallpapers.FirstOrDefault(x => x.LivelyInfo.Id == obj.LivelyInfo.Id); //cached preview
                await libraryVm.AddWallpaperGallery(item ?? obj);
            });

        private RelayCommand<GalleryModel> _unsubscribeCommand;
        public RelayCommand<GalleryModel> UnsubscribeCommand =>
            _unsubscribeCommand ??= new RelayCommand<GalleryModel>(async (obj) =>
            {
                try
                {
                    var item = libraryVm.LibraryItems.FirstOrDefault(x => x.LivelyInfo.Id == obj.LivelyInfo.Id);
                    if (item == null)
                    {
                        await galleryClient.UnsubscribeFromWallpaperAsync(obj.LivelyInfo.Id);
                    }
                    else
                    {
                        //using (WallpapersFiltered.DeferRefresh())
                        //{
                        //    await libraryVm.WallpaperDelete(item, true);
                        //}
                        await libraryVm.WallpaperDelete(item, true);
                    }
                }
                catch (Exception)
                {

                }
            });
    }
}
