using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

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
