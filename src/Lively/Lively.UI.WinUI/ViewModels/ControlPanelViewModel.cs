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

        private CustomiseWallpaperViewModel customiseWallpaperViewModel;

        public ControlPanelViewModel(IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            LibraryViewModel libraryVm)
        {
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.libraryVm = libraryVm;

            SelectedWallpaperLayoutIndex = (int)userSettings.Settings.WallpaperArrangement;
            IsRememberSelectedScreen = userSettings.Settings.RememberSelectedScreen;
            IsScreensaverLockOnResume = userSettings.Settings.ScreensaverLockOnResume;
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

        [ObservableProperty]
        private WallpaperArrangement selectedWallpaperLayout;

        private int _selectedWallpaperLayoutIndex;
        public int SelectedWallpaperLayoutIndex
        {
            get => _selectedWallpaperLayoutIndex;
            set
            {
                if (userSettings.Settings.WallpaperArrangement != (WallpaperArrangement)value && value != -1)
                {
                    var prevArrangement = userSettings.Settings.WallpaperArrangement;
                    userSettings.Settings.WallpaperArrangement = (WallpaperArrangement)value;
                    UpdateSettingsConfigFile();
                    _ = UpdateWallpaper(prevArrangement, userSettings.Settings.WallpaperArrangement);
                }
                SetProperty(ref _selectedWallpaperLayoutIndex, value);
                SelectedWallpaperLayout = (WallpaperArrangement)value;
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
                        libraryVm.SelectedItem = null;
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

        private bool _isScreensaverLockOnResume;
        public bool IsScreensaverLockOnResume
        {
            get => _isScreensaverLockOnResume;
            set
            {
                if (userSettings.Settings.ScreensaverLockOnResume != value)
                {
                    userSettings.Settings.ScreensaverLockOnResume = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isScreensaverLockOnResume, value);
            }
        }

        private RelayCommand _closeWallpaperCommand;
        public RelayCommand CloseWallpaperCommand => _closeWallpaperCommand ??=
            new RelayCommand(() => CloseWallpaper(SelectedItem), CanCloseWallpaper);

        private void CloseWallpaper(ScreenLayoutModel selection)
        {
            if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.per)
                desktopCore.CloseWallpaper(selection.Screen);
            else
                desktopCore.CloseAllWallpapers();

            selection.ScreenImagePath = null;
            selection.LivelyPropertyPath = null;
            CustomiseWallpaperCommand.NotifyCanExecuteChanged();
            CloseWallpaperCommand.NotifyCanExecuteChanged();
        }

        private bool CanCloseWallpaper()
        {
            if (SelectedItem == null)
                return false;

            switch (userSettings.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    foreach (var x in desktopCore.Wallpapers)
                    {
                        if (SelectedItem.Screen.Equals(x.Display))
                            return true;
                    }
                    return false;
                case WallpaperArrangement.span:
                case WallpaperArrangement.duplicate:
                    return desktopCore.Wallpapers.Count != 0;
                default:
                    return true;
            }
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
                    customiseWallpaperViewModel = App.Services.GetRequiredService<CustomiseWallpaperViewModel>();
                    customiseWallpaperViewModel.Load(obj);
                    NavigatePage?.Invoke(this, new NavigatePageEventArgs() { Tag = "customiseWallpaper", Arg = customiseWallpaperViewModel });
                }
            }
        }

        private bool CanCustomiseWallpaper() => !string.IsNullOrEmpty(SelectedItem?.LivelyPropertyPath);

        public RelayCommand NavigateBackWallpaperCommand =>
            new RelayCommand(() => NavigatePage?.Invoke(this, new NavigatePageEventArgs() { Tag = "wallpaper", Arg = null }));

        // Page.Unloaded event is unreliable, if the issue is fixed just call this directly.
        public void CustomiseWallpaperPageOnClosed()
        {
            if (customiseWallpaperViewModel == null)
                return;

            customiseWallpaperViewModel.OnClose();
            customiseWallpaperViewModel = null;
        }

        public void OnWindowClosing(object sender, object e)
        {
            desktopCore.WallpaperChanged -= SetupDesktop_WallpaperChanged;
            CustomiseWallpaperPageOnClosed();
        }

        private void UpdateLayout()
        {
            ScreenItems.Clear();
            foreach (var item in displayManager.DisplayMonitors)
            {
                var wallpaper = userSettings.Settings.WallpaperArrangement switch
                {
                    WallpaperArrangement.per => desktopCore.Wallpapers.FirstOrDefault(x => item.Equals(x.Display)),
                    WallpaperArrangement.span => desktopCore.Wallpapers.FirstOrDefault(),
                    WallpaperArrangement.duplicate => desktopCore.Wallpapers.FirstOrDefault(),
                    _ => throw new NotImplementedException(),
                };
                ScreenItems.Add(new ScreenLayoutModel(item,
                    string.IsNullOrEmpty(wallpaper?.PreviewPath) ? wallpaper?.ThumbnailPath : wallpaper.PreviewPath,
                    wallpaper?.LivelyPropertyCopyPath,
                    item.Index.ToString()));
            }

            SelectedItem = ScreenItems.FirstOrDefault(x => x.Screen.Equals(userSettings.Settings.SelectedDisplay));
        }

        private void UpdateSettingsConfigFile()
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
