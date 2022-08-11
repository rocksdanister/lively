using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Gallery.Client;
using Lively.UI.WinUI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class ManageAccountViewModel : ObservableObject
    {
        private readonly ResourceLoader i18n;
        private readonly GalleryClient galleryClient;
        private readonly IDialogService dialogService;

        public ManageAccountViewModel(GalleryClient galleryClient, IDialogService dialogService)
        {
            this.galleryClient = galleryClient;
            this.dialogService = dialogService;
            i18n = ResourceLoader.GetForViewIndependentUse();

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
            var choice = await dialogService.ShowDialog(i18n.GetString("GalleryAccountDeleteConfirm/Text"),
                                                        i18n.GetString("PleaseWait/Text"),
                                                        i18n.GetString("GalleryAccountDelete/Content"),
                                                        i18n.GetString("Cancel/Content"),
                                                        false);
            if (choice == IDialogService.DialogResult.primary)
            {
                var response = await galleryClient.DeleteAccountAsync();
                if (response != null) //fail
                {
                    await dialogService.ShowDialog(i18n.GetString("GalleryAccountDeleteFail/Text"),
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
