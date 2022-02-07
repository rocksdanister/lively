using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.Wpf.Helpers.MVVM;
using Lively.UI.Wpf.ViewModels;
using Lively.UI.Wpf.Views.LivelyProperty.Dialogues;

namespace Lively.UI.Wpf.ViewModels
{
    public class ScreenLayoutViewModel : ObservableObject
    {
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;
        private readonly LibraryViewModel libraryVm;

        public ScreenLayoutViewModel(IUserSettingsClient userSettings, IDesktopCoreClient desktopCore, IDisplayManagerClient displayManager, LibraryViewModel libraryVm)
        {
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.libraryVm = libraryVm;

            SelectedWallpaperLayout = (int)userSettings.Settings.WallpaperArrangement;
            ScreenItems = new ObservableCollection<ScreenLayoutModel>();
            UpdateLayout();

            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Threading.ThreadStart(delegate
                {
                    UpdateLayout();
                }));
        }

        private ObservableCollection<ScreenLayoutModel> _screenItems;
        public ObservableCollection<ScreenLayoutModel> ScreenItems
        {
            get { return _screenItems; }
            set
            {
                if (value != _screenItems)
                {
                    _screenItems = value;
                    OnPropertyChanged();
                }
            }
        }

        private ScreenLayoutModel _selectedItem;
        public ScreenLayoutModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (value != null)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                    CanCloseWallpaper();
                    CanCustomiseWallpaper();
                    if (!userSettings.Settings.SelectedDisplay.Equals(value.Screen))
                    {
                        userSettings.Settings.SelectedDisplay = value.Screen;
                        userSettings.SaveAsync<ISettingsModel>();
                        //Updating library selected item.
                        libraryVm.UpdateSelection();
                    }
                }
            }
        }

        private int _selectedWallpaperLayout;
        public int SelectedWallpaperLayout
        {
            get
            {
                return _selectedWallpaperLayout;
            }
            set
            {
                _selectedWallpaperLayout = value;
                OnPropertyChanged();

                if (userSettings.Settings.WallpaperArrangement != (WallpaperArrangement)_selectedWallpaperLayout && value != -1)
                {
                    var prevArrangement = userSettings.Settings.WallpaperArrangement;
                    userSettings.Settings.WallpaperArrangement = (WallpaperArrangement)_selectedWallpaperLayout;
                    userSettings.SaveAsync<ISettingsModel>();
                    UpdateWallpaper(prevArrangement, userSettings.Settings.WallpaperArrangement);
                }
            }
        }

        #region commands

        private bool _canCloseWallpaper = false;
        private RelayCommand _closeWallpaperCommand;
        public RelayCommand CloseWallpaperCommand
        {
            get
            {
                if (_closeWallpaperCommand == null)
                {
                    _closeWallpaperCommand = new RelayCommand(
                        param => CloseWallpaper(SelectedItem),
                        param => _canCloseWallpaper);
                }
                return _closeWallpaperCommand;
            }
        }

        private void CloseWallpaper(ScreenLayoutModel selection)
        {
            if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.per)
            {
                desktopCore.CloseWallpaper(selection.Screen);
            }
            else
            {
                desktopCore.CloseAllWallpapers();
            }
            selection.ScreenImagePath = null;
            selection.LivelyPropertyPath = null;
            CanCloseWallpaper();
            CanCustomiseWallpaper();
        }

        private void CanCloseWallpaper()
        {
            bool result = false;
            if (SelectedItem != null)
            {
                foreach (var x in desktopCore.Wallpapers)
                {
                    if (SelectedItem.Screen.Equals(x.Display))
                    {
                        result = true;
                    }
                }
            }
            _canCloseWallpaper = result;
            CloseWallpaperCommand.RaiseCanExecuteChanged();
        }

        private bool _canCustomiseWallpaper = false;
        private RelayCommand _customiseWallpaperCommand;
        public RelayCommand CustomiseWallpaperCommand
        {
            get
            {
                if (_customiseWallpaperCommand == null)
                {
                    _customiseWallpaperCommand = new RelayCommand(
                        param => CustomiseWallpaper(SelectedItem),
                        param => _canCustomiseWallpaper);
                }
                return _customiseWallpaperCommand;
            }
        }

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
                            obj = libraryVm.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath == item.LivelyInfoFolderPath);
                        }
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        {
                            var item = items[0];
                            obj = libraryVm.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath == item.LivelyInfoFolderPath);
                        }
                        break;
                }
                if (obj != null)
                {
                    var settingsWidget = new LivelyPropertiesTrayWidget(obj);
                    settingsWidget.Show();
                }
            }
        }

        private void CanCustomiseWallpaper()
        {
            bool result = false;
            if (SelectedItem != null)
            {
                result = SelectedItem.LivelyPropertyPath != null;
            }
            _canCustomiseWallpaper = result;
            CustomiseWallpaperCommand.RaiseCanExecuteChanged();
        }

        #endregion //commands

        #region helpers

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            desktopCore.WallpaperChanged -= SetupDesktop_WallpaperChanged;
        }

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
                                    imgPath = x.ThumbnailPath;
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
                                x.ThumbnailPath, x.LivelyPropertyCopyPath, "---"));
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
                                x.ThumbnailPath, x.LivelyPropertyCopyPath, "\""));
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

        #endregion //helpers
    }
}