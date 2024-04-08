using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Common.Models;
using Lively.Common.Services.Downloader;
using Lively.Grpc.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class AppUpdateViewModel : ObservableObject
    {
        private readonly IAppUpdaterClient appUpdater;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDownloadService downloader;

        private readonly ResourceLoader languageResource;

        public AppUpdateViewModel(IAppUpdaterClient appUpdater, IDesktopCoreClient desktopCore, IDownloadService downloader)
        {
            this.appUpdater = appUpdater;
            this.desktopCore = desktopCore;
            this.downloader = downloader;

            languageResource = ResourceLoader.GetForViewIndependentUse();

            MenuUpdate(appUpdater.Status, appUpdater.LastCheckTime, appUpdater.LastCheckVersion);
            appUpdater.UpdateChecked += AppUpdater_UpdateChecked;

            downloader.DownloadProgressChanged += UpdateDownload_DownloadProgressChanged;
            downloader.DownloadFileCompleted += UpdateDownload_DownloadFileCompleted;
        }

        public bool IsWinStore => Constants.ApplicationType.IsMSIX;

        public string AppVersionText
        {
            get
            {
                var ver = "v" + desktopCore.AssemblyVersion;
                if (Constants.ApplicationType.IsTestBuild)
                    ver += "b";
                else if (Constants.ApplicationType.IsMSIX)
                    ver += $" {languageResource.GetString("Store/Header")}";
                return ver;
            }
        }

        [ObservableProperty]
        private double currentProgress;

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private bool isCheckingUpdate;

        [ObservableProperty]
        private bool isUpdateAvailable;

        [ObservableProperty]
        private string updateChangelogError;

        [ObservableProperty]
        private string updateStatusText;

        [ObservableProperty]
        private string updateDateText;

        [ObservableProperty]
        private string updateCommandText;

        [ObservableProperty]
        private string updateStatusSeverity = "Warning";

        [RelayCommand]
        private async Task CheckUpdate()
        {
            try
            {
                IsCheckingUpdate = true;
                await appUpdater.CheckUpdate();
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

        [RelayCommand]
        private async Task InstallUpdate()
        {
            await Download();
            // Start() in UpdateDownload_DownloadFileCompleted event.
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
                    await appUpdater.StartUpdate();
            });
        }

        private void AppUpdater_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                MenuUpdate(e.UpdateStatus, e.UpdateDate, e.UpdateVersion);
            });
        }

        private async Task Download()
        {
            try
            {
                IsDownloading = true;

                var fileName = appUpdater.LastCheckUri.Segments.Last();
                var filePath = Path.Combine(Constants.CommonPaths.TempDir, fileName);
                await downloader.DownloadFile(appUpdater.LastCheckUri, filePath);
            }
            finally
            {
                IsDownloading = false;
            }
        }

        private void MenuUpdate(AppUpdateStatus status, DateTime date, Version version)
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
                    UpdateStatusText = $"{languageResource.GetString("DescriptionUpdateAvailable")} (v{version})";
                    break;
                case AppUpdateStatus.invalid:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Error";
                    UpdateStatusText = "This software has unique version tag~";
                    break;
                case AppUpdateStatus.notchecked:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Warning";
                    UpdateStatusText = languageResource.GetString("TextUpdateChecking");
                    break;
                case AppUpdateStatus.error:
                    IsUpdateAvailable = false;
                    UpdateStatusSeverity = "Error";
                    UpdateStatusText = languageResource.GetString("TextupdateCheckFail");
                    break;
            }
            UpdateDateText = status == AppUpdateStatus.notchecked ? $"{languageResource.GetString("TextLastChecked")}: ---" : $"{languageResource.GetString("TextLastChecked")}: {date}";
            UpdateCommandText = IsUpdateAvailable ? languageResource.GetString("TextInstall") : languageResource.GetString("TextUpdateCheck");
        }
    }
}
