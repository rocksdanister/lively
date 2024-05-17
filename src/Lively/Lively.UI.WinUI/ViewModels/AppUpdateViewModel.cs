using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Models;
using Lively.Common.Services.Downloader;
using Lively.Grpc.Client;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class AppUpdateViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IAppUpdaterClient appUpdater;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDownloadService downloader;
        private readonly IDialogService dialogService;
        private readonly ICommandsClient commandsClient;

        private readonly ResourceLoader languageResource;

        public AppUpdateViewModel(IAppUpdaterClient appUpdater,
            IDesktopCoreClient desktopCore,
            IDownloadService downloader,
            ICommandsClient commandsClient,
            IDialogService dialogService)
        {
            this.appUpdater = appUpdater;
            this.desktopCore = desktopCore;
            this.downloader = downloader;
            this.dialogService = dialogService;
            this.commandsClient = commandsClient;

            languageResource = ResourceLoader.GetForViewIndependentUse();

            UpdateState(appUpdater.Status, appUpdater.LastCheckTime, appUpdater.LastCheckVersion);
            appUpdater.UpdateChecked += AppUpdater_UpdateChecked;

            downloader.DownloadProgressChanged += UpdateDownload_DownloadProgressChanged;
            downloader.DownloadFileCompleted += UpdateDownload_DownloadFileCompleted;

            // This is only run once if the main interface is opened before the initial fetchDelay in Core for update check.
            if (appUpdater.Status == AppUpdateStatus.notchecked)
            {
                _ = CheckUpdate();
            }
            else if (appUpdater.Status == AppUpdateStatus.available)
            {
                var fileName = appUpdater.LastCheckFileName ?? appUpdater.LastCheckUri.Segments.Last();
                var filePath = Path.Combine(Constants.CommonPaths.TempDir, fileName);
                IsUpdateDownloaded = File.Exists(filePath);
            }
        }

        public bool IsWinStore => Constants.ApplicationType.IsMSIX;

        public bool IsBetaBuild => Constants.ApplicationType.IsTestBuild;

        public bool IsWebView2Available => WebViewUtil.IsWebView2Available();

        public string AppVersionText
        {
            get
            {
                var ver = "v" + desktopCore.AssemblyVersion;
                if (IsBetaBuild)
                    ver += "(b)";
                else if (IsWinStore)
                    ver += $" {languageResource.GetString("Store/Header")}";
                return ver;
            }
        }

        [ObservableProperty]
        private double currentProgress;

        [ObservableProperty]
        private bool isWebView2Installing;

        [ObservableProperty]
        private bool isUpdateChecking;

        [ObservableProperty]
        private bool isUpdateAvailable;

        [ObservableProperty]
        private bool isUpdateDownloading;

        [ObservableProperty]
        private bool isUpdateDownloaded;

        [ObservableProperty]
        private string updateChangelogError;

        [ObservableProperty]
        private string updateStatusText;

        [ObservableProperty]
        private string updateDateText;

        [ObservableProperty]
        private string updateStatusSeverity = "Warning";

        [ObservableProperty]
        private AppUpdateStatus updateStatus;

        [RelayCommand]
        private async Task CheckUpdate()
        {
            try
            {
                IsUpdateChecking = true;
                await appUpdater.CheckUpdate();
            }
            finally
            {
                IsUpdateChecking = false;
            }
        }

        [RelayCommand]
        private void OpenStorePage()
        {
            LinkUtil.OpenBrowser("ms-windows-store://pdp/?productid=9NTM2QC6QWS7");
        }

        [RelayCommand]
        private async Task DownloadUpdate()
        {
            try
            {
                IsUpdateDownloading = true;

                var fileName = appUpdater.LastCheckFileName ?? appUpdater.LastCheckUri.Segments.Last();
                var filePath = Path.Combine(Constants.CommonPaths.TempDir, fileName);
                Logger.Info($"Downloading update: {filePath}");
                await downloader.DownloadFile(appUpdater.LastCheckUri, filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await dialogService.ShowDialogAsync($"{languageResource.GetString("LivelyExceptionAppUpdateFail")}\n\nException:\n{ex}",
                    languageResource.GetString("TextError"),
                    languageResource.GetString("TextOK"));
            }
            finally
            {
                IsUpdateDownloading = false;
            }
        }

        [RelayCommand]
        private async Task InstallUpdate()
        {
            await appUpdater.StartUpdate();
        }

        [RelayCommand]
        private async Task InstallWebView2()
        {
            try
            {
                IsWebView2Installing = true;

                if (await WebViewUtil.InstallWebView2())
                    _ = commandsClient.RestartUI("--appUpdate true");
                else
                    LinkUtil.OpenBrowser(WebViewUtil.DownloadUrl);
            }
            finally
            {
                IsWebView2Installing = false;
            }
        }

        public void CancelDownload()
        {
            downloader.Cancel();
        }

        private void UpdateDownload_DownloadProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                CurrentProgress = e.Percentage;
            });
        }

        private void UpdateDownload_DownloadFileCompleted(object sender, bool success)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(async () =>
            {
                if (success)
                    IsUpdateDownloaded = true;
            });
        }

        private void AppUpdater_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                UpdateState(e.UpdateStatus, e.UpdateDate, e.UpdateVersion);
            });
        }

        private void UpdateState(AppUpdateStatus status, DateTime date, Version version)
        {
            switch (status)
            {
                case AppUpdateStatus.uptodate:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Informational";
                    UpdateStatusText = languageResource.GetString("TextUpdateUptodate");
                    break;
                case AppUpdateStatus.available:
                    IsUpdateAvailable = true;
                    UpdateStatusSeverity = "Success";
                    UpdateStatusText = languageResource.GetString("DescriptionUpdateAvailable");
                    break;
                case AppUpdateStatus.invalid:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Error";
                    UpdateStatusText = "This software has unique version tag~";
                    break;
                case AppUpdateStatus.notchecked:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = IsWinStore ? "Informational" : "Warning";
                    UpdateStatusText = languageResource.GetString("TextUpdateChecking");
                    break;
                case AppUpdateStatus.error:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Error";
                    UpdateStatusText = languageResource.GetString("TextupdateCheckFail");
                    break;
            }
            UpdateStatus = status;
            UpdateDateText = status == AppUpdateStatus.notchecked ? $"{languageResource.GetString("TextLastChecked")}: ---" : $"{languageResource.GetString("TextLastChecked")}: {date}";
        }
    }
}
