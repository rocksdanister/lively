using livelywpf.DataJSON;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace livelywpf
{
    public class SettingsViewModel
    {
        public SettingsModel settings;

        public SettingsViewModel()
        {
            settings = SettingsJSON.LoadConfig(@"C:\Users\rocks\Documents\GIFS\lively_config.json");
            if(settings == null)
            {
                settings = new SettingsModel();
                SettingsJSON.SaveConfig(@"C:\Users\rocks\Documents\GIFS\lively_config.json", settings);
            }
        }
    }
}
