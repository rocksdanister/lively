using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lively.UI.Shared.ViewModels
{
    public partial class ChooseDisplayViewModel : ObservableObject
    {
        public event EventHandler OnRequestClose;

        private readonly IUserSettingsClient userSettings;
        private readonly IDisplayManagerClient displayManager;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDispatcherService dispatcher;

        public ChooseDisplayViewModel(IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            IDispatcherService dispatcher)
        {
            this.desktopCore = desktopCore;
            this.userSettings = userSettings;
            this.displayManager = displayManager;
            this.dispatcher = dispatcher;

            UpdateLayout();

            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;
        }

        [ObservableProperty]
        private ObservableCollection<ScreenLayoutModel> screenItems = [];

        private ScreenLayoutModel _selectedItem;
        public ScreenLayoutModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        public void OnWindowClosing(object sender, RoutedEventArgs e)
            => desktopCore.WallpaperChanged -= SetupDesktop_WallpaperChanged;

        private void UpdateLayout()
        {
            ScreenItems.Clear();
            foreach (var item in displayManager.DisplayMonitors)
            {
                // Only used for per display wallpaper arrangement.
                var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => item.Equals(x.Display));
                ScreenItems.Add(new ScreenLayoutModel(item,
                    string.IsNullOrEmpty(wallpaper?.PreviewPath) ? wallpaper?.ThumbnailPath : wallpaper.PreviewPath,
                    wallpaper?.LivelyPropertyCopyPath,
                    string.Empty));
            }
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            dispatcher.TryEnqueue(UpdateLayout);
        }
    }
}
