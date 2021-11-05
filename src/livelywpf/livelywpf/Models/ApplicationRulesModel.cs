using livelywpf.Helpers.MVVM;
using Newtonsoft.Json;
using System;

namespace livelywpf.Models
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
                RuleText = _rule switch
                {
                    AppRulesEnum.pause => Properties.Resources.TextPerformancePause,
                    AppRulesEnum.ignore => Properties.Resources.TextPerformanceNone,
                    AppRulesEnum.kill => Properties.Resources.TextPerformanceKill,
                    _ => Properties.Resources.TextPerformanceNone,
                };
                OnPropertyChanged();
            }
        }

        private string _ruleText;
        [JsonIgnore]
        public string RuleText
        {
            get
            {
                return _ruleText;
            }
            set
            {
                _ruleText = value;
                OnPropertyChanged();
            }
        }
    }
}
