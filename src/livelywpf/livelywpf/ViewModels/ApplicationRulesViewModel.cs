using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.Storage;
using livelywpf.Models;
using livelywpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Documents;

namespace livelywpf.ViewModels
{
    public class ApplicationRulesViewModel : ObservableObject
    {
        readonly IUserSettingsService userSettings;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool itemSelected = false;
        public ApplicationRulesViewModel(IUserSettingsService userSettings)
        {
            this.userSettings = userSettings;
            AppRules = new ObservableCollection<IApplicationRulesModel>(userSettings.AppRules);
        }

        private ObservableCollection<IApplicationRulesModel> _appRules;
        public ObservableCollection<IApplicationRulesModel> AppRules
        {
            get
            {
                return _appRules;
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
                RemoveCommand.RaiseCanExecuteChanged();
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
                }
                OnPropertyChanged();
            }
        }
            

        private RelayCommand _addCommand;
        public RelayCommand AddCommand
        {
            get
            {
                if (_addCommand == null)
                {
                    _addCommand = new RelayCommand(
                        param => AddProgram()
                        );
                }
                return _addCommand;
            }
        }

        private RelayCommand _removeCommand;
        public RelayCommand RemoveCommand
        {
            get
            {
                if (_removeCommand == null)
                {
                    _removeCommand = new RelayCommand(
                        param => RemoveProgram(), param => itemSelected
                        );
                }
                return _removeCommand;
            }
        }

        private void AddProgram()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "application (*.exe) |*.exe"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    //skip if same name exists.
                    foreach (var item in AppRules)
                    {
                        if(item.AppName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }

                    var rule = new ApplicationRulesModel(fileName, AppRulesEnum.pause);
                    userSettings.AppRules.Add(rule);
                    AppRules.Add(rule);
                }
                catch (Exception e)
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
            userSettings.Save<List<IApplicationRulesModel>>();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            //save on exit..
            UpdateDiskFile();
        }
    }
}
