using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common.Services;
using Lively.Gallery.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class ManageAccountViewModel : ObservableObject
    {
        private readonly IResourceService i18n;
        private readonly GalleryClient galleryClient;
        private readonly IDialogService dialogService;

        public ManageAccountViewModel(GalleryClient galleryClient, IDialogService dialogService, IResourceService i18n)
        {
            this.galleryClient = galleryClient;
            this.dialogService = dialogService;
            this.i18n = i18n;

            if (!galleryClient.IsLoggedIn)
                return;

            try
            {
                DisplayName = galleryClient.CurrentUser.DisplayName;
                Picture = new Uri(galleryClient.CurrentUser.AvatarUrl);
            }
            catch
            {
                //sad
            }
        }

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private Uri picture;

        [ObservableProperty]
        private string displayName;

        private RelayCommand _exportAccountCommand;
        public RelayCommand ExportAccountCommand =>
            _exportAccountCommand ??= new RelayCommand(() => Debug.WriteLine("Export account command"));

        private RelayCommand _logoutAccountCommand;
        public RelayCommand LogoutAccountCommand =>
            _logoutAccountCommand ??= new RelayCommand(async() => await galleryClient.LogoutAsync());

        private RelayCommand _deleteAccountCommand;
        public RelayCommand DeleteAccountCommand =>
            _deleteAccountCommand ??= new RelayCommand(async () => await DeleteAccount());

        private async Task DeleteAccount()
        {
            //IsProcessing = true;
            var choice = await dialogService.ShowDialogAsync(i18n.GetString("GalleryAccountDeleteConfirm/Text"),
                                                        i18n.GetString("PleaseWait/Text"),
                                                        i18n.GetString("GalleryAccountDelete/Content"),
                                                        i18n.GetString("Cancel/Content"),
                                                        false);
            if (choice == DialogResult.primary)
            {
                var response = await galleryClient.DeleteAccountAsync();
                if (response != null) //fail
                {
                    await dialogService.ShowDialogAsync(i18n.GetString("GalleryAccountDeleteFail/Text"),
                                                   i18n.GetString("TextError"),
                                                   i18n.GetString("TextOK"));
                }
                else
                {
                    //LoggedOut event fires to update Auth state.
                }
            }
            else
            {
                //IsProcessing = false;
            }
        }
    }
}
