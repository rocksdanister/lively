using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.UI.Shared.Helpers;
using Lively.UI.WinUI.Factories;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    //TODO: https://github.com/microsoft/microsoft-ui-xaml/issues/6394 (accent color.)
    public partial class AppThemeViewModel : ObservableObject
    {
        private readonly IResourceService i18n;
        private readonly IUserSettingsClient userSettings;
        private readonly IAppThemeFactory themeFactory;
        private readonly MainViewModel mainVm;
        private readonly IFileService fileService;
        private readonly IDispatcherService dispatcher;

        public AppThemeViewModel(IUserSettingsClient userSettings,
            IAppThemeFactory themeFactory,
            MainViewModel mainVm,
            IFileService fileService,
            IResourceService i18n,
            IDispatcherService dispatcher)
        {
            this.userSettings = userSettings;
            this.themeFactory = themeFactory;
            this.mainVm = mainVm;
            this.fileService = fileService;
            this.dispatcher = dispatcher;
            this.i18n = i18n;

            //Defaults
            Themes.Add(new ThemeModel() { Name = i18n.GetString("TextDefault/Text"), Description = i18n.GetString("DescriptionDefault/Text"), Preview = "ms-appx:///Assets/icons8-application-window-96.png", IsEditable = false });
            Themes.Add(new ThemeModel() { Name = i18n.GetString("TextDynamicTheme/Text"), Description = i18n.GetString("DescriptionDynamicTheme/Text"), Preview = "ms-appx:///Assets/icons8-wallpaper-96.png", IsEditable = false });
            //User collection
            foreach (var item in new DirectoryInfo(Constants.CommonPaths.ThemeDir).GetDirectories("*.*", SearchOption.TopDirectoryOnly).OrderBy(t => t.LastWriteTime))
            {
                try
                {
                    var theme = themeFactory.CreateFromDirectory(item.FullName);
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
                    _ = mainVm.SetAppTheme(userSettings.Settings.ApplicationThemeBackground);
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
            var files = await fileService.PickFileAsync([".jpeg", ".jpg", ".png", ".gif"]);
            if (files.Any())
            {
                try
                {
                    Themes.Add(themeFactory.CreateFromFile(files[0], Path.GetFileName(files[0]), string.Empty));
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
                    await FileUtil.TryDeleteDirectoryAsync(Directory.GetParent(obj.File).FullName, 1000, 4000);
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
            dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
        }

    }
}
