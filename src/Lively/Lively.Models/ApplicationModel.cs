using CommunityToolkit.Mvvm.ComponentModel;

namespace Lively.Models
{
    public partial class ApplicationModel : ObservableObject
    {
        [ObservableProperty]
        private string appName;

        [ObservableProperty]
        private string appPath;

        [ObservableProperty]
        private string appIcon;
    }
}
