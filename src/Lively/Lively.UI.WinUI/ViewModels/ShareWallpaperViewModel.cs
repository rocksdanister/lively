using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Gallery.Client;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class ShareWallpaperViewModel : ObservableObject
    {
        private readonly GalleryClient galleryClient;
        private readonly LibraryViewModel libraryVm;

        public ShareWallpaperViewModel(ILibraryModel obj)
        {
            this.Model = obj;
            galleryClient = App.Services.GetRequiredService<GalleryClient>();
            libraryVm = App.Services.GetRequiredService<LibraryViewModel>();

            if (galleryClient.IsLoggedIn)
                canUploadFile = true;

#if DEBUG != true
            canUploadFile = false;
#endif
        }

        [ObservableProperty]
        private ILibraryModel model;

        private bool canExportFile = true;
        private RelayCommand _exportFileCommand;
        public RelayCommand ExportFileCommand =>
            _exportFileCommand ??= new RelayCommand(async () => await ExportFile(), () => canExportFile);

        private bool canUploadFile = false;
        private RelayCommand _galleryFileUploadCommand;
        public RelayCommand GalleryFileUploadCommand =>
            _galleryFileUploadCommand ??= new RelayCommand(async () => await UploadFile(), () => canUploadFile);

        private bool canCopyLink = false;
        private RelayCommand _copyLinkCommand;
        public RelayCommand CopyLinkCommand =>
           _copyLinkCommand ??= new RelayCommand(async () => await CopyLink(), () => canCopyLink);

        private async Task ExportFile()
        {
            try
            {
                canExportFile = false;
                ExportFileCommand.NotifyCanExecuteChanged();

                var filePicker = new FileSavePicker();
                filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
                filePicker.FileTypeChoices.Add("Compressed archive", new List<string>() { ".zip" });
                filePicker.SuggestedFileName = Model.Title;
                var file = await filePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await libraryVm.WallpaperExport(Model, file.Path);
                    FileOperations.OpenFolder(file.Path);
                }
            }
            catch (Exception)
            {
                //TODO
            }
            finally
            {
                canExportFile = true;
                ExportFileCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task UploadFile()
        {
            var tempFile = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName() + ".zip");
            try
            {
                canUploadFile = false;
                GalleryFileUploadCommand.NotifyCanExecuteChanged();

                await libraryVm.WallpaperExport(Model, tempFile);
                using var fs = new FileStream(tempFile, FileMode.Open);
                await galleryClient.UploadWallpaperAsync(fs);
            }
            finally
            {
                canUploadFile = true;
                GalleryFileUploadCommand.NotifyCanExecuteChanged();

                try
                {
                    File.Delete(tempFile);
                }
                catch { }
            }
        }

        private async Task CopyLink()
        {
            //TODO
        }
    }
}
