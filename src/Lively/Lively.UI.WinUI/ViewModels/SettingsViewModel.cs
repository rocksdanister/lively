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
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Localization;
using Lively.Common.Helpers.MVVM;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Factories;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.ViewModels
{
    public class SettingsViewModel : ObservableObject
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
            SelectedLivelyUIModeIndex = (int)userSettings.Settings.UIMode;
            SelectedWallpaperInputMode = (int)userSettings.Settings.InputForward;
            MouseMoveOnDesktop = userSettings.Settings.MouseInputMovAlways;
            IsSysTrayIconVisible = userSettings.Settings.SysTrayIcon;
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
            SelectedAppThemeIndex = (int)userSettings.Settings.ApplicationTheme;
            //SelectedScreensaverWaitIndex = (int)userSettings.Settings.ScreensaverIdleDelay;
            //IsScreensaverLockOnResume = userSettings.Settings.ScreensaverLockOnResume;
            IsKeepUIAwake = userSettings.Settings.KeepAwakeUI;
            IsStartup = userSettings.Settings.Startup;
            SelectedLanguageItem = SupportedLanguages.GetLanguage(userSettings.Settings.Language);

            //Only pause action is shown to user, rest is for internal use by editing the json file manually..
            AppRules = new ObservableCollection<IApplicationRulesModel>(userSettings.AppRules.Where(x => x.Rule == AppRulesEnum.pause));
        }

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<ISettingsModel>();
            });
        }

        public void UpdateAppRulesConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<List<IApplicationRulesModel>>();
            });
        }

        public bool IsNotWinStore => !Constants.ApplicationType.IsMSIX;

        #region general

        private bool _isStartup;
        public bool IsStartup
        {
            get
            {
                return _isStartup;
            }
            set
            {
                _isStartup = value;
                if (userSettings.Settings.Startup != _isStartup)
                {
                    userSettings.Settings.Startup = _isStartup;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        private ObservableCollection<LanguagesModel> _languageItems;
        public ObservableCollection<LanguagesModel> LanguageItems
        {
            get
            {
                return _languageItems;
            }
            set
            {

                _languageItems = value;
                OnPropertyChanged();
            }
        }

        private LanguagesModel _selectedLanguageItem;
        public LanguagesModel SelectedLanguageItem
        {
            get
            {
                return _selectedLanguageItem;
            }
            set
            {
                _selectedLanguageItem = value;
                OnPropertyChanged();
                if (_selectedLanguageItem.Codes.FirstOrDefault(x => x == userSettings.Settings.Language) == null)
                {
                    userSettings.Settings.Language = _selectedLanguageItem.Codes[0];
                    UpdateSettingsConfigFile();
                }
            }
        }

        private int _selectedTileSizeIndex;
        public int SelectedTileSizeIndex
        {
            get
            {
                return _selectedTileSizeIndex;
            }
            set
            {
                _selectedTileSizeIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.TileSize != _selectedTileSizeIndex)
                {
                    userSettings.Settings.TileSize = _selectedTileSizeIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        public event EventHandler<LivelyGUIState> UIStateChanged;
        private int _selectedLivelyUIModeIndex;
        public int SelectedLivelyUIModeIndex
        {
            get
            {
                return _selectedLivelyUIModeIndex;
            }
            set
            {
                _selectedLivelyUIModeIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.UIMode != (LivelyGUIState)value)
                {
                    userSettings.Settings.UIMode = (LivelyGUIState)value;
                    UpdateSettingsConfigFile();

                    UIStateChanged?.Invoke(this, (LivelyGUIState)value);
                }
            }
        }

        private string _wallpaperDirectory;
        public string WallpaperDirectory
        {
            get { return _wallpaperDirectory; }
            set
            {
                _wallpaperDirectory = value;
                OnPropertyChanged();
            }
        }

        private bool _wallpaperDirectoryChangeOngoing;
        public bool WallpaperDirectoryChangeOngoing
        {
            get => _wallpaperDirectoryChangeOngoing;
            set
            {
                _wallpaperDirectoryChangeOngoing = value;
                OnPropertyChanged();
            }
        }

        private RelayCommand _wallpaperDirectoryChangeCommand;
        public RelayCommand WallpaperDirectoryChangeCommand => _wallpaperDirectoryChangeCommand
            ??= new RelayCommand(WallpaperDirectoryChange, () => !WallpaperDirectoryChangeOngoing);

        private bool _moveExistingWallpaperNewDir;
        public bool MoveExistingWallpaperNewDir
        {
            get { return _moveExistingWallpaperNewDir; }
            set
            {
                _moveExistingWallpaperNewDir = value;
                OnPropertyChanged();

                if (userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir != _moveExistingWallpaperNewDir)
                {
                    userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir = _moveExistingWallpaperNewDir;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private RelayCommand _openWallpaperDirectory;
        public RelayCommand OpenWallpaperDirectory =>
            _openWallpaperDirectory ??= new RelayCommand(async () => await DesktopBridgeUtil.OpenFolder(userSettings.Settings.WallpaperDir));

        //public event EventHandler<AppTheme> AppThemeChanged;
        private int _selectedAppThemeIndex;
        public int SelectedAppThemeIndex
        {
            get
            {
                return _selectedAppThemeIndex;
            }
            set
            {
                _selectedAppThemeIndex = value;
                OnPropertyChanged();

                //prevent running on startup etc.
                if (userSettings.Settings.ApplicationTheme != (AppTheme)value)
                {
                    userSettings.Settings.ApplicationTheme = (AppTheme)value;
                    UpdateSettingsConfigFile();
                    //AppThemeChanged?.Invoke(this, userSettings.Settings.ApplicationTheme);
                }
            }
        }

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
            get
            {
                return _selectedAppFullScreenIndex;
            }
            set
            {
                _selectedAppFullScreenIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.AppFullscreenPause != (AppRulesEnum)_selectedAppFullScreenIndex)
                {
                    userSettings.Settings.AppFullscreenPause = (AppRulesEnum)_selectedAppFullScreenIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private int _selectedAppFocusIndex;
        public int SelectedAppFocusIndex
        {
            get
            {
                return _selectedAppFocusIndex;
            }
            set
            {
                _selectedAppFocusIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.AppFocusPause != (AppRulesEnum)_selectedAppFocusIndex)
                {
                    userSettings.Settings.AppFocusPause = (AppRulesEnum)_selectedAppFocusIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private int _selectedBatteryPowerIndex;
        public int SelectedBatteryPowerIndex
        {
            get
            {
                return _selectedBatteryPowerIndex;
            }
            set
            {
                _selectedBatteryPowerIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.BatteryPause != (AppRulesEnum)_selectedBatteryPowerIndex)
                {
                    userSettings.Settings.BatteryPause = (AppRulesEnum)_selectedBatteryPowerIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private int _selectedPowerSaveModeIndex;
        public int SelectedPowerSaveModeIndex
        {
            get
            {
                return _selectedPowerSaveModeIndex;
            }
            set
            {
                _selectedPowerSaveModeIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.PowerSaveModePause != (AppRulesEnum)_selectedPowerSaveModeIndex)
                {
                    userSettings.Settings.PowerSaveModePause = (AppRulesEnum)_selectedPowerSaveModeIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private int _selectedRemoteDestopPowerIndex;
        public int SelectedRemoteDestopPowerIndex
        {
            get
            {
                return _selectedRemoteDestopPowerIndex;
            }
            set
            {
                _selectedRemoteDestopPowerIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.RemoteDesktopPause != (AppRulesEnum)_selectedRemoteDestopPowerIndex)
                {
                    userSettings.Settings.RemoteDesktopPause = (AppRulesEnum)_selectedRemoteDestopPowerIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private int _selectedDisplayPauseRuleIndex;
        public int SelectedDisplayPauseRuleIndex
        {
            get
            {
                return _selectedDisplayPauseRuleIndex;
            }
            set
            {
                _selectedDisplayPauseRuleIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.DisplayPauseSettings != (DisplayPauseEnum)_selectedDisplayPauseRuleIndex)
                {
                    userSettings.Settings.DisplayPauseSettings = (DisplayPauseEnum)_selectedDisplayPauseRuleIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private int _selectedPauseAlgorithmIndex;
        public int SelectedPauseAlgorithmIndex
        {
            get
            {
                return _selectedPauseAlgorithmIndex;
            }
            set
            {
                _selectedPauseAlgorithmIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.ProcessMonitorAlgorithm != (ProcessMonitorAlgorithm)_selectedPauseAlgorithmIndex)
                {
                    userSettings.Settings.ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)_selectedPauseAlgorithmIndex;
                    UpdateSettingsConfigFile();
                }
            }
        }

        #region apprules

        private ObservableCollection<IApplicationRulesModel> _appRules;
        public ObservableCollection<IApplicationRulesModel> AppRules
        {
            get
            {
                return _appRules ?? new ObservableCollection<IApplicationRulesModel>();
            }
            set
            {
                _appRules = value;
                OnPropertyChanged();
            }
        }

        private IApplicationRulesModel _selectedAppRuleItem;
        public IApplicationRulesModel SelectedAppRuleItem
        {
            get { return _selectedAppRuleItem; }
            set
            {
                _selectedAppRuleItem = value;
                RemoveAppRuleCommand.NotifyCanExecuteChanged();
                OnPropertyChanged();
            }
        }

        private RelayCommand _addAppRuleCommand;
        public RelayCommand AddAppRuleCommand => _addAppRuleCommand ??= new RelayCommand(async() => await AppRuleAddProgram());

        private RelayCommand _removeAppRuleCommand;
        public RelayCommand RemoveAppRuleCommand =>
            _removeAppRuleCommand ??= new RelayCommand(AppRuleRemoveProgram, () => SelectedAppRuleItem != null);

        private async Task AppRuleAddProgram()
        {
            var result = await dialogService.ShowApplicationPickerDialog();
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
            get { return _selectedWallpaperScalingIndex; }
            set
            {
                _selectedWallpaperScalingIndex = value;
                OnPropertyChanged();

                if (userSettings.Settings.WallpaperScaling != (WallpaperScaler)_selectedWallpaperScalingIndex)
                {
                    userSettings.Settings.WallpaperScaling = (WallpaperScaler)_selectedWallpaperScalingIndex;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.video, WallpaperType.picture, WallpaperType.videostream, WallpaperType.gif });
                }
            }
        }

        private int _selectedWallpaperInputMode;
        public int SelectedWallpaperInputMode
        {
            get { return _selectedWallpaperInputMode; }
            set
            {
                _selectedWallpaperInputMode = value;
                OnPropertyChanged();

                if (userSettings.Settings.InputForward != (InputForwardMode)_selectedWallpaperInputMode)
                {
                    userSettings.Settings.InputForward = (InputForwardMode)_selectedWallpaperInputMode;
                    UpdateSettingsConfigFile();
                }

                //todo: show msg to user desc whats happening.
                if (userSettings.Settings.InputForward == InputForwardMode.mousekeyboard)
                {
                    DesktopUtil.SetDesktopIconVisibility(false);
                }
                else
                {
                    DesktopUtil.SetDesktopIconVisibility(DesktopUtil.DesktopIconVisibilityDefault);
                }
            }
        }

        private bool _mouseMoveOnDesktop;
        public bool MouseMoveOnDesktop
        {
            get { return _mouseMoveOnDesktop; }
            set
            {
                _mouseMoveOnDesktop = value;
                OnPropertyChanged();

                if (userSettings.Settings.MouseInputMovAlways != _mouseMoveOnDesktop)
                {
                    userSettings.Settings.MouseInputMovAlways = _mouseMoveOnDesktop;
                    UpdateSettingsConfigFile();
                }
            }
        }

        private bool _isSelectedVideoPlayerAvailable;
        public bool IsSelectedVideoPlayerAvailable
        {
            get { return _isSelectedVideoPlayerAvailable; }
            set { _isSelectedVideoPlayerAvailable = value; OnPropertyChanged(); }
        }

        private int _selectedVideoPlayerIndex;
        public int SelectedVideoPlayerIndex
        {
            get
            {
                return _selectedVideoPlayerIndex;
            }
            set
            {
                _selectedVideoPlayerIndex = value;
                IsSelectedVideoPlayerAvailable = IsVideoPlayerAvailable((LivelyMediaPlayer)value);
                //_selectedVideoPlayerIndex = IsVideoPlayerAvailable((LivelyMediaPlayer)value) ? value : (int)LivelyMediaPlayer.mpv;
                OnPropertyChanged();

                if (userSettings.Settings.VideoPlayer != (LivelyMediaPlayer)_selectedVideoPlayerIndex && IsSelectedVideoPlayerAvailable)
                {
                    userSettings.Settings.VideoPlayer = (LivelyMediaPlayer)_selectedVideoPlayerIndex;
                    UpdateSettingsConfigFile();
                    //VideoPlayerSwitch((LivelyMediaPlayer)_selectedVideoPlayerIndex);
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.video, WallpaperType.picture, WallpaperType.videostream });
                }
            }
        }

        private bool _videoPlayerHWDecode;
        public bool VideoPlayerHWDecode
        {
            get { return _videoPlayerHWDecode; }
            set
            {
                _videoPlayerHWDecode = value;
                OnPropertyChanged();
                if (userSettings.Settings.VideoPlayerHwAccel != _videoPlayerHWDecode)
                {
                    userSettings.Settings.VideoPlayerHwAccel = _videoPlayerHWDecode;
                    UpdateSettingsConfigFile();
                    //if mpv player is also set as gif player..
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.video, WallpaperType.videostream, WallpaperType.gif });
                }
            }
        }

        private bool _isSelectedGifPlayerAvailable;
        public bool IsSelectedGifPlayerAvailable
        {
            get { return _isSelectedGifPlayerAvailable; }
            set { _isSelectedGifPlayerAvailable = value; OnPropertyChanged(); }
        }

        private int _selectedGifPlayerIndex;
        public int SelectedGifPlayerIndex
        {
            get
            {
                return _selectedGifPlayerIndex;
            }
            set
            {
                _selectedGifPlayerIndex = value;
                IsSelectedGifPlayerAvailable = IsGifPlayerAvailable((LivelyGifPlayer)value);
                //_selectedGifPlayerIndex = IsGifPlayerAvailable((LivelyGifPlayer)value) ? value : (int)LivelyGifPlayer.mpv;
                OnPropertyChanged();

                if (userSettings.Settings.GifPlayer != (LivelyGifPlayer)_selectedGifPlayerIndex && IsSelectedGifPlayerAvailable)
                {
                    userSettings.Settings.GifPlayer = (LivelyGifPlayer)_selectedGifPlayerIndex;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.gif, WallpaperType.picture });
                }
            }
        }

        private bool _isSelectedWebBrowserAvailable;
        public bool IsSelectedWebBrowserAvailable
        {
            get { return _isSelectedWebBrowserAvailable; }
            set { _isSelectedWebBrowserAvailable = value; OnPropertyChanged(); }
        }

        private int _selectedWebBrowserIndex;
        public int SelectedWebBrowserIndex
        {
            get
            {
                return _selectedWebBrowserIndex;
            }
            set
            {
                _selectedWebBrowserIndex = value;
                IsSelectedWebBrowserAvailable = IsWebPlayerAvailable((LivelyWebBrowser)value);
                //_selectedWebBrowserIndex = IsWebPlayerAvailable((LivelyWebBrowser)value) ? value : (int)LivelyWebBrowser.cef;
                OnPropertyChanged();

                if (userSettings.Settings.WebBrowser != (LivelyWebBrowser)_selectedWebBrowserIndex && IsSelectedWebBrowserAvailable)
                {
                    userSettings.Settings.WebBrowser = (LivelyWebBrowser)_selectedWebBrowserIndex;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.web, WallpaperType.webaudio, WallpaperType.url, WallpaperType.videostream });
                }
            }
        }

        private string _webDebuggingPort;
        public string WebDebuggingPort
        {
            get { return _webDebuggingPort; }
            set
            {
                _webDebuggingPort = value;
                if (userSettings.Settings.WebDebugPort != _webDebuggingPort)
                {
                    userSettings.Settings.WebDebugPort = _webDebuggingPort;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        private bool _cefDiskCache;
        public bool CefDiskCache
        {
            get { return _cefDiskCache; }
            set
            {
                _cefDiskCache = value;
                if (userSettings.Settings.CefDiskCache != _cefDiskCache)
                {
                    userSettings.Settings.CefDiskCache = _cefDiskCache;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
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
            get { return _selectedWallpaperStreamQualityIndex; }
            set
            {
                _selectedWallpaperStreamQualityIndex = value;
                OnPropertyChanged();
                if (userSettings.Settings.StreamQuality != (StreamQualitySuggestion)_selectedWallpaperStreamQualityIndex)
                {
                    userSettings.Settings.StreamQuality = (StreamQualitySuggestion)_selectedWallpaperStreamQualityIndex;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.videostream });
                }
            }
        }

        private bool _detectStreamWallpaper;
        public bool DetectStreamWallpaper
        {
            get { return _detectStreamWallpaper; }
            set
            {
                _detectStreamWallpaper = value;
                if (userSettings.Settings.AutoDetectOnlineStreams != _detectStreamWallpaper)
                {
                    userSettings.Settings.AutoDetectOnlineStreams = _detectStreamWallpaper;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        #endregion wallpaper

        #region audio

        private int _globalWallpaperVolume;
        public int GlobalWallpaperVolume
        {
            get { return _globalWallpaperVolume; }
            set
            {
                _globalWallpaperVolume = value;
                if (userSettings.Settings.AudioVolumeGlobal != _globalWallpaperVolume)
                {
                    userSettings.Settings.AudioVolumeGlobal = _globalWallpaperVolume;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        private bool _isAudioOnlyOnDesktop;
        public bool IsAudioOnlyOnDesktop
        {
            get
            {
                return _isAudioOnlyOnDesktop;
            }
            set
            {
                _isAudioOnlyOnDesktop = value;
                if (userSettings.Settings.AudioOnlyOnDesktop != _isAudioOnlyOnDesktop)
                {
                    userSettings.Settings.AudioOnlyOnDesktop = _isAudioOnlyOnDesktop;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        #endregion //audio

        #region system

        /*
        private bool _isLockScreenAutoWallpaper;
        public bool IsLockScreenAutoWallpaper
        {
            get
            {
                return _isLockScreenAutoWallpaper;
            }
            set
            {
                _isLockScreenAutoWallpaper = value;
                if (Settings.LockScreenAutoWallpaper != _isLockScreenAutoWallpaper)
                {
                    Settings.LockScreenAutoWallpaper = _isLockScreenAutoWallpaper;
                    UpdateConfigFile();
                }
                OnPropertyChanged();
            }
        }
        */

        private bool _isDesktopAutoWallpaper;
        public bool IsDesktopAutoWallpaper
        {
            get
            {
                return _isDesktopAutoWallpaper;
            }
            set
            {
                _isDesktopAutoWallpaper = value;
                if (userSettings.Settings.DesktopAutoWallpaper != _isDesktopAutoWallpaper)
                {
                    userSettings.Settings.DesktopAutoWallpaper = _isDesktopAutoWallpaper;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        private int _selectedTaskbarThemeIndex;
        public int SelectedTaskbarThemeIndex
        {
            get
            {
                return _selectedTaskbarThemeIndex;
            }
            set
            {
                _selectedTaskbarThemeIndex = value;
                //save the data..
                if (userSettings.Settings.SystemTaskbarTheme != (TaskbarTheme)_selectedTaskbarThemeIndex)
                {
                    userSettings.Settings.SystemTaskbarTheme = (TaskbarTheme)_selectedTaskbarThemeIndex;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
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
        //    get
        //    {
        //        return _isScreensaverLockOnResume;
        //    }
        //    set
        //    {
        //        _isScreensaverLockOnResume = value;
        //        if (userSettings.Settings.ScreensaverLockOnResume != _isScreensaverLockOnResume)
        //        {
        //            userSettings.Settings.ScreensaverLockOnResume = _isScreensaverLockOnResume;
        //            UpdateSettingsConfigFile();
        //        }
        //        OnPropertyChanged();
        //    }
        //}

        #endregion //system

        #region misc

        private bool _isSysTrayIconVisible;
        public bool IsSysTrayIconVisible
        {
            get
            {
                return _isSysTrayIconVisible;
            }
            set
            {
                _isSysTrayIconVisible = value;
                if (userSettings.Settings.SysTrayIcon != _isSysTrayIconVisible)
                {
                    //_ = commands.AutomationCommandAsync(new string[] { "--showTray", JsonUtil.Serialize(value) });
                    userSettings.Settings.SysTrayIcon = _isSysTrayIconVisible;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        /*
        public event EventHandler<bool> DebugMenuVisibilityChange;
        private bool _isDebugMenuVisible;
        public bool IsDebugMenuVisible
        {
            get { return _isDebugMenuVisible; }
            set
            {
                _isDebugMenuVisible = value;
                OnPropertyChanged();
                if (userSettings.Settings.DebugMenu != _isDebugMenuVisible)
                {
                    DebugMenuVisibilityChange?.Invoke(null, _isDebugMenuVisible);
                    userSettings.Settings.DebugMenu = _isDebugMenuVisible;
                    UpdateConfigFile();
                }
            }
        }
        */

        private bool _isKeepUIAwake;
        public bool IsKeepUIAwake
        {
            get
            {
                return _isKeepUIAwake;
            }
            set
            {
                _isKeepUIAwake = value;
                OnPropertyChanged();
                if (userSettings.Settings.KeepAwakeUI != _isKeepUIAwake)
                {
                    userSettings.Settings.KeepAwakeUI = _isKeepUIAwake;
                    UpdateSettingsConfigFile();
                }
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
                    await dialogService.ShowDialog(ex.Message, "Error", "OK");
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
            if (originalWallpapers.Count() > 0)
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
                        FileOperations.DirectoryCopy(Path.Combine(WallpaperDirectory, Constants.CommonPartialPaths.WallpaperInstallDir),
                            Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallDir), true);
                        FileOperations.DirectoryCopy(Path.Combine(WallpaperDirectory, Constants.CommonPartialPaths.WallpaperInstallTempDir),
                            Path.Combine(newDir, Constants.CommonPartialPaths.WallpaperInstallTempDir), true);
                        FileOperations.DirectoryCopy(Path.Combine(WallpaperDirectory, Constants.CommonPartialPaths.WallpaperSettingsDir),
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
                var result1 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, Constants.CommonPartialPaths.WallpaperInstallDir), 1000, 3000);
                var result2 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, Constants.CommonPartialPaths.WallpaperInstallTempDir), 0, 1000);
                var result3 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, Constants.CommonPartialPaths.WallpaperSettingsDir), 0, 1000);
                if (!(result1 && result2 && result3))
                {
                    //TODO: Dialogue
                }
            }
        }

        #endregion //helper fns
    }
}
