using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace livelywpf
{
    public class ApplicationRulesViewModel : ObservableObject
    {
        public ApplicationRulesViewModel()
        {
            var list = ApplicationRulesJSON.LoadAppRules(@"C:\Users\rocks\Documents\GIFS\application_rules.json");
            if (list == null)
            {
                AppRules = new ObservableCollection<ApplicationRulesModel>
                {
                    new ApplicationRulesModel("Photoshop", AppRulesEnum.pause),
                    new ApplicationRulesModel("Discord", AppRulesEnum.ignore)
                };
                ApplicationRulesJSON.SaveAppRules(AppRules.ToList(), @"C:\Users\rocks\Documents\GIFS\application_rules.json");
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
    }
}
