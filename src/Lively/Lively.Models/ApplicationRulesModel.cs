using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Newtonsoft.Json;
using System;

namespace Lively.Models
{
    [Serializable]
    public class ApplicationRulesModel : ObservableObject, IApplicationRulesModel
    {
        public ApplicationRulesModel(string appName, AppRulesEnum rule)
        {
            AppName = appName;
            Rule = rule;
        }

        private string _appName;
        public string AppName
        {
            get
            {
                return _appName;
            }
            set
            {
                _appName = value;
                OnPropertyChanged();
            }
        }

        private AppRulesEnum _rule;
        public AppRulesEnum Rule
        {
            get
            {
                return _rule;
            }
            set
            {
                _rule = value;
                OnPropertyChanged();
            }
        }
    }
}
