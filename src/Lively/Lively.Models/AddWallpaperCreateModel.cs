using Lively.Common;
using Lively.Common.Helpers.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Models
{
    public class AddWallpaperCreateModel : ObservableObject
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged(); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        private string _icon;
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; OnPropertyChanged(); }
        }

        private WallpaperType _typeSupported;
        public WallpaperType TypeSupported
        {
            get { return _typeSupported; }
            set { _typeSupported = value; OnPropertyChanged(); }
        }

        private WallpaperCreateType _createType;
        public WallpaperCreateType CreateType
        {
            get { return _createType; }
            set { _createType = value; OnPropertyChanged(); }
        }
    }

    public enum WallpaperCreateType
    {
        none,
        depthmap,
        //videotranscode,
    }
}
