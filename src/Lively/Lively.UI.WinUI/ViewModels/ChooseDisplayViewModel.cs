using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Common;
using Lively.Grpc.Client;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class ChooseDisplayViewModel : ObservableObject
    {
        public event EventHandler OnRequestClose;

        private readonly IUserSettingsClient userSettings;
        private readonly IDisplayManagerClient displayManager;
        private readonly IDesktopCoreClient desktopCore;

        public ChooseDisplayViewModel(IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager)
        {
            this.desktopCore = desktopCore;
            this.userSettings = userSettings;
            this.displayManager = displayManager;

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
                    item.Index.ToString()));
            }
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                UpdateLayout();
            });
        }
    }
}
