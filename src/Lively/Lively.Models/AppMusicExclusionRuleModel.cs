using CommunityToolkit.Mvvm.ComponentModel;

namespace Lively.Models
{
    public partial class AppMusicExclusionRuleModel : ObservableObject
    {
        public AppMusicExclusionRuleModel(string appName, string appPath)
        {
            this.AppName = appName;
            this.AppPath = appPath;
        }

        [ObservableProperty]
        private string appName;

        [ObservableProperty]
        private string appPath;
    }
}
