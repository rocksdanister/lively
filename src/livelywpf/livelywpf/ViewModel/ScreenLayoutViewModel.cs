using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using livelywpf.Model;

namespace livelywpf
{
    public class ScreenLayoutViewModel : ObservableObject
    {
        public ScreenLayoutViewModel()
        {
            SelectedWallpaperLayout = (int)Program.SettingsVM.Settings.WallpaperArrangement;
            ScreenItems = new ObservableCollection<ScreenLayoutModel>();
            UpdateLayout();

            SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
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
                    OnPropertyChanged("ScreenItems");
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
                    OnPropertyChanged("SelectedItem");
                    CanCloseWallpaper();
                    CanCustomiseWallpaper();
                    if (!ScreenHelper.ScreenCompare(value.Screen, Program.SettingsVM.Settings.SelectedDisplay, DisplayIdentificationMode.deviceId))
                    {
                        Program.SettingsVM.Settings.SelectedDisplay = value.Screen;
                        Program.SettingsVM.UpdateConfigFile();
                        //Updating library selected item.
                        Program.LibraryVM.SetupDesktop_WallpaperChanged(null, null);
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
                OnPropertyChanged("SelectedWallpaperLayout");

                if (Program.SettingsVM.Settings.WallpaperArrangement != (WallpaperArrangement)_selectedWallpaperLayout && value != -1)
                {
                    var prevArrangement = Program.SettingsVM.Settings.WallpaperArrangement;
                    Program.SettingsVM.Settings.WallpaperArrangement = (WallpaperArrangement)_selectedWallpaperLayout;
                    Program.SettingsVM.UpdateConfigFile();
                    //SetupDesktop.CloseAllWallpapers();
                    UpdateWallpaper(prevArrangement, Program.SettingsVM.Settings.WallpaperArrangement);
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
            if(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.per)
            {
                SetupDesktop.CloseWallpaper(selection.Screen);
            }
            else
            {
                SetupDesktop.CloseAllWallpapers();
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
                SetupDesktop.Wallpapers.ForEach(x =>
                {
                    if (ScreenHelper.ScreenCompare(x.GetScreen(), SelectedItem.Screen, DisplayIdentificationMode.deviceId))
                    {
                        result = true;
                    }
                });
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
            var items = SetupDesktop.Wallpapers.FindAll(x => x.GetWallpaperData().LivelyPropertyPath != null);
            if (items.Count > 0)
            {
                LibraryModel obj = null;
                switch (Program.SettingsVM.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        obj = items.Find(x => 
                            ScreenHelper.ScreenCompare(x.GetScreen(), selection.Screen, DisplayIdentificationMode.deviceId))?.GetWallpaperData();
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        obj = items[0].GetWallpaperData();
                        break;                
                }
                if (obj != null)
                {
                    var settingsWidget = new Cef.LivelyPropertiesTrayWidget(obj);
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

        private void UpdateLayout()
        {
            ScreenItems.Clear();
            switch (Program.SettingsVM.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    {
                        var unsortedScreenItems = new List<ScreenLayoutModel>();
                        foreach (var item in ScreenHelper.GetScreen())
                        {
                            string imgPath = null;
                            string livelyPropertyFilePath = null;
                            SetupDesktop.Wallpapers.ForEach(x =>
                            {
                                if (ScreenHelper.ScreenCompare(item, x.GetScreen(), DisplayIdentificationMode.deviceId))
                                {
                                    imgPath = x.GetWallpaperData().ThumbnailPath;
                                    livelyPropertyFilePath = x.GetLivelyPropertyCopyPath();
                                }
                            });
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
                        if (SetupDesktop.Wallpapers.Count == 0)
                        {
                            ScreenItems.Add(new ScreenLayoutModel(Program.SettingsVM.Settings.SelectedDisplay, null, null, "---"));
                        }
                        else
                        {
                            var x = SetupDesktop.Wallpapers[0];
                            ScreenItems.Add(new ScreenLayoutModel(Program.SettingsVM.Settings.SelectedDisplay,
                                x.GetWallpaperData().ThumbnailPath, x.GetLivelyPropertyCopyPath(), "---"));
                        }
                    }
                    break;
                case WallpaperArrangement.duplicate:
                    {
                        if (SetupDesktop.Wallpapers.Count == 0)
                        {
                            ScreenItems.Add(new ScreenLayoutModel(Program.SettingsVM.Settings.SelectedDisplay, null, null, "\""));
                        }
                        else
                        {
                            var x = SetupDesktop.Wallpapers[0];
                            ScreenItems.Add(new ScreenLayoutModel(Program.SettingsVM.Settings.SelectedDisplay, 
                                x.GetWallpaperData().ThumbnailPath, x.GetLivelyPropertyCopyPath(), "\""));
                        }
                    }
                    break;
            }

            foreach (var item in ScreenItems)
            {
                if (ScreenHelper.ScreenCompare(item.Screen, Program.SettingsVM.Settings.SelectedDisplay, DisplayIdentificationMode.deviceId))
                {
                    SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateWallpaper(WallpaperArrangement prev, WallpaperArrangement curr)
        {
            if (SetupDesktop.Wallpapers.Count > 0)
            {
                var wallpapers = SetupDesktop.Wallpapers.ToList();
                SetupDesktop.CloseAllWallpapers();
                if ((prev == WallpaperArrangement.per && curr == WallpaperArrangement.span) || (prev == WallpaperArrangement.per && curr == WallpaperArrangement.duplicate))
                {
                    var wp = wallpapers.FirstOrDefault(x => ScreenHelper.ScreenCompare(x.GetScreen(), SelectedItem.Screen, DisplayIdentificationMode.deviceId)) ?? wallpapers[0];
                    SetupDesktop.SetWallpaper(wp.GetWallpaperData(), ScreenHelper.GetPrimaryScreen());
                }
                else if ((prev == WallpaperArrangement.span && curr == WallpaperArrangement.per) || (prev == WallpaperArrangement.duplicate && curr == WallpaperArrangement.per))
                {
                    SetupDesktop.SetWallpaper(wallpapers[0].GetWallpaperData(), SelectedItem.Screen);
                }
                else if ((prev == WallpaperArrangement.span && curr == WallpaperArrangement.duplicate) || (prev == WallpaperArrangement.duplicate && curr == WallpaperArrangement.span))
                {
                    SetupDesktop.SetWallpaper(wallpapers[0].GetWallpaperData(), ScreenHelper.GetPrimaryScreen());
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
