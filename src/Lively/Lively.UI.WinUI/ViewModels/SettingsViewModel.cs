using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Factories;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Localization;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Extensions;
using Lively.UI.WinUI.Factories;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        public event EventHandler<string> WallpaperDirChanged;
        private readonly DispatcherQueue dispatcherQueue;

        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly ICommandsClient commands;
        private readonly IApplicationsRulesFactory appRuleFactory;
        private readonly IDialogService dialogService;
        //private readonly IScreensaverService screenSaver;
        //private readonly IAppUpdaterService appUpdater;
        //private readonly ITransparentTbService ttbService;

        public SettingsViewModel(
            IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            ICommandsClient commands,
            IDialogService dialogService,
            IApplicationsRulesFactory appRuleFactory)
        //IScreensaverService screenSaver, 
        //IAppUpdaterService appUpdater, 
        //ITransparentTbService ttbService)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.commands = commands;
            this.appRuleFactory = appRuleFactory;
            this.dialogService = dialogService;
            //this.screenSaver = screenSaver;
            //this.appUpdater = appUpdater;
            //this.ttbService = ttbService;

            //MainWindow dispatcher may not be ready yet, creating our own instead..
            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;

            //lang-codes: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
            LanguageItems = new ObservableCollection<LanguagesModel>(SupportedLanguages.Languages);

            SelectedTileSizeIndex = userSettings.Settings.TileSize;
            SelectedAppFullScreenIndex = (int)userSettings.Settings.AppFullscreenPause;
            SelectedAppFocusIndex = (int)userSettings.Settings.AppFocusPause;
            SelectedBatteryPowerIndex = (int)userSettings.Settings.BatteryPause;
            SelectedRemoteDestopPowerIndex = (int)userSettings.Settings.RemoteDesktopPause;
            SelectedPowerSaveModeIndex = (int)userSettings.Settings.PowerSaveModePause;
            SelectedDisplayPauseRuleIndex = (int)userSettings.Settings.DisplayPauseSettings;
            SelectedPauseAlgorithmIndex = (int)userSettings.Settings.ProcessMonitorAlgorithm;
            SelectedVideoPlayerIndex = (int)userSettings.Settings.VideoPlayer;
            VideoPlayerHWDecode = userSettings.Settings.VideoPlayerHwAccel;
            SelectedGifPlayerIndex = (int)userSettings.Settings.GifPlayer;
            SelectedWallpaperStreamQualityIndex = (int)userSettings.Settings.StreamQuality;
            SelectedWallpaperInputMode = (int)userSettings.Settings.InputForward;
            MouseMoveOnDesktop = userSettings.Settings.MouseInputMovAlways;
            IsSysTrayIconVisible = userSettings.Settings.SysTrayIcon;
            IsReducedMotion = userSettings.Settings.UIMode != LivelyGUIState.normal;
            WebDebuggingPort = userSettings.Settings.WebDebugPort;
            DetectStreamWallpaper = userSettings.Settings.AutoDetectOnlineStreams;
            WallpaperDirectory = userSettings.Settings.WallpaperDir;
            MoveExistingWallpaperNewDir = userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir;
            GlobalWallpaperVolume = userSettings.Settings.AudioVolumeGlobal;
            IsAudioOnlyOnDesktop = userSettings.Settings.AudioOnlyOnDesktop;
            SelectedWallpaperScalingIndex = (int)userSettings.Settings.WallpaperScaling;
            CefDiskCache = userSettings.Settings.CefDiskCache;
            //IsLockScreenAutoWallpaper = userSettings.Settings.LockScreenAutoWallpaper;
            SelectedTaskbarThemeIndex = (int)userSettings.Settings.SystemTaskbarTheme;
            IsDesktopAutoWallpaper = userSettings.Settings.DesktopAutoWallpaper;
            //IsDebugMenuVisible = userSettings.Settings.DebugMenu;
            SelectedWebBrowserIndex = (int)userSettings.Settings.WebBrowser;
            //SelectedScreensaverWaitIndex = (int)userSettings.Settings.ScreensaverIdleDelay;
            //IsScreensaverLockOnResume = userSettings.Settings.ScreensaverLockOnResume;
            IsKeepUIAwake = userSettings.Settings.KeepAwakeUI;
            IsStartup = userSettings.Settings.Startup;
            SelectedLanguageItem = SupportedLanguages.GetLanguage(userSettings.Settings.Language);

            //Only pause action is shown to user, rest is for internal use by editing the json file manually..
            AppRules = new ObservableCollection<ApplicationRulesModel>(userSettings.AppRules.Where(x => x.Rule == AppRulesEnum.pause));
        }

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<SettingsModel>();
            });
        }

        public void UpdateAppRulesConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<List<ApplicationRulesModel>>();
            });
        }

        public bool IsNotWinStore => !Constants.ApplicationType.IsMSIX;

        #region general

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

        private int _selectedTileSizeIndex;
        public int SelectedTileSizeIndex
        {
            get => _selectedTileSizeIndex;
            set
            {
                if (userSettings.Settings.TileSize != value)
                {
                    userSettings.Settings.TileSize = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedTileSizeIndex, value);
            }
        }

        [ObservableProperty]
        private string wallpaperDirectory;

        [ObservableProperty]
        private bool wallpaperDirectoryChangeOngoing;

        private RelayCommand _wallpaperDirectoryChangeCommand;
        public RelayCommand WallpaperDirectoryChangeCommand => _wallpaperDirectoryChangeCommand
            ??= new RelayCommand(WallpaperDirectoryChange, () => !WallpaperDirectoryChangeOngoing);

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

        private RelayCommand _openWallpaperDirectory;
        public RelayCommand OpenWallpaperDirectory =>
            _openWallpaperDirectory ??= new RelayCommand(async () => await DesktopBridgeUtil.OpenFolder(userSettings.Settings.WallpaperDir));

        private RelayCommand _themeBackgroundCommand;
        public RelayCommand ThemeBackgroundCommand => _themeBackgroundCommand ??= new RelayCommand(async () => await dialogService.ShowThemeDialogAsync());

        #endregion general

        #region performance

        private RelayCommand _applicationRulesCommand;
        public RelayCommand ApplicationRulesCommand =>
            _applicationRulesCommand ??= new RelayCommand(ShowApplicationRulesWindow);

        private void ShowApplicationRulesWindow()
        {
            /*
            _ = new ApplicationRulesView()
            {
                Owner = App.Services.GetRequiredService<MainWindow>(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            }.ShowDialog();
            */
        }

        private int _selectedAppFullScreenIndex;
        public int SelectedAppFullScreenIndex
        {
            get => _selectedAppFullScreenIndex;
            set
            {
                if (userSettings.Settings.AppFullscreenPause != (AppRulesEnum)value)
                {
                    userSettings.Settings.AppFullscreenPause = (AppRulesEnum)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedAppFullScreenIndex, value);
            }
        }

        private int _selectedAppFocusIndex;
        public int SelectedAppFocusIndex
        {
            get => _selectedAppFocusIndex;
            set
            {
                if (userSettings.Settings.AppFocusPause != (AppRulesEnum)value)
                {
                    userSettings.Settings.AppFocusPause = (AppRulesEnum)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedAppFocusIndex, value);
            }
        }

        private int _selectedBatteryPowerIndex;
        public int SelectedBatteryPowerIndex
        {
            get => _selectedBatteryPowerIndex;
            set
            {
                if (userSettings.Settings.BatteryPause != (AppRulesEnum)value)
                {
                    userSettings.Settings.BatteryPause = (AppRulesEnum)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedBatteryPowerIndex, value);
            }
        }

        private int _selectedPowerSaveModeIndex;
        public int SelectedPowerSaveModeIndex
        {
            get => _selectedPowerSaveModeIndex;
            set
            {
                if (userSettings.Settings.PowerSaveModePause != (AppRulesEnum)value)
                {
                    userSettings.Settings.PowerSaveModePause = (AppRulesEnum)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedPowerSaveModeIndex, value);
            }
        }

        private int _selectedRemoteDestopPowerIndex;
        public int SelectedRemoteDestopPowerIndex
        {
            get => _selectedRemoteDestopPowerIndex;
            set
            {
                if (userSettings.Settings.RemoteDesktopPause != (AppRulesEnum)value)
                {
                    userSettings.Settings.RemoteDesktopPause = (AppRulesEnum)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedRemoteDestopPowerIndex, value);
            }
        }

        private int _selectedDisplayPauseRuleIndex;
        public int SelectedDisplayPauseRuleIndex
        {
            get => _selectedDisplayPauseRuleIndex;
            set
            {
                if (userSettings.Settings.DisplayPauseSettings != (DisplayPauseEnum)value)
                {
                    userSettings.Settings.DisplayPauseSettings = (DisplayPauseEnum)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedDisplayPauseRuleIndex, value);
            }
        }

        private int _selectedPauseAlgorithmIndex;
        public int SelectedPauseAlgorithmIndex
        {
            get => _selectedPauseAlgorithmIndex;
            set
            {
                if (userSettings.Settings.ProcessMonitorAlgorithm != (ProcessMonitorAlgorithm)value)
                {
                    userSettings.Settings.ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedPauseAlgorithmIndex, value);
            }
        }

        #region apprules

        [ObservableProperty]
        private ObservableCollection<ApplicationRulesModel> appRules = new();

        private ApplicationRulesModel _selectedAppRuleItem;
        public ApplicationRulesModel SelectedAppRuleItem
        {
            get => _selectedAppRuleItem;
            set
            {
                SetProperty(ref _selectedAppRuleItem, value);
                RemoveAppRuleCommand.NotifyCanExecuteChanged();
            }
        }

        private RelayCommand _addAppRuleCommand;
        public RelayCommand AddAppRuleCommand => _addAppRuleCommand ??= new RelayCommand(async () => await AppRuleAddProgram());

        private RelayCommand _removeAppRuleCommand;
        public RelayCommand RemoveAppRuleCommand =>
            _removeAppRuleCommand ??= new RelayCommand(AppRuleRemoveProgram, () => SelectedAppRuleItem != null);

        private async Task AppRuleAddProgram()
        {
            var result = await dialogService.ShowApplicationPickerDialogAsync();
            if (result != null)
            {
                try
                {
                    var rule = appRuleFactory.CreateAppRule(result.AppPath, AppRulesEnum.pause);
                    if (AppRules.Any(x => x.AppName.Equals(rule.AppName, StringComparison.Ordinal)))
                    {
                        return;
                    }
                    userSettings.AppRules.Add(rule);
                    AppRules.Add(rule);
                    UpdateAppRulesConfigFile();
                }
                catch (Exception)
                {
                    //TODO
                }
            }
        }

        private void AppRuleRemoveProgram()
        {
            userSettings.AppRules.Remove(SelectedAppRuleItem);
            AppRules.Remove(SelectedAppRuleItem);
            UpdateAppRulesConfigFile();
        }


        #endregion //apprules

        #endregion performance

        #region wallpaper

        private int _selectedWallpaperScalingIndex;
        public int SelectedWallpaperScalingIndex
        {
            get => _selectedWallpaperScalingIndex;
            set
            {
                if (userSettings.Settings.WallpaperScaling != (WallpaperScaler)value)
                {
                    userSettings.Settings.WallpaperScaling = (WallpaperScaler)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.video, WallpaperType.picture, WallpaperType.videostream, WallpaperType.gif });
                }
                SetProperty(ref  _selectedWallpaperScalingIndex, value);
            }
        }

        private int _selectedWallpaperInputMode;
        public int SelectedWallpaperInputMode
        {
            get => _selectedWallpaperInputMode;
            set
            {
                if (userSettings.Settings.InputForward != (InputForwardMode)value)
                {
                    userSettings.Settings.InputForward = (InputForwardMode)value;
                    UpdateSettingsConfigFile();
                }

                if (userSettings.Settings.InputForward == InputForwardMode.mousekeyboard)
                {
                    DesktopUtil.SetDesktopIconVisibility(false);
                    IsDesktopIconsHidden = true;
                }
                else
                {
                    DesktopUtil.SetDesktopIconVisibility(DesktopUtil.DesktopIconVisibilityDefault);
                    IsDesktopIconsHidden = false;
                }
                SetProperty(ref _selectedWallpaperInputMode, value);
            }
        }

        [ObservableProperty]
        private bool isDesktopIconsHidden;

        private bool _mouseMoveOnDesktop;
        public bool MouseMoveOnDesktop
        {
            get => _mouseMoveOnDesktop;
            set
            {
                if (userSettings.Settings.MouseInputMovAlways != value)
                {
                    userSettings.Settings.MouseInputMovAlways = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _mouseMoveOnDesktop, value);
            }
        }

        [ObservableProperty]
        private bool isSelectedVideoPlayerAvailable;

        private int _selectedVideoPlayerIndex;
        public int SelectedVideoPlayerIndex
        {
            get => _selectedVideoPlayerIndex;
            set
            {
                IsSelectedVideoPlayerAvailable = IsVideoPlayerAvailable((LivelyMediaPlayer)value);
                if (userSettings.Settings.VideoPlayer != (LivelyMediaPlayer)value && IsSelectedVideoPlayerAvailable)
                {
                    userSettings.Settings.VideoPlayer = (LivelyMediaPlayer)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.video, WallpaperType.picture, WallpaperType.videostream });
                }
                SetProperty(ref _selectedVideoPlayerIndex, value);
            }
        }

        private bool _videoPlayerHWDecode;
        public bool VideoPlayerHWDecode
        {
            get => _videoPlayerHWDecode;
            set
            {
                if (userSettings.Settings.VideoPlayerHwAccel != value)
                {
                    userSettings.Settings.VideoPlayerHwAccel = value;
                    UpdateSettingsConfigFile();
                    //if mpv player is also set as gif player..
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.video, WallpaperType.videostream, WallpaperType.gif });
                }
                SetProperty(ref _videoPlayerHWDecode, value);
            }
        }

        [ObservableProperty]
        private bool isSelectedGifPlayerAvailable;

        private int _selectedGifPlayerIndex;
        public int SelectedGifPlayerIndex
        {
            get => _selectedGifPlayerIndex;
            set
            {
                IsSelectedGifPlayerAvailable = IsGifPlayerAvailable((LivelyGifPlayer)value);
                if (userSettings.Settings.GifPlayer != (LivelyGifPlayer)value && IsSelectedGifPlayerAvailable)
                {
                    userSettings.Settings.GifPlayer = (LivelyGifPlayer)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.gif, WallpaperType.picture });
                }
                SetProperty(ref _selectedGifPlayerIndex, value);
            }
        }

        [ObservableProperty]
        private bool isSelectedWebBrowserAvailable;

        private int _selectedWebBrowserIndex;
        public int SelectedWebBrowserIndex
        {
            get => _selectedWebBrowserIndex;
            set
            {
                IsSelectedWebBrowserAvailable = IsWebPlayerAvailable((LivelyWebBrowser)value);
                if (userSettings.Settings.WebBrowser != (LivelyWebBrowser)value && IsSelectedWebBrowserAvailable)
                {
                    userSettings.Settings.WebBrowser = (LivelyWebBrowser)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.web, WallpaperType.webaudio, WallpaperType.url, WallpaperType.videostream });
                }
                SetProperty(ref _selectedWebBrowserIndex, value);
            }
        }

        private string _webDebuggingPort;
        public string WebDebuggingPort
        {
            get => _webDebuggingPort;
            set
            {
                if (userSettings.Settings.WebDebugPort != value)
                {
                    userSettings.Settings.WebDebugPort = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _webDebuggingPort, value);
            }
        }

        private bool _cefDiskCache;
        public bool CefDiskCache
        {
            get => _cefDiskCache;
            set
            {
                if (userSettings.Settings.CefDiskCache != value)
                {
                    userSettings.Settings.CefDiskCache = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _cefDiskCache, value);
            }
        }

        public bool IsStreamSupported
        {
            get
            {
                try
                {
                    return File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "mpv", "youtube-dl.exe"));
                }
                catch
                {
                    return false;
                }
            }
        }

        private int _selectedWallpaperStreamQualityIndex;
        public int SelectedWallpaperStreamQualityIndex
        {
            get => _selectedWallpaperStreamQualityIndex;
            set
            {
                if (userSettings.Settings.StreamQuality != (StreamQualitySuggestion)value)
                {
                    userSettings.Settings.StreamQuality = (StreamQualitySuggestion)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.videostream });
                }
                SetProperty(ref _selectedWallpaperStreamQualityIndex, value);
            }
        }

        private bool _detectStreamWallpaper;
        public bool DetectStreamWallpaper
        {
            get => _detectStreamWallpaper;
            set
            {
                if (userSettings.Settings.AutoDetectOnlineStreams != value)
                {
                    userSettings.Settings.AutoDetectOnlineStreams = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _detectStreamWallpaper, value);
            }
        }

        #endregion wallpaper

        #region audio

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

        #endregion //audio

        #region system


        //private bool _isLockScreenAutoWallpaper;
        //public bool IsLockScreenAutoWallpaper
        //{
        //    get => _isLockScreenAutoWallpaper;
        //    set
        //    {
        //        if (userSettings.Settings.LockScreenAutoWallpaper != value)
        //        {
        //            userSettings.Settings.LockScreenAutoWallpaper = value;
        //            UpdateSettingsConfigFile();
        //        }
        //        SetProperty(ref _isLockScreenAutoWallpaper, value);
        //    }
        //}

        private bool _isDesktopAutoWallpaper;
        public bool IsDesktopAutoWallpaper
        {
            get => _isDesktopAutoWallpaper;
            set
            {
                if (userSettings.Settings.DesktopAutoWallpaper != value)
                {
                    userSettings.Settings.DesktopAutoWallpaper = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isDesktopAutoWallpaper, value);
            }
        }

        private int _selectedTaskbarThemeIndex;
        public int SelectedTaskbarThemeIndex
        {
            get => _selectedTaskbarThemeIndex;
            set
            {
                if (userSettings.Settings.SystemTaskbarTheme != (TaskbarTheme)value)
                {
                    userSettings.Settings.SystemTaskbarTheme = (TaskbarTheme)value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _selectedTaskbarThemeIndex, value);
            }
        }

        //private int _selectedScreensaverWaitIndex;
        //public int SelectedScreensaverWaitIndex
        //{
        //    get
        //    {
        //        return _selectedScreensaverWaitIndex;
        //    }
        //    set
        //    {
        //        _selectedScreensaverWaitIndex = value;
        //        uint idleTime = (ScreensaverIdleTime)_selectedScreensaverWaitIndex switch
        //        {
        //            ScreensaverIdleTime.none => 0,
        //            ScreensaverIdleTime.min1 => 60000,
        //            ScreensaverIdleTime.min2 => 120000,
        //            ScreensaverIdleTime.min3 => 180000,
        //            ScreensaverIdleTime.min5 => 300000,
        //            ScreensaverIdleTime.min10 => 600000,
        //            ScreensaverIdleTime.min15 => 900000,
        //            ScreensaverIdleTime.min20 => 1200000,
        //            ScreensaverIdleTime.min25 => 1500000,
        //            ScreensaverIdleTime.min30 => 1800000,
        //            ScreensaverIdleTime.min45 => 2700000,
        //            ScreensaverIdleTime.min60 => 3600000,
        //            ScreensaverIdleTime.min120 => 7200000,
        //            _ => 0,
        //        };
        //        if (idleTime != 0)
        //        {
        //            //screenSaver.StartIdleTimer(idleTime);
        //        }
        //        else
        //        {
        //            //screenSaver.StopIdleTimer();
        //        }
        //        //save the data..
        //        if (userSettings.Settings.ScreensaverIdleDelay != (ScreensaverIdleTime)_selectedScreensaverWaitIndex)
        //        {
        //            if (!userSettings.Settings.ScreensaverOledWarning)
        //            {
        //                //_ = Task.Run(() =>
        //                //       System.Windows.MessageBox.Show(Properties.Resources.DescOledScreensaverNotice,
        //                //           Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information));
        //                userSettings.Settings.ScreensaverOledWarning = true;
        //            }
        //            userSettings.Settings.ScreensaverIdleDelay = (ScreensaverIdleTime)_selectedScreensaverWaitIndex;
        //            UpdateSettingsConfigFile();
        //        }
        //        OnPropertyChanged();
        //    }
        //}

        //private bool _isScreensaverLockOnResume;
        //public bool IsScreensaverLockOnResume
        //{
        //    get => _isScreensaverLockOnResume;
        //    set
        //    {
        //        if (userSettings.Settings.ScreensaverLockOnResume != value)
        //        {
        //            userSettings.Settings.ScreensaverLockOnResume = value;
        //            UpdateSettingsConfigFile();
        //        }
        //        SetProperty(ref _isScreensaverLockOnResume, value);
        //    }
        //}

        #endregion //system

        #region misc

        private bool _isSysTrayIconVisible;
        public bool IsSysTrayIconVisible
        {
            get => _isSysTrayIconVisible;
            set
            {
                if (userSettings.Settings.SysTrayIcon != value)
                {
                    //_ = commands.AutomationCommandAsync(new string[] { "--showTray", JsonUtil.Serialize(value) });
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
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isReducedMotion, value);
            }
        }

        private bool _isKeepUIAwake;
        public bool IsKeepUIAwake
        {
            get => _isKeepUIAwake;
            set
            {
                if (userSettings.Settings.KeepAwakeUI != value)
                {
                    userSettings.Settings.KeepAwakeUI = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isKeepUIAwake, value);
            }
        }

        private RelayCommand _showDebugCommand;
        public RelayCommand ShowDebugCommand => _showDebugCommand ??= new RelayCommand(() => commands.ShowDebugger());

        private RelayCommand _extractLogCommand;
        public RelayCommand ExtractLogCommand => _extractLogCommand ??= new RelayCommand(ExtractLog);

        private async void ExtractLog()
        {
            var filePicker = new FileSavePicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            filePicker.FileTypeChoices.Add("Compressed archive", new List<string>() { ".zip" });
            filePicker.SuggestedFileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var file = await filePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    LogUtil.ExtractLogFiles(file.Path);
                }
                catch (Exception ex)
                {
                    await dialogService.ShowDialogAsync(ex.Message, "Error", "OK");
                }
            }
        }

        /*
        public string SwitchBranchText => Constants.ApplicationType.IsTestBuild ? Properties.Resources.TextSwitchBranchOfficial : Properties.Resources.TextSwitchBranchDev;

        private bool canSwitchBranchCommand = !Constants.ApplicationType.IsMSIX;
        private RelayCommand _switchBranchCommand;
        public RelayCommand SwitchBranchCommand
        {
            get
            {
                if (_switchBranchCommand == null)
                {
                    _switchBranchCommand = new RelayCommand(
                            param => _ = BranchSwitchDialog(),
                            param => canSwitchBranchCommand
                        );
                }
                return _switchBranchCommand;
            }
        }

        private async Task BranchSwitchDialog()
        {
            try
            {
                canSwitchBranchCommand = false;
                SwitchBranchCommand.RaiseCanExecuteChanged();
                (Uri, Version, string) item = await appUpdater.GetLatestRelease(!Constants.ApplicationType.IsTestBuild);
                var msg = Constants.ApplicationType.IsTestBuild ?
                    item.Item3 : $"!! {Properties.Resources.TitleWarning} !!\n{Properties.Resources.DescSwitchBranchBetaWarning}\n\n{Properties.Resources.TitleChangelog}\n{item.Item3}";
                (new AppUpdaterView(item.Item1, msg)
                {
                    Owner = App.Services.GetRequiredService<MainWindow>(),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }).ShowDialog();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
            finally
            {
                canSwitchBranchCommand = true;
                SwitchBranchCommand.RaiseCanExecuteChanged();
            }
        }
        */

        #endregion //misc

        #region helper fns

        private bool IsVideoPlayerAvailable(LivelyMediaPlayer mp)
        {
            return mp switch
            {
                LivelyMediaPlayer.libvlc => false, //depreciated
                LivelyMediaPlayer.libmpv => false, //depreciated
                LivelyMediaPlayer.wmf => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "wmf", "Lively.PlayerWmf.exe")),
                LivelyMediaPlayer.libvlcExt => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "libVLCPlayer", "libVLCPlayer.exe")),
                LivelyMediaPlayer.libmpvExt => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyMediaPlayer.mpv => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "mpv", "mpv.exe")),
                LivelyMediaPlayer.vlc => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "vlc", "vlc.exe")),
                _ => false,
            };
        }

        private bool IsGifPlayerAvailable(LivelyGifPlayer gp)
        {
            return gp switch
            {
                LivelyGifPlayer.win10Img => false, //xaml island
                LivelyGifPlayer.libmpvExt => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyGifPlayer.mpv => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "mpv", "mpv.exe")),
                _ => false,
            };
        }

        private bool IsWebPlayerAvailable(LivelyWebBrowser wp)
        {
            return wp switch
            {
                LivelyWebBrowser.cef => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "Cef", "Lively.PlayerCefSharp.exe")),
                LivelyWebBrowser.webview2 => File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "Wv2", "Lively.PlayerWebView2.exe")),
                _ => false,
            };
        }

        private async Task WallpaperRestart(WallpaperType[] type)
        {
            var originalWallpapers = desktopCore.Wallpapers.Where(x => type.Any(y => y == x.Category)).ToList();
            if (originalWallpapers.Any())
            {
                foreach (var item in type)
                {
                    await desktopCore.CloseWallpaper(item, true);
                }

                foreach (var item in originalWallpapers)
                {
                    await desktopCore.SetWallpaper(item.LivelyInfoFolderPath, item.Display.DeviceId);
                    if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span
                        || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                    {
                        break;
                    }
                }
            }
        }

        private async void WallpaperDirectoryChange()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            folderPicker.FileTypeFilter.Add("*");
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await WallpaperDirectoryChange(folder.Path);
            }
        }

        public async Task WallpaperDirectoryChange(string newDir)
        {
            bool isDestEmptyDir = false;
            if (string.Equals(newDir, userSettings.Settings.WallpaperDir, StringComparison.OrdinalIgnoreCase))
            {
                //refresh request to other classes..
                WallpaperDirChanged?.Invoke(this, userSettings.Settings.WallpaperDir);
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
            WallpaperDirChanged?.Invoke(this, newDir);

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

        #endregion //helper fns
    }
}
