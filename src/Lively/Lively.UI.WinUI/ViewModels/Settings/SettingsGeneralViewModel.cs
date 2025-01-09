using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Localization;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.UI.WinUI.Extensions;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.ViewModels.Settings
{
    public partial class SettingsGeneralViewModel : ObservableObject
    {
        private readonly DispatcherQueue dispatcherQueue;

        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDialogService dialogService;
        private readonly LibraryViewModel libraryVm;

        public SettingsGeneralViewModel(LibraryViewModel libraryVm, IDesktopCoreClient desktopCore,
            IUserSettingsClient userSettings,
            IDialogService dialogService)
        {
            this.libraryVm = libraryVm;
            this.desktopCore = desktopCore;
            this.userSettings = userSettings;
            this.dialogService = dialogService;

            //MainWindow dispatcher may not be ready yet, creating our own instead..
            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;

            //lang-codes: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
            LanguageItems = new ObservableCollection<LanguagesModel>(SupportedLanguages.Languages);

            IsStartup = userSettings.Settings.Startup;
            WallpaperDirectory = userSettings.Settings.WallpaperDir;
            IsSysTrayIconVisible = userSettings.Settings.SysTrayIcon;
            GlobalWallpaperVolume = userSettings.Settings.AudioVolumeGlobal;
            IsAudioOnlyOnDesktop = userSettings.Settings.AudioOnlyOnDesktop;
            IsReducedMotion = userSettings.Settings.UIMode != LivelyGUIState.normal;
            SelectedLanguageItem = SupportedLanguages.GetLanguage(userSettings.Settings.Language);
            MoveExistingWallpaperNewDir = userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir;
        }

        private bool _isStartup;
        public bool IsStartup
        {
            get => _isStartup;
            set
            {
                if (userSettings.Settings.Startup != value)
                {
                    userSettings.Settings.Startup = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isStartup, value);
            }
        }

        [ObservableProperty]
        private ObservableCollection<LanguagesModel> languageItems;

        private LanguagesModel _selectedLanguageItem;
        public LanguagesModel SelectedLanguageItem
        {
            get => _selectedLanguageItem;
            set
            {
                if (value.Codes.FirstOrDefault(x => x == userSettings.Settings.Language) == null)
                {
                    userSettings.Settings.Language = value.Codes[0];
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedLanguageItem, value);
            }
        }

        private bool _isSysTrayIconVisible;
        public bool IsSysTrayIconVisible
        {
            get => _isSysTrayIconVisible;
            set
            {
                if (userSettings.Settings.SysTrayIcon != value)
                {
                    userSettings.Settings.SysTrayIcon = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isSysTrayIconVisible, value);
            }
        }

        private bool _isReducedMotion;
        public bool IsReducedMotion
        {
            get => _isReducedMotion;
            set
            {
                var currentValue = userSettings.Settings.UIMode != LivelyGUIState.normal;
                if (currentValue != value)
                {
                    userSettings.Settings.UIMode = value ? LivelyGUIState.lite : LivelyGUIState.normal;
                    libraryVm.UpdateAnimationSettings(userSettings.Settings.UIMode);
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isReducedMotion, value);
            }
        }

        private int _globalWallpaperVolume;
        public int GlobalWallpaperVolume
        {
            get => _globalWallpaperVolume;
            set
            {
                if (userSettings.Settings.AudioVolumeGlobal != value)
                {
                    userSettings.Settings.AudioVolumeGlobal = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _globalWallpaperVolume, value);
            }
        }

        private bool _isAudioOnlyOnDesktop;
        public bool IsAudioOnlyOnDesktop
        {
            get => _isAudioOnlyOnDesktop;
            set
            {
                if (userSettings.Settings.AudioOnlyOnDesktop != value)
                {
                    userSettings.Settings.AudioOnlyOnDesktop = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isAudioOnlyOnDesktop, value);
            }
        }

        [RelayCommand]
        private async Task ThemeBackground()
        {
            await dialogService.ShowThemeDialogAsync();
        }

        [ObservableProperty]
        private string wallpaperDirectory;

        private bool _moveExistingWallpaperNewDir;
        public bool MoveExistingWallpaperNewDir
        {
            get => _moveExistingWallpaperNewDir;
            set
            {
                if (userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir != value)
                {
                    userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _moveExistingWallpaperNewDir, value);
            }
        }

        [ObservableProperty]
        private bool wallpaperDirectoryChangeOngoing;

        [RelayCommand]
        private async Task OpenWallpaperDirectory()
        {
            await DesktopBridgeUtil.OpenFolder(userSettings.Settings.WallpaperDir);
        }

        [RelayCommand(CanExecute = nameof(CanExecuteDirectoryChange))]
        private async Task WallpaperDirectoryChange()
        {
            var folder = await ShowFolderPicker("*");
            if (folder is null)
                return;

            await WallpaperDirectoryChange(folder.Path);
        }

        private bool CanExecuteDirectoryChange() => !WallpaperDirectoryChangeOngoing;

        public async Task WallpaperDirectoryChange(string newDir)
        {
            bool isDestEmptyDir = false;
            if (string.Equals(newDir, userSettings.Settings.WallpaperDir, StringComparison.OrdinalIgnoreCase))
            {
                // Refresh request.
                libraryVm.UpdateWallpaperDirectory(userSettings.Settings.WallpaperDir);
                return;
            }

            try
            {
                var parentDir = Directory.GetParent(newDir).ToString();
                if (parentDir != null)
                {
                    if (Directory.Exists(Path.Combine(parentDir, Constants.CommonPartialPaths.WallpaperInstallDir)) &&
                        Directory.Exists(Path.Combine(parentDir, Constants.CommonPartialPaths.WallpaperSettingsDir)))
                    {
                        //User selected wrong directory, lively needs the SaveData folder also(root).
                        newDir = parentDir;
                    }
                }

                WallpaperDirectoryChangeOngoing = true;
                WallpaperDirectoryChangeCommand.NotifyCanExecuteChanged();
                //create destination directory's if not exist.
                Directory.CreateDirectory(Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallDir));
                Directory.CreateDirectory(Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallTempDir));
                Directory.CreateDirectory(Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperSettingsDir));

                if (userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir)
                {
                    await Task.Run(() =>
                    {
                        FileUtil.DirectoryCopy(Path.Combine(WallpaperDirectory, Constants.CommonPartialPaths.WallpaperInstallDir),
                            Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallDir), true);
                        FileUtil.DirectoryCopy(Path.Combine(WallpaperDirectory, Constants.CommonPartialPaths.WallpaperInstallTempDir),
                            Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallTempDir), true);
                        FileUtil.DirectoryCopy(Path.Combine(WallpaperDirectory, Constants.CommonPartialPaths.WallpaperSettingsDir),
                            Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperSettingsDir), true);
                    });
                }
                else
                {
                    isDestEmptyDir = true;
                }
            }
            catch (Exception)
            {
                //TODO: log
                return;
            }
            finally
            {
                WallpaperDirectoryChangeOngoing = false;
                WallpaperDirectoryChangeCommand.NotifyCanExecuteChanged();
            }

            //exit all running wp's immediately
            await desktopCore.CloseAllWallpapers(true);

            var previousDirectory = userSettings.Settings.WallpaperDir;
            userSettings.Settings.WallpaperDir = newDir;
            UpdateSettingsConfigFile();
            WallpaperDirectory = userSettings.Settings.WallpaperDir;
            libraryVm.UpdateWallpaperDirectory(newDir);

            if (!isDestEmptyDir)
            {
                //not deleting the root folder, what if the user selects a folder that is not used by Lively alone!
                var result1 = await FileUtil.TryDeleteDirectoryAsync(Path.Combine(previousDirectory, Constants.CommonPartialPaths.WallpaperInstallDir), 1000, 3000);
                var result2 = await FileUtil.TryDeleteDirectoryAsync(Path.Combine(previousDirectory, Constants.CommonPartialPaths.WallpaperInstallTempDir), 0, 1000);
                var result3 = await FileUtil.TryDeleteDirectoryAsync(Path.Combine(previousDirectory, Constants.CommonPartialPaths.WallpaperSettingsDir), 0, 1000);
                if (!(result1 && result2 && result3))
                {
                    //TODO: Dialogue
                }
            }
        }

        private static async Task<StorageFolder> ShowFolderPicker(string filter)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            folderPicker.FileTypeFilter.Add(filter);
            return await folderPicker.PickSingleFolderAsync();
        }

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<SettingsModel>();
            });
        }
    }
}
