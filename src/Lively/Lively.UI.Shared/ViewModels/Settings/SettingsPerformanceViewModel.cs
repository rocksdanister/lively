using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common.Factories;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class SettingsPerformanceViewModel : ObservableObject
    {
        private readonly IDialogService dialogService;
        private readonly IUserSettingsClient userSettings;
        private readonly IDispatcherService dispatcher;
        private readonly IApplicationsRulesFactory appRuleFactory;

        public SettingsPerformanceViewModel(
            IUserSettingsClient userSettings,
            IDialogService dialogService,
            IDispatcherService dispatcher,
            IApplicationsRulesFactory appRuleFactory)
        {
            this.userSettings = userSettings;
            this.dialogService = dialogService;
            this.dispatcher = dispatcher;
            this.appRuleFactory = appRuleFactory;

            SelectedAppFullScreenIndex = (int)userSettings.Settings.AppFullscreenPause;
            SelectedAppFocusIndex = (int)userSettings.Settings.AppFocusPause;
            SelectedBatteryPowerIndex = (int)userSettings.Settings.BatteryPause;
            SelectedRemoteDestopPowerIndex = (int)userSettings.Settings.RemoteDesktopPause;
            SelectedPowerSaveModeIndex = (int)userSettings.Settings.PowerSaveModePause;
            SelectedDisplayPauseRuleIndex = (int)userSettings.Settings.DisplayPauseSettings;
            SelectedPauseAlgorithmIndex = (int)userSettings.Settings.ProcessMonitorAlgorithm;
            //Only pause rules are shown to user, rest is internal use.
            AppRules = new ObservableCollection<ApplicationRulesModel>(userSettings.AppRules.Where(x => x.Rule == Models.Enums.AppRules.pause));
        }

        private int _selectedAppFullScreenIndex;
        public int SelectedAppFullScreenIndex
        {
            get => _selectedAppFullScreenIndex;
            set
            {
                if (userSettings.Settings.AppFullscreenPause != (AppRules)value)
                {
                    userSettings.Settings.AppFullscreenPause = (AppRules)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedAppFullScreenIndex, value);
            }
        }

        private int _selectedAppFocusIndex;
        public int SelectedAppFocusIndex
        {
            get => _selectedAppFocusIndex;
            set
            {
                if (userSettings.Settings.AppFocusPause != (AppRules)value)
                {
                    userSettings.Settings.AppFocusPause = (AppRules)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedAppFocusIndex, value);
            }
        }

        private int _selectedBatteryPowerIndex;
        public int SelectedBatteryPowerIndex
        {
            get => _selectedBatteryPowerIndex;
            set
            {
                if (userSettings.Settings.BatteryPause != (AppRules)value)
                {
                    userSettings.Settings.BatteryPause = (AppRules)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedBatteryPowerIndex, value);
            }
        }

        private int _selectedPowerSaveModeIndex;
        public int SelectedPowerSaveModeIndex
        {
            get => _selectedPowerSaveModeIndex;
            set
            {
                if (userSettings.Settings.PowerSaveModePause != (AppRules)value)
                {
                    userSettings.Settings.PowerSaveModePause = (AppRules)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedPowerSaveModeIndex, value);
            }
        }

        private int _selectedRemoteDestopPowerIndex;
        public int SelectedRemoteDestopPowerIndex
        {
            get => _selectedRemoteDestopPowerIndex;
            set
            {
                if (userSettings.Settings.RemoteDesktopPause != (AppRules)value)
                {
                    userSettings.Settings.RemoteDesktopPause = (AppRules)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedRemoteDestopPowerIndex, value);
            }
        }

        private int _selectedDisplayPauseRuleIndex;
        public int SelectedDisplayPauseRuleIndex
        {
            get => _selectedDisplayPauseRuleIndex;
            set
            {
                if (userSettings.Settings.DisplayPauseSettings != (DisplayPause)value)
                {
                    userSettings.Settings.DisplayPauseSettings = (DisplayPause)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedDisplayPauseRuleIndex, value);
            }
        }

        private int _selectedPauseAlgorithmIndex;
        public int SelectedPauseAlgorithmIndex
        {
            get => _selectedPauseAlgorithmIndex;
            set
            {
                if (userSettings.Settings.ProcessMonitorAlgorithm != (ProcessMonitorAlgorithm)value)
                {
                    userSettings.Settings.ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedPauseAlgorithmIndex, value);
            }
        }

        #region apprules

        [ObservableProperty]
        private ObservableCollection<ApplicationRulesModel> appRules = [];

        private ApplicationRulesModel _selectedAppRuleItem;
        public ApplicationRulesModel SelectedAppRuleItem
        {
            get => _selectedAppRuleItem;
            set
            {
                SetProperty(ref _selectedAppRuleItem, value);
                RemoveAppRuleCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private async Task AddAppRule()
        {
            var result = await dialogService.ShowApplicationPickerDialogAsync();
            if (result is null)
                return;

            try
            {
                var rule = appRuleFactory.CreateAppPauseRule(result.AppPath, Models.Enums.AppRules.pause);
                if (AppRules.Any(x => x.AppName.Equals(rule.AppName, StringComparison.Ordinal)))
                    return;

                userSettings.AppRules.Add(rule);
                AppRules.Add(rule);
                UpdateAppRulesConfigFile();
            }
            catch { /* Failed to parse program information, ignore. */ }
        }

        [RelayCommand(CanExecute = nameof(CanRemoveAppRule))]
        private void RemoveAppRule()
        {
            userSettings.AppRules.Remove(SelectedAppRuleItem);
            AppRules.Remove(SelectedAppRuleItem);
            UpdateAppRulesConfigFile();
        }

        private bool CanRemoveAppRule => SelectedAppRuleItem != null;

        #endregion //apprules

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
        }

        public void UpdateAppRulesConfigFile()
        {
            _ = dispatcher.TryEnqueue(userSettings.Save<List<ApplicationRulesModel>>);
        }
    }
}
