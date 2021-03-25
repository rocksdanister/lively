using System;
using System.Collections.Generic;
using System.Windows.Xps.Serialization;
using livelywpf.Core;

namespace livelywpf.Model
{
    public class ScreenLayoutModel : ObservableObject
    {
        public ScreenLayoutModel(LivelyScreen screen, string screenImagePath, string livelypropertyFilePath, string screenTitle)
        {
            this.Screen = screen;
            this.ScreenImagePath = screenImagePath;
            this.LivelyPropertyPath = livelypropertyFilePath;
            this.ScreenTitle = screenTitle;
        }

        private LivelyScreen _screen;
        public LivelyScreen Screen
        {
            get { return _screen; }
            set
            {
                _screen = value;
                OnPropertyChanged("Screen");
            }
        }

        private string _screenImagePath;
        public string ScreenImagePath
        {
            get { return _screenImagePath; }
            set
            {
                _screenImagePath = value;
                OnPropertyChanged("ScreenImagePath");
            }
        }

        private string _livelyPropertyPath;
        public string LivelyPropertyPath
        {
            get { return _livelyPropertyPath; }
            set
            {
                _livelyPropertyPath = value;
                OnPropertyChanged("LivelyPropertyPath");
            }
        }

        private string _screenTitle;
        public string ScreenTitle
        {
            get { return _screenTitle; }
            set
            {
                _screenTitle = value;
                OnPropertyChanged("ScreenTitle");
            }
        }
    }
}
