using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf
{
    [Serializable]
    public class ApplicationRulesModel : ObservableObject
    {
        public ApplicationRulesModel(string appName, AppRulesEnum rule)
        {
            this.AppName = appName;
            this.Rule = rule;
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
                OnPropertyChanged("AppName");
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
                OnPropertyChanged("Rule");
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
                OnPropertyChanged("RuleText");
            }
        }
    }
}
