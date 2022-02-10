using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        private bool updateAvailable;
        private readonly IAppUpdaterClient appUpdater;
        private readonly IDesktopCoreClient desktopCore;
        ResourceLoader languageResource;

        public AboutViewModel(IAppUpdaterClient appUpdater, IDesktopCoreClient desktopCore)
        {
            this.appUpdater = appUpdater;
            this.desktopCore = desktopCore;
            languageResource = ResourceLoader.GetForViewIndependentUse();

            MenuUpdate(appUpdater.Status, appUpdater.LastCheckTime, appUpdater.LastCheckVersion);
            appUpdater.UpdateChecked += AppUpdater_UpdateChecked;
        }

        public bool IsNotWinStore => !Constants.ApplicationType.IsMSIX;

        public string AppVersionText => "v" + desktopCore.AssemblyVersion +
                (Constants.ApplicationType.IsTestBuild ? "b" : (IsNotWinStore ? " " + "Store" : string.Empty));

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
        public RelayCommand UpdateAppCommand => _updateAppCommand ??= new RelayCommand(async () => await CheckUpdate(), () => canUpdateAppCommand);

        private async Task CheckUpdate()
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
        }

        private void AppUpdater_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            //Debug.WriteLine($"Update Checked: {appUpdater.Status}, {appUpdater.LastCheckTime}, {appUpdater.LastCheckVersion}");
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
            UpdateDateText = date == DateTime.MinValue ? $"{languageResource.GetString("TextLastChecked")}: ---" : $"{languageResource.GetString("TextLastChecked")}: {date}";
            UpdateCommandText = updateAvailable ? languageResource.GetString("TextDownload") : languageResource.GetString("TextUpdateCheck");
        }

        public void OnWindowClosing(object sender, RoutedEventArgs e)
            => appUpdater.UpdateChecked -= AppUpdater_UpdateChecked;
    }
}
