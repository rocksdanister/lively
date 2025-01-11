using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAC = UACHelper.UACHelper;

namespace Lively.UI.Shared.ViewModels
{
    public partial class AddWallpaperViewModel : ObservableObject
    {
        public event EventHandler<List<string>> OnRequestAddFile;
        public event EventHandler<string> OnRequestAddUrl;
        public event EventHandler OnRequestOpenCreate;

        private readonly IUserSettingsClient userSettings;
        private readonly IDispatcherService dispatcher;
        private readonly IFileService fileService;

        public AddWallpaperViewModel(IUserSettingsClient userSettings, IDispatcherService dispatcher, IFileService fileService)
        {
            this.userSettings = userSettings;
            this.dispatcher = dispatcher;
            this.fileService = fileService;

            IsElevated = UAC.IsElevated;
            WebUrlText = userSettings.Settings.SavedURL;
        }

        public void UpdateSettingsConfigFile()
        {
            dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
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
            if (!LinkUtil.TrySanitizeUrl(WebUrlText, out Uri uri))
                return;

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
            var files = await fileService.PickWallpaperFile(true);

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