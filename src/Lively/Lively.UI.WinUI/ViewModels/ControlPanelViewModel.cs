using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Grpc.Client;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class ControlPanelViewModel : ObservableObject
    {
        public class NavigatePageEventArgs : EventArgs
        {
            public string Tag { get; set; }
            public object Arg { get; set; }
        }
        public event EventHandler<NavigatePageEventArgs> NavigatePage;

        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;
        private readonly LibraryViewModel libraryVm;

        public ControlPanelViewModel(IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            LibraryViewModel libraryVm)
        {
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.libraryVm = libraryVm;

            SelectedWallpaperLayout = (int)userSettings.Settings.WallpaperArrangement;
            IsRememberSelectedScreen = userSettings.Settings.RememberSelectedScreen;
            ScreenItems = new ObservableCollection<ScreenLayoutModel>();
            UpdateLayout();

            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;
        }

        public void UpdateSettingsConfigFile()
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<SettingsModel>();
            });
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            _ = App.Services.GetRequiredService<MainWindow>().DispatcherQueue.TryEnqueue(() =>
            {
                UpdateLayout();
            });
        }

        [ObservableProperty]
        private ObservableCollection<ScreenLayoutModel> screenItems;

        private ScreenLayoutModel _selectedItem;
        public ScreenLayoutModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value is null)
                    return;

                SetProperty(ref _selectedItem, value);
                CustomiseWallpaperCommand.NotifyCanExecuteChanged();
                CloseWallpaperCommand.NotifyCanExecuteChanged();

                if (!userSettings.Settings.SelectedDisplay.Equals(value.Screen))
                {
                    userSettings.Settings.SelectedDisplay = value.Screen;
                    UpdateSettingsConfigFile();
                    //Updating library selected item.
                    libraryVm.UpdateSelectedWallpaper();
                }
            }
        }

        private int _selectedWallpaperLayout;
        public int SelectedWallpaperLayout
        {
            get => _selectedWallpaperLayout;
            set
            {
                if (userSettings.Settings.WallpaperArrangement != (WallpaperArrangement)value && value != -1)
                {
                    var prevArrangement = userSettings.Settings.WallpaperArrangement;
                    userSettings.Settings.WallpaperArrangement = (WallpaperArrangement)value;
                    UpdateSettingsConfigFile();
                    _ = UpdateWallpaper(prevArrangement, userSettings.Settings.WallpaperArrangement);
                }
                SetProperty(ref _selectedWallpaperLayout, value);
            }
        }

        private bool _isRememberSelectedScreen;
        public bool IsRememberSelectedScreen
        {
            get => _isRememberSelectedScreen;
            set
            {
                if (userSettings.Settings.RememberSelectedScreen != value)
                {
                    userSettings.Settings.RememberSelectedScreen = value;
                    if (value)
                    {
                        libraryVm.LibrarySelectionMode = "Single";
                        //Updating library selected item.
                        libraryVm.UpdateSelectedWallpaper();
                    }
                    else
                    {
                        libraryVm.LibrarySelectionMode = "None";
                    }
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isRememberSelectedScreen, value);
            }
        }

        public bool IsScreensaverPluginNotify 
        {
            get
            {
                try
                {
                    return !File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Lively.scr"));
                }
                catch
                {
                    return true;
                }
            }
        }

        private RelayCommand _closeWallpaperCommand;
        public RelayCommand CloseWallpaperCommand => _closeWallpaperCommand ??=
            new RelayCommand(() => CloseWallpaper(SelectedItem), CanCloseWallpaper);

        private void CloseWallpaper(ScreenLayoutModel selection)
        {
            if(userSettings.Settings.WallpaperArrangement == WallpaperArrangement.per)
            {
                desktopCore.CloseWallpaper(selection.Screen);
            }
            else
            {
                desktopCore.CloseAllWallpapers();
            }
            selection.ScreenImagePath = null;
            selection.LivelyPropertyPath = null;
            CustomiseWallpaperCommand.NotifyCanExecuteChanged();
            CloseWallpaperCommand.NotifyCanExecuteChanged();
        }

        private bool CanCloseWallpaper()
        {
            if (SelectedItem != null)
            {
                foreach (var x in desktopCore.Wallpapers)
                {
                    if (SelectedItem.Screen.Equals(x.Display))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private RelayCommand _customiseWallpaperCommand;
        public RelayCommand CustomiseWallpaperCommand => _customiseWallpaperCommand ??=
            new RelayCommand(() => CustomiseWallpaper(SelectedItem), CanCustomiseWallpaper);

        private void CustomiseWallpaper(ScreenLayoutModel selection)
        {
            //only for running wallpapers..
            var items = desktopCore.Wallpapers.Where(x => !string.IsNullOrEmpty(x.LivelyPropertyCopyPath)).ToList();
            if (items.Count > 0)
            {
                LibraryModel obj = null;
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        {
                            var item = items.Find(x => selection.Screen.Equals(x.Display));
                            obj = libraryVm.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(item.LivelyInfoFolderPath, StringComparison.OrdinalIgnoreCase));
                        }
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        {
                            var item = items[0];
                            obj = libraryVm.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(item.LivelyInfoFolderPath, StringComparison.OrdinalIgnoreCase));
                        }
                        break;      
                }

                if (obj != null)
                {
                    NavigatePage?.Invoke(this, new NavigatePageEventArgs() { Tag = "customiseWallpaper", Arg = obj });
                }
            }
        }

        private bool CanCustomiseWallpaper() => !string.IsNullOrEmpty(SelectedItem?.LivelyPropertyPath);

        public RelayCommand NavigateBackWallpaperCommand =>
            new RelayCommand(() => NavigatePage?.Invoke(this, new NavigatePageEventArgs() { Tag = "wallpaper", Arg = null }));

        public void OnWindowClosing(object sender, RoutedEventArgs e) 
            => desktopCore.WallpaperChanged -= SetupDesktop_WallpaperChanged;

        private void UpdateLayout()
        {
            ScreenItems.Clear();
            switch (userSettings.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    {
                        var unsortedScreenItems = new List<ScreenLayoutModel>();
                        foreach (var item in displayManager.DisplayMonitors)
                        {
                            string imgPath = null;
                            string livelyPropertyFilePath = null;
                            foreach (var x in desktopCore.Wallpapers)
                            {
                                if (item.Equals(x.Display))
                                {
                                    imgPath = string.IsNullOrEmpty(x.PreviewPath) ? x.ThumbnailPath : x.PreviewPath;
                                    livelyPropertyFilePath = x.LivelyPropertyCopyPath;
                                }
                            }
                            unsortedScreenItems.Add(
                                new ScreenLayoutModel((DisplayMonitor)item, imgPath, livelyPropertyFilePath, item.Index.ToString()));
                        }

                        foreach (var item in unsortedScreenItems.OrderBy(x => x.Screen.Bounds.X).ToList())
                        {
                            ScreenItems.Add(item);
                        }
                    }
                    break;
                case WallpaperArrangement.span:
                    {
                        if (desktopCore.Wallpapers.Count == 0)
                        {
                            ScreenItems.Add(new ScreenLayoutModel(userSettings.Settings.SelectedDisplay, null, null, "---"));
                        }
                        else
                        {
                            var x = desktopCore.Wallpapers[0];
                            ScreenItems.Add(new ScreenLayoutModel(userSettings.Settings.SelectedDisplay,
                                string.IsNullOrEmpty(x.PreviewPath) ? x.ThumbnailPath : x.PreviewPath, x.LivelyPropertyCopyPath, "---"));
                        }
                    }
                    break;
                case WallpaperArrangement.duplicate:
                    {
                        if (desktopCore.Wallpapers.Count == 0)
                        {
                            ScreenItems.Add(new ScreenLayoutModel(userSettings.Settings.SelectedDisplay, null, null, "\""));
                        }
                        else
                        {
                            var x = desktopCore.Wallpapers[0];
                            ScreenItems.Add(new ScreenLayoutModel(userSettings.Settings.SelectedDisplay,
                                string.IsNullOrEmpty(x.PreviewPath) ? x.ThumbnailPath : x.PreviewPath, x.LivelyPropertyCopyPath, "\""));
                        }
                    }
                    break;
            }

            foreach (var item in ScreenItems)
            {
                if (item.Screen.Equals(userSettings.Settings.SelectedDisplay))
                {
                    SelectedItem = item;
                    break;
                }
            }
        }

        private async Task UpdateWallpaper(WallpaperArrangement prev, WallpaperArrangement curr)
        {
            if (desktopCore.Wallpapers.Count > 0)
            {
                var wallpapers = desktopCore.Wallpapers.ToList();
                await desktopCore.CloseAllWallpapers();
                if ((prev == WallpaperArrangement.per && curr == WallpaperArrangement.span) || (prev == WallpaperArrangement.per && curr == WallpaperArrangement.duplicate))
                {
                    var primary = displayManager.DisplayMonitors.FirstOrDefault(x => x.IsPrimary);
                    var wp = wallpapers.FirstOrDefault(x => SelectedItem.Screen.Equals(x.Display)) ?? wallpapers[0];
                    await desktopCore.SetWallpaper(wp.LivelyInfoFolderPath, primary.DeviceId);
                }
                else if ((prev == WallpaperArrangement.span && curr == WallpaperArrangement.per) || (prev == WallpaperArrangement.duplicate && curr == WallpaperArrangement.per))
                {
                    await desktopCore.SetWallpaper(wallpapers[0].LivelyInfoFolderPath, SelectedItem.Screen.DeviceId);
                }
                else if ((prev == WallpaperArrangement.span && curr == WallpaperArrangement.duplicate) || (prev == WallpaperArrangement.duplicate && curr == WallpaperArrangement.span))
                {
                    var primary = displayManager.DisplayMonitors.FirstOrDefault(x => x.IsPrimary);
                    await desktopCore.SetWallpaper(wallpapers[0].LivelyInfoFolderPath, primary.DeviceId);
                }
            }
            else
            {
                UpdateLayout();
            }
        }
    }
}
