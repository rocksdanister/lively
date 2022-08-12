using Lively.Common.Helpers.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Models
{
    public class ApplicationModel : ObservableObject
    {
        private string _appName;
        public string AppName
        {
            get { return _appName; }
            set { _appName = value; OnPropertyChanged(); }
        }

        private string _appPath;
        public string AppPath
        {
            get { return _appPath; }
            set { _appPath = value; OnPropertyChanged(); }
        }

        private string _appIcon;
        public string AppIcon
        {
            get { return _appIcon; }
            set { _appIcon = value; OnPropertyChanged(); }
        }
    }
}
