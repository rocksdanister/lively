using Lively.Common.Helpers.MVVM;
using Lively.Models;

namespace Lively.UI.Wpf.Models
{
    public class ScreenLayoutModel : ObservableObject, IScreenLayoutModel
    {
        public ScreenLayoutModel(IDisplayMonitor screen, string screenImagePath, string livelypropertyFilePath, string screenTitle)
        {
            this.Screen = screen;
            this.ScreenImagePath = screenImagePath;
            this.LivelyPropertyPath = livelypropertyFilePath;
            this.ScreenTitle = screenTitle;
        }

        private IDisplayMonitor _screen;
        public IDisplayMonitor Screen
        {
            get { return _screen; }
            set
            {
                _screen = value;
                OnPropertyChanged();
            }
        }

        private string _screenImagePath;
        public string ScreenImagePath
        {
            get { return _screenImagePath; }
            set
            {
                _screenImagePath = value;
                OnPropertyChanged();
            }
        }

        private string _livelyPropertyPath;
        public string LivelyPropertyPath
        {
            get { return _livelyPropertyPath; }
            set
            {
                _livelyPropertyPath = value;
                OnPropertyChanged();
            }
        }

        private string _screenTitle;
        public string ScreenTitle
        {
            get { return _screenTitle; }
            set
            {
                _screenTitle = value;
                OnPropertyChanged();
            }
        }
    }
}
