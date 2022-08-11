using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        private bool updateAvailable;
        private readonly IAppUpdaterClient appUpdater;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IHttpClientFactory httpClientFactory;
        ResourceLoader languageResource;

        public AboutViewModel(IAppUpdaterClient appUpdater, IDesktopCoreClient desktopCore, IHttpClientFactory httpClientFactory)
        {
            this.appUpdater = appUpdater;
            this.desktopCore = desktopCore;
            this.httpClientFactory = httpClientFactory;
            languageResource = ResourceLoader.GetForViewIndependentUse();

            MenuUpdate(appUpdater.Status, appUpdater.LastCheckTime, appUpdater.LastCheckVersion);
            appUpdater.UpdateChecked += AppUpdater_UpdateChecked;
        }

        public bool IsNotWinStore => !Constants.ApplicationType.IsMSIX;

        public string AppVersionText => "v" + desktopCore.AssemblyVersion +
                (Constants.ApplicationType.IsTestBuild ? "b" : (Constants.ApplicationType.IsMSIX ? $" {languageResource.GetString("Store/Header")}" : string.Empty));

        private string _updateStatusText;
        public string UpdateStatusText
        {
            get { return _updateStatusText; }
            set
            {
                _updateStatusText = value;
                OnPropertyChanged();
            }
        }

        private string _updateDateText;
        public string UpdateDateText
        {
            get { return _updateDateText; }
            set
            {
                _updateDateText = value;
                OnPropertyChanged();
            }
        }

        private string _updateCommandText;
        public string UpdateCommandText
        {
            get { return _updateCommandText; }
            set
            {
                _updateCommandText = value;
                OnPropertyChanged();
            }
        }

        private bool canUpdateAppCommand = true;
        private RelayCommand _updateAppCommand;
        public RelayCommand UpdateAppCommand => _updateAppCommand ??= new RelayCommand(CheckUpdate, () => canUpdateAppCommand);

        private void CheckUpdate()
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(async () =>
            {
                if (updateAvailable)
                {
                    await appUpdater.StartUpdate();
                }
                else
                {
                    try
                    {
                        canUpdateAppCommand = false;
                        _updateAppCommand.NotifyCanExecuteChanged();
                        await appUpdater.CheckUpdate();
                        MenuUpdate(appUpdater.Status, appUpdater.LastCheckTime, appUpdater.LastCheckVersion);
                    }
                    finally
                    {
                        canUpdateAppCommand = true;
                        _updateAppCommand.NotifyCanExecuteChanged();
                    }
                }
            });
        }

        private void AppUpdater_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                MenuUpdate(e.UpdateStatus, e.UpdateDate, e.UpdateVersion);
            });
        }

        private void MenuUpdate(AppUpdateStatus status, DateTime date, Version version)
        {
            switch (status)
            {
                case AppUpdateStatus.uptodate:
                    updateAvailable = false;
                    UpdateStatusText = languageResource.GetString("TextUpdateUptodate");
                    break;
                case AppUpdateStatus.available:
                    updateAvailable = true;
                    UpdateStatusText = $"{languageResource.GetString("DescriptionUpdateAvailable")} (v{version})";
                    break;
                case AppUpdateStatus.invalid:
                    updateAvailable = false;
                    UpdateStatusText = "This software has unique version tag~";
                    break;
                case AppUpdateStatus.notchecked:
                    updateAvailable = false;
                    UpdateStatusText = languageResource.GetString("TextUpdateChecking");
                    break;
                case AppUpdateStatus.error:
                    updateAvailable = false;
                    UpdateStatusText = languageResource.GetString("TextupdateCheckFail");
                    break;
            }
            UpdateDateText = status == AppUpdateStatus.notchecked ? $"{languageResource.GetString("TextLastChecked")}: ---" : $"{languageResource.GetString("TextLastChecked")}: {date}";
            UpdateCommandText = updateAvailable ? languageResource.GetString("TextDownload") : languageResource.GetString("TextUpdateCheck");
        }

        public string _patreonMembers;
        public string PatreonMembers
        {
            get => _patreonMembers;
            set
            {
                _patreonMembers = value;
                OnPropertyChanged();
            }
        }

        private async Task<string> GetPatreonMembers()
        {
            try
            {
                using var client = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/wiki/rocksdanister/lively/Patreon.md");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public async void OnPatreonLoaded(object sender, RoutedEventArgs e)
            => PatreonMembers = await GetPatreonMembers();

        public void OnWindowClosing(object sender, RoutedEventArgs e)
            => appUpdater.UpdateChecked -= AppUpdater_UpdateChecked;
    }
}
