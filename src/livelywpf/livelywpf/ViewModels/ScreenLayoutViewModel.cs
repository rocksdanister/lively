using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using livelywpf.Core;
using livelywpf.Helpers;
using livelywpf.Helpers.MVVM;
using livelywpf.Models;
using livelywpf.Services;
using livelywpf.Views.LivelyProperty.Dialogues;
using Microsoft.Extensions.DependencyInjection;

namespace livelywpf.ViewModels
{
    public class ScreenLayoutViewModel : ObservableObject
    {
        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly LibraryViewModel libraryVm;

        public ScreenLayoutViewModel(IUserSettingsService userSettings, IDesktopCore desktopCore, LibraryViewModel libraryVm)
        {
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
                    if (!ScreenHelper.ScreenCompare(value.Screen, userSettings.Settings.SelectedDisplay, DisplayIdentificationMode.deviceId))
                    {
                        userSettings.Settings.SelectedDisplay = value.Screen;
                        userSettings.Save<ISettingsModel>();
                        //Updating library selected item.
                        libraryVm.SetupDesktop_WallpaperChanged(null, null);
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
                    userSettings.Save<ISettingsModel>();
                    //SetupDesktop.CloseAllWallpapers();
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
                    if (SelectedItem.Screen.Equals(x.Screen))
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
            var items = desktopCore.Wallpapers.Where(x => x.Model.LivelyPropertyPath != null).ToList();
            if (items.Count > 0)
            {
                LibraryModel obj = null;
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        obj = (LibraryModel)(items.Find(x => 
                            ScreenHelper.ScreenCompare(x.Screen, selection.Screen, DisplayIdentificationMode.deviceId))?.Model);
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        obj = (LibraryModel)items[0].Model;
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
                        foreach (var item in ScreenHelper.GetScreen())
                        {
                            string imgPath = null;
                            string livelyPropertyFilePath = null;
                            foreach (var x in desktopCore.Wallpapers)
                            {
                                if (ScreenHelper.ScreenCompare(item, x.Screen, DisplayIdentificationMode.deviceId))
                                {
                                    imgPath = x.Model.ThumbnailPath;
                                    livelyPropertyFilePath = x.LivelyPropertyCopyPath;
                                }
                            }
                            unsortedScreenItems.Add(
                                new ScreenLayoutModel(item, imgPath, livelyPropertyFilePath, item.DeviceNumber));
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
                                x.Model.ThumbnailPath, x.LivelyPropertyCopyPath, "---"));
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
                                x.Model.ThumbnailPath, x.LivelyPropertyCopyPath, "\""));
                        }
                    }
                    break;
            }

            foreach (var item in ScreenItems)
            {
                if (ScreenHelper.ScreenCompare(item.Screen, userSettings.Settings.SelectedDisplay, DisplayIdentificationMode.deviceId))
                {
                    SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateWallpaper(WallpaperArrangement prev, WallpaperArrangement curr)
        {
            if (desktopCore.Wallpapers.Count > 0)
            {
                var wallpapers = desktopCore.Wallpapers.ToList();
                desktopCore.CloseAllWallpapers();
                if ((prev == WallpaperArrangement.per && curr == WallpaperArrangement.span) || (prev == WallpaperArrangement.per && curr == WallpaperArrangement.duplicate))
                {
                    var wp = wallpapers.FirstOrDefault(x => ScreenHelper.ScreenCompare(x.Screen, SelectedItem.Screen, DisplayIdentificationMode.deviceId)) ?? wallpapers[0];
                    desktopCore.SetWallpaper(wp.Model, ScreenHelper.GetPrimaryScreen());
                }
                else if ((prev == WallpaperArrangement.span && curr == WallpaperArrangement.per) || (prev == WallpaperArrangement.duplicate && curr == WallpaperArrangement.per))
                {
                    desktopCore.SetWallpaper(wallpapers[0].Model, SelectedItem.Screen);
                }
                else if ((prev == WallpaperArrangement.span && curr == WallpaperArrangement.duplicate) || (prev == WallpaperArrangement.duplicate && curr == WallpaperArrangement.span))
                {
                    desktopCore.SetWallpaper(wallpapers[0].Model, ScreenHelper.GetPrimaryScreen());
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
