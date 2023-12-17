using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models;

namespace Lively.Models
{
    public partial class ScreenLayoutModel : ObservableObject
    {
        public ScreenLayoutModel(DisplayMonitor screen, string screenImagePath, string livelypropertyFilePath, string screenTitle)
        {
            this.Screen = screen;
            this.ScreenImagePath = screenImagePath;
            this.LivelyPropertyPath = livelypropertyFilePath;
            this.ScreenTitle = screenTitle;
        }

        [ObservableProperty]
        private DisplayMonitor screen;

        [ObservableProperty]
        private string screenImagePath;

        [ObservableProperty]
        private string livelyPropertyPath;

        [ObservableProperty]
        private string screenTitle;
    }
}
