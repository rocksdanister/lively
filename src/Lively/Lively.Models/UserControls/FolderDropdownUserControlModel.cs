using CommunityToolkit.Mvvm.ComponentModel;

namespace Lively.Models.UserControls
{
    public partial class FolderDropdownUserControlModel : ObservableObject
    {
        [ObservableProperty]
        private string fileName;

        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private string imagePath;
    }
}
