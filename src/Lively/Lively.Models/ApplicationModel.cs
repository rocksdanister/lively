using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

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
