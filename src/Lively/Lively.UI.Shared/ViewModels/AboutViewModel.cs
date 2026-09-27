using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;

namespace Lively.UI.Shared.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [RelayCommand]
        private void OpenWebsite()
        {
            LinkUtil.OpenBrowser("https://livelywallpaper.net");
        }
    }
}
