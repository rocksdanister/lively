using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Lively.UI.Shared.ViewModels
{
    public partial class ControlPanelViewModel : ObservableObject
    {
        public event EventHandler<NavigatePageEventArgs> NavigatePage;

        public WallpaperLayoutViewModel WallpaperVm { get; }
        public ScreensaverLayoutViewModel ScreensaverVm { get; }

        public ControlPanelViewModel(WallpaperLayoutViewModel wallpaperVm,
            ScreensaverLayoutViewModel screensaverVm)
        {
            this.WallpaperVm = wallpaperVm;
            this.ScreensaverVm = screensaverVm;

            this.WallpaperVm.NavigatePage += WallpaperVm_NavigatePage;
            this.ScreensaverVm.PropertyChanged += ScreensaverVm_PropertyChanged;
        }

        [ObservableProperty]
        private bool isHideDialog;

        [RelayCommand]
        private void NavigateBackWallpaper()
        {
            NavigatePage?.Invoke(this, new NavigatePageEventArgs() { Tag = "wallpaper", Arg = null });
        }

        private void ScreensaverVm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScreensaverVm.IsHideDialog))
                IsHideDialog = ScreensaverVm.IsHideDialog;
        }

        public void OnWindowClosing(object sender, object e)
        {
            WallpaperVm?.OnWindowClosing();
            ScreensaverVm?.OnWindowClosing();

            this.WallpaperVm.NavigatePage -= WallpaperVm_NavigatePage;
            this.ScreensaverVm.PropertyChanged -= ScreensaverVm_PropertyChanged;
        }

        private void WallpaperVm_NavigatePage(object sender, NavigatePageEventArgs e)
        {
            NavigatePage?.Invoke(this, e);
        }

        public class NavigatePageEventArgs : EventArgs
        {
            public string Tag { get; set; }
            public object Arg { get; set; }
        }
    }
}
