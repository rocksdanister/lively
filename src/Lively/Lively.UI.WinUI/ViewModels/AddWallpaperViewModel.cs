using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using UAC = UACHelper.UACHelper;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class AddWallpaperViewModel : ObservableObject
    {
        public event EventHandler<List<string>> OnRequestAddFile;
        public event EventHandler<string> OnRequestAddUrl;
        public event EventHandler OnRequestOpenCreate;

        private readonly IUserSettingsClient userSettings;

        public AddWallpaperViewModel(IUserSettingsClient userSettings)
        {
            this.userSettings = userSettings;

            IsElevated = UAC.IsElevated;
            WebUrlText = userSettings.Settings.SavedURL;
        }

        public void UpdateSettingsConfigFile()
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<ISettingsModel>();
            });
        }

        [ObservableProperty]
        private string webUrlText;

        [ObservableProperty]
        private string errorMessage;

        public bool IsElevated { get; }

        private RelayCommand _browseWebCommand;
        public RelayCommand BrowseWebCommand => _browseWebCommand ??= new RelayCommand(WebBrowseAction);

        private RelayCommand _createWallpaperCommand;
        public RelayCommand CreateWallpaperCommand => _createWallpaperCommand ??= 
            new RelayCommand(()=> OnRequestOpenCreate?.Invoke(this, EventArgs.Empty));

        private void WebBrowseAction()
        {
            Uri uri;
            try
            {
                uri = LinkHandler.SanitizeUrl(WebUrlText);
            }
            catch
            {
                return;
            }

            WebUrlText = uri.OriginalString;
            userSettings.Settings.SavedURL = WebUrlText;
            UpdateSettingsConfigFile();

            AddWallpaperLink(uri);
        }

        public void AddWallpaperLink(Uri uri) => OnRequestAddUrl?.Invoke(this, uri.OriginalString);

        private RelayCommand _browseFileCommand;
        public RelayCommand BrowseFileCommand => _browseFileCommand ??= new RelayCommand(async () => await FileBrowseAction());

        private async Task FileBrowseAction()
        {
            ErrorMessage = null;
            var files = await FilePickerUtil.PickLivelyWallpaperMultipleFile();

            if (files.Count > 0)
            {
                if (files.Count == 1)
                    AddWallpaperFile(files[0]);
                else
                    AddWallpaperFiles(files.ToList());
            }
        }

        public void AddWallpaperFile(string path) => OnRequestAddFile?.Invoke(this, new List<string>() { path });

        public void AddWallpaperFiles(List<string> filePaths) => OnRequestAddFile?.Invoke(this, filePaths);
    }
}