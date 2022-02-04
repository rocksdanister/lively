using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Factories;
using Lively.UI.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT;

namespace Lively.UI.WinUI.ViewModels
{
    public class ApplicationRulesViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool itemSelected = false;

        private readonly IUserSettingsClient userSettings;
        private readonly IApplicationsRulesFactory appRuleFactory;

        public ApplicationRulesViewModel(IUserSettingsClient userSettings, IApplicationsRulesFactory appRuleFactory)
        {
            this.userSettings = userSettings;
            this.appRuleFactory = appRuleFactory;

            AppRules = new ObservableCollection<IApplicationRulesModel>(userSettings.AppRules);
            //Localization of apprules text..
            foreach (var item in AppRules)
            {
                item.RuleText = "TODO";//LocalizationUtil.GetLocalizedAppRules(item.Rule);
            }
        }

        private ObservableCollection<IApplicationRulesModel> _appRules;
        public ObservableCollection<IApplicationRulesModel> AppRules
        {
            get
            {
                return _appRules ?? new ObservableCollection<IApplicationRulesModel>();
            }
            set
            {
                _appRules = value;
                OnPropertyChanged();
            }
        }

        private IApplicationRulesModel _selectedItem;
        public IApplicationRulesModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                itemSelected = _selectedItem != null;
                RemoveCommand.NotifyCanExecuteChanged();
                if (itemSelected)
                {
                    SelectedAppRuleProperty = (int)_selectedItem.Rule;
                }
                OnPropertyChanged();
            }
        }

        
        private int _selectedAppRuleProperty;
        public int SelectedAppRuleProperty
        {
            get { return _selectedAppRuleProperty; }
            set
            {
                _selectedAppRuleProperty = value;
                if (itemSelected)
                {
                    SelectedItem.Rule = (AppRulesEnum)_selectedAppRuleProperty;
                    SelectedItem.RuleText = "TODO";//LocalizationUtil.GetLocalizedAppRules(SelectedItem.Rule);
                }
                OnPropertyChanged();
            }
        }
            

        private RelayCommand _addCommand;
        public RelayCommand AddCommand => _addCommand ??= new RelayCommand(AddProgram);

        private RelayCommand _removeCommand;
        public RelayCommand RemoveCommand => _removeCommand ??= new RelayCommand(RemoveProgram, () => itemSelected);

        private async void AddProgram()
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            filePicker.FileTypeFilter.Add("*");
            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    var rule = appRuleFactory.CreateAppRule(file.Path, AppRulesEnum.pause);
                    if (AppRules.Any(x => x.AppName.Equals(rule.AppName, StringComparison.Ordinal)))
                    {
                        return;
                    }
                    userSettings.AppRules.Add(rule);
                    rule.RuleText = "TODO";//LocalizationUtil.GetLocalizedAppRules(rule.Rule);
                    AppRules.Add(rule);
                }
                catch (Exception)
                {
                    //todo loggin
                }
            }
        }

        private void RemoveProgram()
        {
            userSettings.AppRules.Remove(SelectedItem);
            AppRules.Remove(SelectedItem);
        }

        public void UpdateDiskFile()
        {
            _ = userSettings.SaveAsync<List<IApplicationRulesModel>>();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            //save on exit..
            UpdateDiskFile();
        }
    }
}
