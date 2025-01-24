using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models.Services;
using Lively.UI.Shared.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class AppUpdateViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IResourceService i18n;
        private readonly IAppUpdaterClient appUpdater;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDownloadService downloader;
        private readonly IDialogService dialogService;
        private readonly ICommandsClient commandsClient;
        private readonly IDispatcherService dispatcher;

        private CancellationTokenSource downloadCts;

        public AppUpdateViewModel(IAppUpdaterClient appUpdater,
            IDesktopCoreClient desktopCore,
            IDownloadService downloader,
            ICommandsClient commandsClient,
            IDispatcherService dispatcher,
            IDialogService dialogService,
            IResourceService i18n)
        {
            this.appUpdater = appUpdater;
            this.desktopCore = desktopCore;
            this.downloader = downloader;
            this.dialogService = dialogService;
            this.commandsClient = commandsClient;
            this.dispatcher = dispatcher;
            this.i18n = i18n;

            UpdateState(appUpdater.Status, appUpdater.LastCheckTime, appUpdater.LastCheckVersion);
            appUpdater.UpdateChecked += AppUpdater_UpdateChecked;

            // This is only run once if the main interface is opened before the initial fetchDelay in Core for update check.
            if (appUpdater.Status == AppUpdateStatus.notchecked)
            {
                _ = CheckUpdate();
            }
            else if (appUpdater.Status == AppUpdateStatus.available)
            {
                try
                {
                    var fileName = appUpdater.LastCheckFileName;
                    var filePath = Path.Combine(Constants.CommonPaths.TempDir, fileName);
                    IsUpdateDownloaded = File.Exists(filePath);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
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
                    ver += $" {i18n.GetString("Store/Header")}";
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

                var fileName = appUpdater.LastCheckFileName;
                var filePath = Path.Combine(Constants.CommonPaths.TempDir, fileName);
                downloadCts = new CancellationTokenSource();

                Logger.Info($"Downloading update: {filePath}");
                await downloader.DownloadFile(appUpdater.LastCheckUri, filePath, new Progress<(double downloaded, double total)>(progress =>
                {
                    CurrentProgress = (float)(progress.downloaded * 100 / progress.total);
                }), downloadCts.Token);

                if (!downloadCts.Token.IsCancellationRequested)
                    IsUpdateDownloaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await dialogService.ShowDialogAsync($"{i18n.GetString("LivelyExceptionAppUpdateFail")}\n\nException:\n{ex}",
                    i18n.GetString("TextError"),
                    i18n.GetString("TextOK"));
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

                if (await WebViewUtil.InstallWebView2(downloader))
                    _ = commandsClient.RestartUI("--appUpdate true");
                else
                    LinkUtil.OpenBrowser(WebViewUtil.DownloadUrl);
            }
            finally
            {
                IsWebView2Installing = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            downloadCts?.Cancel();
        }

        private void AppUpdater_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            dispatcher.TryEnqueue(() =>
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
                    UpdateStatusText = i18n.GetString("TextUpdateUptodate");
                    break;
                case AppUpdateStatus.available:
                    IsUpdateAvailable = true;
                    UpdateStatusSeverity = "Success";
                    UpdateStatusText = i18n.GetString("DescriptionUpdateAvailable");
                    break;
                case AppUpdateStatus.invalid:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Error";
                    UpdateStatusText = "This software has unique version tag~";
                    break;
                case AppUpdateStatus.notchecked:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = IsWinStore ? "Informational" : "Warning";
                    UpdateStatusText = i18n.GetString("TextUpdateChecking");
                    break;
                case AppUpdateStatus.error:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Error";
                    UpdateStatusText = i18n.GetString("TextupdateCheckFail");
                    break;
            }
            UpdateStatus = status;
            UpdateDateText = status == AppUpdateStatus.notchecked ? $"{i18n.GetString("TextLastChecked")}: ---" : $"{i18n.GetString("TextLastChecked")}: {date}";
        }
    }
}
