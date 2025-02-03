using CommunityToolkit.Mvvm.ComponentModel;

namespace Lively.Models
{
    public partial class InAppNotificationModel : ObservableObject
    {
        [ObservableProperty]
        private bool isOpen;

        [ObservableProperty]
        private string message;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private int progress;

        [ObservableProperty]
        private bool isProgressIndeterminate;
    }
}