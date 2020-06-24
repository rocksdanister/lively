using livelywpf.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace livelywpf
{
    public class SettingsViewModel : ObservableObject
    {
        public SettingsViewModel()
        {
            Settings = SettingsJSON.LoadConfig(@"C:\Users\rocks\Documents\GIFS\lively_config.json");
            if (Settings == null)
            {
                Settings = new SettingsModel();
                SettingsJSON.SaveConfig(@"C:\Users\rocks\Documents\GIFS\lively_config.json", Settings);
            }

            CmbboxSelectedTileSizeIndex = Settings.TileSize;
        }

        private SettingsModel _settings;
        public SettingsModel Settings
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
                OnPropertyChanged("Settings");
            }
        }

        private bool _isStartup;
        public bool IsStartup
        {
            get
            {
                return _isStartup;
            }
            set
            {
                _isStartup = value;
                OnPropertyChanged("IsStartup");
                //run startup fn call here...
                //SetStartup()
            }
        }

        private ICommand _applicationRulesCommand;
        public ICommand ApplicationRulesCommand
        {
            get
            {
                if (_applicationRulesCommand == null)
                {
                    _applicationRulesCommand = new RelayCommand(
                        param => ShowApplicationRulesWindow()
                        );
                }
                return _applicationRulesCommand;
            }
        }
        
        private void ShowApplicationRulesWindow()
        {
            MessageBox.Show("application rules");
        }

        private int _cmbboxSelectedTileSizeIndex;
        public int CmbboxSelectedTileSizeIndex
        {
            get
            {
                return _cmbboxSelectedTileSizeIndex;
            }
            set
            {
                _cmbboxSelectedTileSizeIndex = value;
                OnPropertyChanged("CmbboxSelectedTileSizeIndex");

                //todo: argumentoutofrange exception
                Settings.TileSize = value;
                /*
                if(LibraryView.LivelyGridControl != null)
                    LibraryView.LivelyGridControl.GridElementSize((livelygrid.GridSize)value);
                */
                SettingsJSON.SaveConfig(@"C:\Users\rocks\Documents\GIFS\lively_config.json", Settings);
            }
        }

    }
}
