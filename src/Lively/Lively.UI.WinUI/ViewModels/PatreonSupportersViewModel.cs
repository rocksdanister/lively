using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class PatreonSupportersViewModel : ObservableObject
    {
        [ObservableProperty]
        private string supportersFetchError;
    }
}
