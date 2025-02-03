using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models.Enums;

namespace Lively.Models.UserControls
{
    public partial class NavigationItem : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string glyph;

        [ObservableProperty]
        private bool isAlert;

        [ObservableProperty]
        private int alert;

        [ObservableProperty]
        private ContentPageType pageType;
    }
}
