using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models.Enums;

namespace Lively.Models
{
    public partial class ApplicationRulesModel : ObservableObject
    {
        public ApplicationRulesModel(string appName, AppRules rule)
        {
            AppName = appName;
            Rule = rule;
        }

        [ObservableProperty]
        private string appName;

        [ObservableProperty]
        private AppRules rule;
    }
}
