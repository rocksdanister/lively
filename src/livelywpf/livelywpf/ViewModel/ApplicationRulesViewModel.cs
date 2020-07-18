using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace livelywpf
{
    public class ApplicationRulesViewModel : ObservableObject
    {
        private bool itemSelected = false;
        public ApplicationRulesViewModel()
        {
            var list = ApplicationRulesJSON.LoadAppRules(@"C:\Users\rocks\Documents\GIFS\application_rules.json");
            if (list == null)
            {
                AppRules = new ObservableCollection<ApplicationRulesModel>
                {
                    //default parameters.
                    new ApplicationRulesModel("Photoshop", AppRulesEnum.pause),
                    new ApplicationRulesModel("Discord", AppRulesEnum.ignore)
                };
                UpdateDiskFile();
            }
            else
            {
                AppRules = new ObservableCollection<ApplicationRulesModel>(list);
            }
        }

        private ObservableCollection<ApplicationRulesModel> _appRules;
        public ObservableCollection<ApplicationRulesModel> AppRules
        {
            get
            {
                return _appRules;
            }
            set
            {
                _appRules = value;
                OnPropertyChanged("AppRules");
            }
        }

        private ApplicationRulesModel _selectedItem;
        public ApplicationRulesModel SelectedItem
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
                OnPropertyChanged("SelectedItem");
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
                OnPropertyChanged("SelectedAppRuleProperty");
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
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
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

                    AppRules.Add(new ApplicationRulesModel(fileName, AppRulesEnum.pause));
                }
                catch (Exception e)
                {
                    //todo loggin
                }
            }
        }

        private void RemoveProgram()
        {
            AppRules.Remove(SelectedItem);
        }

        public void UpdateDiskFile()
        {
            //test
            ApplicationRulesJSON.SaveAppRules(AppRules.ToList(), @"C:\Users\rocks\Documents\GIFS\application_rules.json");
        }
    }
}
