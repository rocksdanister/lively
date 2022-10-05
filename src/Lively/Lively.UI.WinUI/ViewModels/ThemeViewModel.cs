using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Microsoft.UI.Dispatching;
using Lively.UI.WinUI.Factories;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.ViewModels
{
    //TODO: https://github.com/microsoft/microsoft-ui-xaml/issues/6394 (accent color.)
    public partial class ThemeViewModel : ObservableObject
    {
        private readonly ResourceLoader i18n;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly IUserSettingsClient userSettings;
        private readonly IThemeFactory themeFactory;
        private readonly MainViewModel mainVm;

        public ThemeViewModel(IUserSettingsClient userSettings, IThemeFactory themeFactory, MainViewModel mainVm)
        {
            this.userSettings = userSettings;
            this.themeFactory = themeFactory;
            this.mainVm = mainVm;
            //MainWindow dispatcher may not be ready yet, creating our own instead..
            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;
            i18n = ResourceLoader.GetForViewIndependentUse();

            //Defaults
            Themes.Add(new ThemeModel() { Name = i18n.GetString("TextDefault/Text"), Description = i18n.GetString("DescriptionDefault/Text"), Preview = "ms-appx:///Assets/icons8-application-window-96.png", IsEditable = false });
            Themes.Add(new ThemeModel() { Name = i18n.GetString("TextDynamicTheme/Text"), Description = i18n.GetString("DescriptionDynamicTheme/Text"), Preview = "ms-appx:///Assets/icons8-wallpaper-96.png", IsEditable = false });
            //User collection
            foreach (var item in new DirectoryInfo(Constants.CommonPaths.ThemeDir).GetDirectories("*.*", SearchOption.TopDirectoryOnly).OrderBy(t => t.LastWriteTime))
            {
                try
                {
                    var theme = themeFactory.CreateTheme(item.FullName);
                    Themes.Add(theme);
                }
                catch { }
            }

            SelectedItem = userSettings.Settings.ApplicationThemeBackground switch
            {
                AppThemeBackground.dynamic => Themes[1],
                AppThemeBackground.default_mica => Themes[0],
                AppThemeBackground.default_acrylic => Themes[0],
                AppThemeBackground.custom => Themes.Skip(2).FirstOrDefault(x => Directory.GetParent(x.File).FullName.Equals(userSettings.Settings.ApplicationThemeBackgroundPath)) ?? Themes[0],
                _ => Themes[0],
            };
            SelectedAppThemeIndex = (int)userSettings.Settings.ApplicationTheme;
        }

        [ObservableProperty]
        private ObservableCollection<ThemeModel> themes = new();

        private ThemeModel _selectedItem;
        public ThemeModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                var index = Themes.IndexOf(value);
                SetProperty(ref _selectedItem, value);
                var prevTheme = userSettings.Settings.ApplicationThemeBackground;
                var prevPath = userSettings.Settings.ApplicationThemeBackgroundPath;
                if (index == 0 || index == -1)
                {
                    userSettings.Settings.ApplicationThemeBackground = AppThemeBackground.default_mica;
                    userSettings.Settings.ApplicationThemeBackgroundPath = String.Empty;
                }
                else if (index == 1)
                {
                    userSettings.Settings.ApplicationThemeBackground = AppThemeBackground.dynamic;
                    userSettings.Settings.ApplicationThemeBackgroundPath = String.Empty;
                }
                else
                {
                    userSettings.Settings.ApplicationThemeBackground = AppThemeBackground.custom;
                    userSettings.Settings.ApplicationThemeBackgroundPath = Directory.GetParent(_selectedItem.File).FullName;
                }

                if (prevPath != userSettings.Settings.ApplicationThemeBackgroundPath || prevTheme != userSettings.Settings.ApplicationThemeBackground)
                {
                    UpdateSettingsConfigFile();
                    _ = mainVm.UpdateTheme();
                }
            }
        }

        private int _selectedAppThemeIndex;
        public int SelectedAppThemeIndex
        {
            get => _selectedAppThemeIndex;
            set
            {
                SetProperty(ref _selectedAppThemeIndex, value);
                if (userSettings.Settings.ApplicationTheme != (AppTheme)value)
                {
                    userSettings.Settings.ApplicationTheme = (AppTheme)value;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private RelayCommand _browseCommand;
        public RelayCommand BrowseCommand => _browseCommand ??= new RelayCommand(async () => await BrowseTheme());

        private async Task BrowseTheme()
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".gif");
            //filePicker.FileTypeFilter.Add(".lwt");
            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    Themes.Add(themeFactory.CreateTheme(file.Path, file.DisplayName, file.DisplayType));
                    SelectedItem = Themes.Last();
                }
                catch { }
            }
        }

        private RelayCommand<ThemeModel> _deleteCommand;
        public RelayCommand<ThemeModel> DeleteCommand =>
            _deleteCommand ??= new RelayCommand<ThemeModel>(async (obj) => {
                if (obj.IsEditable)
                {
                    SelectedItem = SelectedItem != obj ? SelectedItem : Themes[0];
                    Themes.Remove(obj);
                    await FileOperations.DeleteDirectoryAsync(Directory.GetParent(obj.File).FullName);
                }
            });

        private RelayCommand<ThemeModel> _openCommand;
        public RelayCommand<ThemeModel> OpenCommand =>
            _openCommand ??= new RelayCommand<ThemeModel>(async (obj) => {
                if (obj.IsEditable)
                {
                    await DesktopBridgeUtil.OpenFolder(Directory.GetParent(obj.File).FullName);
                }
            });

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<ISettingsModel>();
            });
        }

    }
}
