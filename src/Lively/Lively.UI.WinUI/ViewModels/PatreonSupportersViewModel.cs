using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class PatreonSupportersViewModel : ObservableObject
    {
        public bool IsBetaBuild => Constants.ApplicationType.IsTestBuild;

        [ObservableProperty]
        private string supportersFetchError;
    }
}
