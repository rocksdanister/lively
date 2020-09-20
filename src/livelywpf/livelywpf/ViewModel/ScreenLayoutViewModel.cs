using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.TextFormatting;
using livelywpf.Core;
using livelywpf.Model;
using Octokit;

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
                    if (!ScreenHelper.ScreenCompare(value.Screen, Program.SettingsVM.Settings.SelectedDisplay, DisplayIdentificationMode.screenLayout))
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
                if (Program.SettingsVM.Settings.WallpaperArrangement != (WallpaperArrangement)value)
                {
                    Program.SettingsVM.Settings.WallpaperArrangement = (WallpaperArrangement)value;
                    Program.SettingsVM.UpdateConfigFile();
                    SetupDesktop.CloseAllWallpapers();
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
                    if (ScreenHelper.ScreenCompare(x.GetScreen(), SelectedItem.Screen, DisplayIdentificationMode.screenLayout))
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
            if (SelectedItem != null)
            {
                foreach (var x in SetupDesktop.Wallpapers)
                {
                    if (ScreenHelper.ScreenCompare(x.GetScreen(), selection.Screen, DisplayIdentificationMode.screenLayout))
                    {
                        if (selection.LivelyPropertyPath != null)
                        {
                            var settingsWidget = new Cef.LivelyPropertiesTrayWidget(
                               x.GetWallpaperData(),
                               selection.LivelyPropertyPath,
                               selection.Screen
                               );
                            settingsWidget.Show();
                        }
                        break;
                    }
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
            if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
            {
                if (SetupDesktop.Wallpapers.Count == 0)
                {
                    ScreenItems.Add(new ScreenLayoutModel(Program.SettingsVM.Settings.SelectedDisplay, null, null, "---"));
                }
                else
                {
                    var x = SetupDesktop.Wallpapers[0];
                    ScreenItems.Add(new ScreenLayoutModel(x.GetScreen(), x.GetWallpaperData().ThumbnailPath, x.GetLivelyPropertyCopyPath(), "---"));
                }
            }
            else
            {
                List<Model.ScreenLayoutModel> unsortedScreenItems = new List<Model.ScreenLayoutModel>();
                foreach (var item in ScreenHelper.GetScreen())
                {
                    string imgPath = null;
                    string livelyPropertyFilePath = null;
                    SetupDesktop.Wallpapers.ForEach(x =>
                    {
                        if (ScreenHelper.ScreenCompare(item, x.GetScreen(), DisplayIdentificationMode.screenLayout))
                        {
                            imgPath = x.GetWallpaperData().ThumbnailPath;
                            livelyPropertyFilePath = x.GetLivelyPropertyCopyPath();
                        }
                    });
                    if(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                    {
                        unsortedScreenItems.Add(new Model.ScreenLayoutModel(item, imgPath, livelyPropertyFilePath, item.DeviceNumber + '"'));
                    }
                    else
                    {
                        unsortedScreenItems.Add(new Model.ScreenLayoutModel(item, imgPath, livelyPropertyFilePath, item.DeviceNumber));
                    }
                }

                foreach (var item in unsortedScreenItems.OrderBy(x => x.Screen.Bounds.X).ToList())
                {
                    ScreenItems.Add(item);
                }
            }

            foreach (var item in ScreenItems)
            {
                if (ScreenHelper.ScreenCompare(item.Screen, Program.SettingsVM.Settings.SelectedDisplay, DisplayIdentificationMode.screenLayout))
                {
                    SelectedItem = item;
                    break;
                }
            }
        }

        #endregion //helpers
    }
}
