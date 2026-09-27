using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class SettingsSystemViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUserSettingsClient userSettings;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcher;
        private readonly IFileService fileService;
        private readonly IAppUpdaterClient appUpdater;
        private readonly ICommandsClient commands;
        private readonly IResourceService i18n;

        private bool isSwitchingChannel;

        public SettingsSystemViewModel(IUserSettingsClient userSettings, 
            ICommandsClient commands,
            IDispatcherService dispatcher,
            IFileService fileService,
            IResourceService i18n,
            IAppUpdaterClient appUpdater,
            IDialogService dialogService)
        {
            this.userSettings = userSettings;
            this.commands = commands;
            this.dispatcher = dispatcher;
            this.fileService = fileService;
            this.appUpdater = appUpdater;
            this.dialogService = dialogService;
            this.i18n = i18n;

            SelectedTaskbarThemeIndex = (int)userSettings.Settings.SystemTaskbarTheme;
        }

        public bool IsWinStore => PackageUtil.IsRunningAsPackaged;

        public bool IsBetaBuild => Constants.ApplicationType.IsTestBuild;

        public bool IsTaskbarThemeSupported => Environment.OSVersion.Version < new Version(10, 0, 22621, 1343);

        private int _selectedTaskbarThemeIndex;
        public int SelectedTaskbarThemeIndex
        {
            get => _selectedTaskbarThemeIndex;
            set
            {
                if (userSettings.Settings.SystemTaskbarTheme != (TaskbarTheme)value)
                {
                    userSettings.Settings.SystemTaskbarTheme = (TaskbarTheme)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedTaskbarThemeIndex, value);
            }
        }

        [RelayCommand]
        private void ShowDebug()
        {
            commands.ShowDebugger();
        }

        [RelayCommand]
        private async Task SwitchReleaseChannel()
        {
            if (isSwitchingChannel)
                return;

            isSwitchingChannel = true;
            using var switchCts = new CancellationTokenSource();
            using var dialogCts = new CancellationTokenSource();

            try
            {
                var dialogTask = dialogService.ShowCancellableProgressDialogAsync(i18n.GetString("PleaseWait/Text"), dialogCts.Token);
                var switchTask = appUpdater.SwitchReleaseChannel(!IsBetaBuild, switchCts.Token);
                var completed = await Task.WhenAny(dialogTask, switchTask);

                if (completed == dialogTask)
                {
                    bool userCancelled = await dialogTask;
                    if (userCancelled)
                        switchCts.Cancel();

                    // Still need to await the switchTask so that any exception is observed
                    await switchTask;
                }
                else
                {
                    // Propagate any exception
                    await switchTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                dialogCts.Cancel();
            }
            finally
            {
                isSwitchingChannel = false;
            }
        }

        [RelayCommand]
        private async Task ExtractLog()
        {
            var suggestedFileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var file = await fileService.PickSaveFileAsync(suggestedFileName, [("Compressed archive", [".zip"])]);
            if (file != null)
            {
                try
                {
                    LogUtil.ExtractLogFiles(file);
                }
                catch (Exception ex)
                {
                    await dialogService.ShowDialogAsync(ex.Message, "Error", "OK");
                }
            }
        }

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
        }
    }
}
