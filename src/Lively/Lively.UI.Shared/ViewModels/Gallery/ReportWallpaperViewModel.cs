using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class ReportWallpaperViewModel : ObservableObject
    {
        public ReportWallpaperViewModel(LibraryModel obj)
        {
            this.Model = obj;
        }

        [ObservableProperty]
        private LibraryModel model;
    }
}
