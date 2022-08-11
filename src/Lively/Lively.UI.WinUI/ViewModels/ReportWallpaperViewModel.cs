using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class ReportWallpaperViewModel : ObservableObject
    {
        public ReportWallpaperViewModel(ILibraryModel obj)
        {
            this.Model = obj;
        }

        [ObservableProperty]
        private ILibraryModel model;
    }
}
