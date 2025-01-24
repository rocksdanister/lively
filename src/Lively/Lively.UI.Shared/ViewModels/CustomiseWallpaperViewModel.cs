using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.LivelyControls;
using Lively.Models.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Lively.UI.Shared.ViewModels
{
    public partial class CustomiseWallpaperViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;
        private readonly IUserSettingsClient userSettings;
        private readonly IDispatcherService dispatcher;

        private Dictionary<string, ControlModel> livelyControlsCopy;
        private DisplayMonitor currentScreen;
        private string currentFilePath;

        public CustomiseWallpaperViewModel(IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            IDispatcherService dispatcher,
            IUserSettingsClient userSettings)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.dispatcher = dispatcher;
        }

        public void Load(LibraryModel model)
        {
            ErrorText = InfoText = null;

            try
            {
                this.Model = model;
                // We create a copy of the LivelyProperties.json file and modify it instead.
                (this.currentFilePath, this.currentScreen) = CreatePropertyCopy(model, userSettings.Settings.WallpaperArrangement, userSettings.Settings.SelectedDisplay);

                var livelyControls = LivelyPropertyUtil.GetControls(this.currentFilePath);
                this.Controls = new ObservableCollection<ControlModel>(livelyControls.Values);

                // For checking value change and updating storage file.
                this.livelyControlsCopy = LivelyPropertyUtil.GetControls(this.currentFilePath);

                if (livelyControls.Count == 0)
                    InfoText = "No control(s) defined.";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                ErrorText = ex.Message;
            }
        }

        [ObservableProperty]
        private LibraryModel model;

        [ObservableProperty]
        private ObservableCollection<ControlModel> controls;

        [ObservableProperty]
        private string errorText;

        [ObservableProperty]
        private string infoText;

        [RelayCommand]
        private void Button(ControlModel control)
        {
            if (control is null)
                return;

            var button = control as ButtonModel;
            WallpaperSendMsg(new LivelyButton() { Name = button.Name });
        }

        [RelayCommand]
        private void SliderValueChanged(ControlModel control)
        {
            if (control is null)
                return;

            var slider = control as SliderModel;
            var copy = livelyControlsCopy[slider.Name] as SliderModel;
            if (copy.Value == slider.Value)
                return;

            WallpaperSendMsg(new LivelySlider() { Name = slider.Name, Value = slider.Value, Step = slider.Step });
            copy.Value = slider.Value;
        }

        [RelayCommand]
        private void ColorPickerValueChanged(ControlModel control)
        {
            if (control is null)
                return;

            var colorPicker = control as ColorPickerModel;
            var copy = livelyControlsCopy[colorPicker.Name] as ColorPickerModel;
            if (copy.Value == colorPicker.Value)
                return;

            WallpaperSendMsg(new LivelyColorPicker() { Name = colorPicker.Name, Value = colorPicker.Value });
            copy.Value = colorPicker.Value;
        }

        [RelayCommand]
        private void TextboxValueChanged(ControlModel control)
        {
            if (control is null)
                return;

            var textBox = control as TextboxModel;
            var copy = livelyControlsCopy[textBox.Name] as TextboxModel;
            if (copy.Value == textBox.Value)
                return;

            WallpaperSendMsg(new LivelyTextBox() { Name = textBox.Name, Value = textBox.Value });
            copy.Value = textBox.Value;
        }

        [RelayCommand]
        private void CheckboxValueChanged(ControlModel control)
        {
            if (control is null)
                return;

            var checkBox = control as CheckboxModel;
            var copy = livelyControlsCopy[checkBox.Name] as CheckboxModel;
            if (copy.Value == checkBox.Value)
                return;

            WallpaperSendMsg(new LivelyCheckbox() { Name = checkBox.Name, Value = checkBox.Value });
            copy.Value = checkBox.Value;
        }

        [RelayCommand]
        private void DropdownValueChanged(ControlModel control)
        {
            if (control is null)
                return;

            switch (control)
            {
                case DropdownModel dropDown:
                    {
                        var copy = livelyControlsCopy[dropDown.Name] as DropdownModel;
                        if (copy.Value == dropDown.Value)
                            return;

                        WallpaperSendMsg(new LivelyDropdown() { Name = dropDown.Name, Value = dropDown.Value });
                        copy.Value = dropDown.Value;
                    }
                    break;
                case ScalerDropdownModel scalerDropdown:
                    {
                        var copy = livelyControlsCopy[scalerDropdown.Name] as ScalerDropdownModel;
                        if (copy.Value == scalerDropdown.Value)
                            return;

                        WallpaperSendMsg(new LivelyDropdownScaler() { Name = scalerDropdown.Name, Value = scalerDropdown.Value });
                        copy.Value = scalerDropdown.Value;
                    }
                    break;
                case FolderDropdownModel folderDropdown:
                    {
                        var copy = livelyControlsCopy[folderDropdown.Name] as FolderDropdownModel;
                        if (copy.Value == folderDropdown.Value)
                            return;

                        // It is null when no item is selected or file missing.
                        var relativeFilePath = folderDropdown.Value is null || folderDropdown.Folder is null ? null : Path.Combine(folderDropdown.Folder, folderDropdown.Value);
                        WallpaperSendMsg(new LivelyFolderDropdown() { Name = folderDropdown.Name, Value = relativeFilePath });
                        copy.Value = folderDropdown.Value;
                    }
                    break;
            }
        }

        [RelayCommand]
        private void RestoreDefault()
        {
            try
            {
                File.Copy(Model.LivelyPropertyPath, currentFilePath, true);
                WallpaperSendMsg(new LivelyButton() { Name = "lively_default_settings_reload", IsDefault = true });
                Load(Model);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void OnClose()
        {
            UpdatePropertyFile();
        }

        private void UpdatePropertyFile()
        {
            try
            {
                var serializedJson = JsonConvert.SerializeObject(livelyControlsCopy, Formatting.Indented);
                File.WriteAllText(currentFilePath, serializedJson);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        private void WallpaperSendMsg(IpcMessage msg)
        {
            _ = dispatcher.TryEnqueue(() =>
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        desktopCore.SendMessageWallpaper(currentScreen, Model, msg);
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        desktopCore.SendMessageWallpaper(Model, msg);
                        break;
                }
            });
        }

        /// <summary>
        /// Get LivelyProperties.json copy filepath and corresponding screen logic.
        /// </summary>
        /// <param name="obj">LibraryModel object</param>
        /// <returns></returns>
        public (string filePath, DisplayMonitor screen) CreatePropertyCopy(LibraryModel obj, WallpaperArrangement arrangement, DisplayMonitor selectedScreen)
        {
            if (obj.LivelyPropertyPath == null)
                throw new ArgumentException("Customisation not supported.");

            string propertyCopyPath = null;
            DisplayMonitor wallpaperScreen = null;
            var items = desktopCore.Wallpapers.Where(x => x.LivelyInfoFolderPath == obj.LivelyInfoFolderPath);
            if (!items.Any())
            {
                // We create the files only when wallpaper is not running, when launching the wallpaper the Core will create the file for us.
                wallpaperScreen = selectedScreen;
                var dataFolder = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir);
                //Create a directory with the wallpaper foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                //Further modifications are done to the copy file.
                string wallpaperDataDirectoryPath = null;
                switch (arrangement)
                {
                    case WallpaperArrangement.per:
                        wallpaperDataDirectoryPath = Path.Combine(dataFolder, new DirectoryInfo(obj.LivelyInfoFolderPath).Name, wallpaperScreen.Index.ToString());
                        break;
                    case WallpaperArrangement.span:
                        wallpaperDataDirectoryPath = Path.Combine(dataFolder, new DirectoryInfo(obj.LivelyInfoFolderPath).Name, "span");
                        break;
                    case WallpaperArrangement.duplicate:
                        wallpaperDataDirectoryPath = Path.Combine(dataFolder, new DirectoryInfo(obj.LivelyInfoFolderPath).Name, "duplicate");
                        break;
                }
                Directory.CreateDirectory(wallpaperDataDirectoryPath);
                //copy the original file if not found..
                propertyCopyPath = Path.Combine(wallpaperDataDirectoryPath, "LivelyProperties.json");
                if (!File.Exists(propertyCopyPath))
                    File.Copy(obj.LivelyPropertyPath, propertyCopyPath);
            }
            else if (items.Count() == 1)
            {
                //send regardless of selected display, if wallpaper is running on non-selected display - its modified instead.
                propertyCopyPath = items.First().LivelyPropertyCopyPath;
                wallpaperScreen = displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(items.First().Display));
            }
            else
            {
                switch (arrangement)
                {
                    case WallpaperArrangement.per:
                        {
                            //more than one screen; if selected display, sendpath otherwise send the first one found.
                            var selection = items.FirstOrDefault(x => selectedScreen.Equals(x.Display));
                            propertyCopyPath = selection != null ? selection.LivelyPropertyCopyPath : items.First().LivelyPropertyCopyPath;
                            wallpaperScreen = selection != null ? selection.Display : items.First().Display;
                        }
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        {
                            propertyCopyPath = items.First().LivelyPropertyCopyPath;
                            wallpaperScreen = items.First().Display;
                        }
                        break;
                }
            }
            return (propertyCopyPath, wallpaperScreen);
        }
    }
}
