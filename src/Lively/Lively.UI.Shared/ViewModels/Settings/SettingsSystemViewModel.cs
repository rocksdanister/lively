using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common.Helpers;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class SettingsSystemViewModel : ObservableObject
    {
        private readonly IUserSettingsClient userSettings;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcher;
        private readonly IFileService fileService;
        private readonly ICommandsClient commands;

        public SettingsSystemViewModel(IUserSettingsClient userSettings, 
            ICommandsClient commands,
            IDispatcherService dispatcher,
            IFileService fileService,
            IDialogService dialogService)
        {
            this.userSettings = userSettings;
            this.commands = commands;
            this.dispatcher = dispatcher;
            this.fileService = fileService;
            this.dialogService = dialogService;

            SelectedTaskbarThemeIndex = (int)userSettings.Settings.SystemTaskbarTheme;
        }

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
        private async Task ExtractLog()
        {
            var suggestedFileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var fileTypeChoices = new Dictionary<string, IList<string>>()
            {
                { "Compressed archive", new List<string>() { ".zip" } }
            };
            var file = await fileService.PickSaveFileAsync(suggestedFileName, fileTypeChoices);
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
