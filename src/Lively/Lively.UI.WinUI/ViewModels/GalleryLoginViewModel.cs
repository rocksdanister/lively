using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Gallery.Client;
using Lively.Models;
using Lively.UI.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class GalleryLoginViewModel : ObservableObject
    {
        private readonly ResourceLoader i18n;
        private readonly GalleryClient galleryClient;
        private readonly IDialogService dialogService;
        private readonly LibraryViewModel libraryVm;

        public GalleryLoginViewModel(GalleryClient galleryClient, IDialogService dialogService, LibraryViewModel libraryVm)
        {
            this.galleryClient = galleryClient;
            this.dialogService = dialogService;
            this.libraryVm = libraryVm;
            i18n = ResourceLoader.GetForViewIndependentUse();

            Message = i18n.GetString("GalleryPleaseLogin/Text");
#if DEBUG
            IsOpen = true;
#endif
        }

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private bool isOpen = false;

        private RelayCommand _authGoogleCommand;
        public RelayCommand AuthGoogleCommand =>
            _authGoogleCommand ??= new RelayCommand(async () => await Authenticate("GOOGLE"));

        private RelayCommand _authGithubCommand;
        public RelayCommand AuthGithubCommand =>
            _authGithubCommand ??= new RelayCommand(async () => await Authenticate("GITHUB"));

        [ObservableProperty]
        private Uri picture;

        [ObservableProperty]
        private string message;

        private async Task Authenticate(string provider)
        {
            if (galleryClient.IsLoggedIn)
                return;

            IsProcessing = true;
            Exception exception = null;
            await Task.Run(async () =>
            {
                try
                {
                    switch (provider)
                    {
                        case "GOOGLE":
                            {
                                var code = await galleryClient.RequestCodeAsync("GOOGLE");
                                if (code == null)
                                    throw new Exception("Auth code value cannot be null.");

                                await galleryClient.AuthenticateGoogleAsync(code);
                            }
                            break;
                        case "GITHUB":
                            {
                                var code = await galleryClient.RequestCodeAsync("GITHUB");
                                if (code == null)
                                    throw new Exception("Auth code value cannot be null.");

                                await galleryClient.AuthenticateGithubAsync(code);
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
            });

            if (exception != null)
            {
                await dialogService.ShowDialog(exception.ToString(), i18n.GetString("TextError"), i18n.GetString("TextClose"));
                IsProcessing = false;
            }
            else
            {
                try
                {
                    Message = $"{i18n.GetString("GalleryWelcomeMessage/Text")} {galleryClient.CurrentUser.DisplayName}";
                    Picture = new Uri(galleryClient.CurrentUser.AvatarUrl);
                }
                catch
                {
                    //sad
                }

                //Show restore dialog everytime user login and if any subbed wallpaper missing
                await Task.Delay(3500);
                if (galleryClient.IsLoggedIn)
                {
                    await RestoreSubscribedWallpapers();
                }
            }
        }

        private async Task RestoreSubscribedWallpapers()
        {
            var subsDto = await galleryClient.GetWallpaperSubscriptions();
            var existingDto = subsDto.FindAll(x => libraryVm.LibraryItems.Any(y => y.LivelyInfo.Id == x.Id));
            var missingDto = subsDto.Except(existingDto);
            if (missingDto.Any())
            {
                var vm = App.Services.GetRequiredService<RestoreWallpaperViewModel>();
                foreach (var item in missingDto)
                {
                    vm.Wallpapers.Add(new GalleryModel(item, false) { IsSelected = true });
                }
                var result = await dialogService.ShowDialog(
                    new Views.Pages.Gallery.RestoreWallpaperView(vm),
                    i18n.GetString("TitleWelcomeback/Text"),
                    i18n.GetString("TextDownloadNow/Content"),
                    i18n.GetString("TextMaybeLater/Content"));
                if (result == IDialogService.DialogResult.primary)
                {
                    foreach (var item in vm.SelectedItems)
                    {
                        _ = libraryVm.AddWallpaperGallery(item);
                    }
                }
            }
        }
    }
}
