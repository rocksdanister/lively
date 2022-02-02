using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.MVVM;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.Storage.Pickers;

namespace Lively.UI.WinUI.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        //private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly ICommandsClient commands;
        //private readonly IScreensaverService screenSaver;
        //private readonly IAppUpdaterService appUpdater;
        //private readonly ITransparentTbService ttbService;

        public SettingsViewModel(
            IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            ICommandsClient commands)
            //IScreensaverService screenSaver, 
            //IAppUpdaterService appUpdater, 
            //ITransparentTbService ttbService)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.commands = commands;
            //this.screenSaver = screenSaver;
            //this.appUpdater = appUpdater;
            //this.ttbService = ttbService;

            //lang-codes: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
            LanguageItems = new ObservableCollection<LanguagesModel>(LocalizationUtil.SupportedLanguages);

            /*
            if (Constants.ApplicationType.IsMSIX)
            {
                _ = WindowsStartup.StartupWin10(userSettings.Settings.Startup);
                IsStartup = userSettings.Settings.Startup;
            }
            else
            {
                //Ignoring the Settings.json savefile value, only checking the windows registry and user action on the ui.
                IsStartup = WindowsStartup.CheckStartupRegistry() == 1 || WindowsStartup.CheckStartupRegistry() == -1;
            }

            //Restrictions..
            userSettings.Settings.LockScreenAutoWallpaper = false;

            userSettings.Settings.SelectedDisplay = ScreenHelper.GetScreen(userSettings.Settings.SelectedDisplay.DeviceId, userSettings.Settings.SelectedDisplay.DeviceName,
                        userSettings.Settings.SelectedDisplay.Bounds, userSettings.Settings.SelectedDisplay.WorkingArea, DisplayIdentificationMode.deviceId) ?? ScreenHelper.GetPrimaryScreen();

            //Restrictions on wpf only version of Lively.
            if (userSettings.Settings.LivelyGUIRendering == LivelyGUIState.normal)
            {
                userSettings.Settings.LivelyGUIRendering = LivelyGUIState.lite;
            }
            */

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
            SelectedLivelyUIModeIndex = (int)userSettings.Settings.LivelyGUIRendering;
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
            //SelectedTaskbarThemeIndex = (int)userSettings.Settings.SystemTaskbarTheme;
            IsDesktopAutoWallpaper = userSettings.Settings.DesktopAutoWallpaper;
            //IsDebugMenuVisible = userSettings.Settings.DebugMenu;
            SelectedWebBrowserIndex = (int)userSettings.Settings.WebBrowser;
            SelectedAppThemeIndex = (int)userSettings.Settings.ApplicationTheme;
            //SelectedScreensaverWaitIndex = (int)userSettings.Settings.ScreensaverIdleWait;
            //IsScreensaverLockOnResume = userSettings.Settings.ScreensaverLockOnResume;
            IsKeepUIAwake = userSettings.Settings.KeepAwakeUI;
            IsStartup = userSettings.Settings.Startup;
            SelectedLanguageItem = LocalizationUtil.GetSupportedLanguage(userSettings.Settings.Language);
        }

        public void UpdateConfigFile()
        {
            userSettings.Save<ISettingsModel>();
        }

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
                    _ = commands.AutomationCommandAsync(new string[] { "--startup", JsonUtil.Serialize(value) });
                    userSettings.Settings.Startup = _isStartup;
                    UpdateConfigFile();
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
                    UpdateConfigFile();
                    _ = commands.RestartUI();
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
                    UpdateConfigFile();
                }
            }
        }

        public event EventHandler<LivelyGUIState> LivelyGUIStateChanged;
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

                //prevent running on startup etc.
                if (userSettings.Settings.LivelyGUIRendering != (LivelyGUIState)value)
                {
                    userSettings.Settings.LivelyGUIRendering = (LivelyGUIState)value;
                    UpdateConfigFile();

                    LivelyGUIStateChanged?.Invoke(null, (LivelyGUIState)value);
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
            ??= new RelayCommand(WallpaperDirectoryChange, () => !Constants.ApplicationType.IsMSIX && !WallpaperDirectoryChangeOngoing);

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
                    UpdateConfigFile();
                }
            }
        }

        private RelayCommand _openWallpaperDirectory;
        public RelayCommand OpenWallpaperDirectory => 
            _openWallpaperDirectory ??= new RelayCommand(() => FileOperations.OpenFolder(userSettings.Settings.WallpaperDir));

        public event EventHandler<AppTheme> AppThemeChanged;
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
                    UpdateConfigFile();

                    AppThemeChanged?.Invoke(this, userSettings.Settings.ApplicationTheme);
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
                }
            }
        }

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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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

        private int _selectedVideoPlayerIndex;
        public int SelectedVideoPlayerIndex
        {
            get
            {
                return _selectedVideoPlayerIndex;
            }
            set
            {
                _selectedVideoPlayerIndex = IsVideoPlayerAvailable((LivelyMediaPlayer)value) ? value : (int)LivelyMediaPlayer.mpv;
                OnPropertyChanged();

                if (userSettings.Settings.VideoPlayer != (LivelyMediaPlayer)_selectedVideoPlayerIndex)
                {
                    userSettings.Settings.VideoPlayer = (LivelyMediaPlayer)_selectedVideoPlayerIndex;
                    UpdateConfigFile();
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
                    UpdateConfigFile();
                    //if mpv player is also set as gif player..
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.video, WallpaperType.videostream, WallpaperType.gif });
                }
            }
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
                _selectedGifPlayerIndex = IsGifPlayerAvailable((LivelyGifPlayer)value) ? value : (int)LivelyGifPlayer.win10Img;
                OnPropertyChanged();
                if (userSettings.Settings.GifPlayer != (LivelyGifPlayer)_selectedGifPlayerIndex)
                {
                    userSettings.Settings.GifPlayer = (LivelyGifPlayer)_selectedGifPlayerIndex;
                    UpdateConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.gif, WallpaperType.picture });
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
                    UpdateConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.videostream });
                }
            }
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
                OnPropertyChanged();

                if (userSettings.Settings.WebBrowser != (LivelyWebBrowser)_selectedWebBrowserIndex)
                {
                    userSettings.Settings.WebBrowser = (LivelyWebBrowser)_selectedWebBrowserIndex;
                    UpdateConfigFile();
                    _ = WallpaperRestart(new WallpaperType[] { WallpaperType.web, WallpaperType.webaudio, WallpaperType.url, WallpaperType.videostream });
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
                }
                OnPropertyChanged();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                    UpdateConfigFile();
                }
                OnPropertyChanged();
            }
        }

        /*
        private bool ttbInitialized = false;
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
                if (!ttbInitialized)
                {
                    if ((TaskbarTheme)_selectedTaskbarThemeIndex != TaskbarTheme.none)
                    {
                        string pgm = null;
                        if ((pgm = ttbService.CheckIncompatiblePrograms()) == null)
                        {
                            ttbService.Start((TaskbarTheme)_selectedTaskbarThemeIndex);
                            ttbInitialized = true;
                        }
                        else
                        {
                            _selectedTaskbarThemeIndex = (int)TaskbarTheme.none;
                            _ = Task.Run(() =>
                                    System.Windows.MessageBox.Show(Properties.Resources.DescIncompatibleTaskbarTheme + "\n\n" + pgm,
                                        Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information));
                        }
                    }
                }
                else
                {
                    ttbService.Start((TaskbarTheme)_selectedTaskbarThemeIndex);
                }
                //save the data..
                if (userSettings.Settings.SystemTaskbarTheme != (TaskbarTheme)_selectedTaskbarThemeIndex)
                {
                    userSettings.Settings.SystemTaskbarTheme = (TaskbarTheme)_selectedTaskbarThemeIndex;
                    UpdateConfigFile();
                }
                OnPropertyChanged();
            }
        }

        private int _selectedScreensaverWaitIndex;
        public int SelectedScreensaverWaitIndex
        {
            get
            {
                return _selectedScreensaverWaitIndex;
            }
            set
            {
                _selectedScreensaverWaitIndex = value;
                uint idleTime = (ScreensaverIdleTime)_selectedScreensaverWaitIndex switch
                {
                    ScreensaverIdleTime.none => 0,
                    ScreensaverIdleTime.min1 => 60000,
                    ScreensaverIdleTime.min2 => 120000,
                    ScreensaverIdleTime.min3 => 180000,
                    ScreensaverIdleTime.min5 => 300000,
                    ScreensaverIdleTime.min10 => 600000,
                    ScreensaverIdleTime.min15 => 900000,
                    ScreensaverIdleTime.min20 => 1200000,
                    ScreensaverIdleTime.min25 => 1500000,
                    ScreensaverIdleTime.min30 => 1800000,
                    ScreensaverIdleTime.min45 => 2700000,
                    ScreensaverIdleTime.min60 => 3600000,
                    ScreensaverIdleTime.min120 => 7200000,
                    _ => 300000,
                };
                if (idleTime != 0)
                {
                    screenSaver.StartIdleTimer(idleTime);
                }
                else
                {
                    screenSaver.StopIdleTimer();
                }
                //save the data..
                if (userSettings.Settings.ScreensaverIdleWait != (ScreensaverIdleTime)_selectedScreensaverWaitIndex)
                {
                    if (!userSettings.Settings.ScreensaverOledWarning)
                    {
                        _ = Task.Run(() =>
                               System.Windows.MessageBox.Show(Properties.Resources.DescOledScreensaverNotice,
                                   Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information));
                        userSettings.Settings.ScreensaverOledWarning = true;
                    }
                    userSettings.Settings.ScreensaverIdleWait = (ScreensaverIdleTime)_selectedScreensaverWaitIndex;
                    UpdateConfigFile();
                }
                OnPropertyChanged();
            }
        }

        private bool _isScreensaverLockOnResume;
        public bool IsScreensaverLockOnResume
        {
            get
            {
                return _isScreensaverLockOnResume;
            }
            set
            {
                _isScreensaverLockOnResume = value;
                if (userSettings.Settings.ScreensaverLockOnResume != _isScreensaverLockOnResume)
                {
                    userSettings.Settings.ScreensaverLockOnResume = _isScreensaverLockOnResume;
                    UpdateConfigFile();
                }
                OnPropertyChanged();
            }
        }
        */

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
                    _ = commands.AutomationCommandAsync(new string[] { "--showTray", JsonUtil.Serialize(value) });
                    userSettings.Settings.SysTrayIcon = _isSysTrayIconVisible;
                    UpdateConfigFile();
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
                    UpdateConfigFile();
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
                LogUtil.ExtractLogFiles(file.Path);
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

        private static bool IsVideoPlayerAvailable(LivelyMediaPlayer mp)
        {
            return mp switch
            {
                LivelyMediaPlayer.libvlc => false, //depreciated
                LivelyMediaPlayer.libmpv => false, //depreciated
                LivelyMediaPlayer.wmf => File.Exists(Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\")), "plugins", "wmf", "Lively.PlayerWmf.exe")),
                LivelyMediaPlayer.libvlcExt => File.Exists(Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\")), "plugins", "libVLCPlayer", "libVLCPlayer.exe")),
                LivelyMediaPlayer.libmpvExt => File.Exists(Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\")), "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyMediaPlayer.mpv => File.Exists(Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\")), "plugins", "mpv", "mpv.exe")), 
                LivelyMediaPlayer.vlc => File.Exists(Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\")), "plugins", "vlc", "vlc.exe")),
                _ => false,
            };
        }

        private static bool IsGifPlayerAvailable(LivelyGifPlayer gp)
        {
            return gp switch
            {
                LivelyGifPlayer.win10Img => false, //xaml island
                LivelyGifPlayer.libmpvExt => File.Exists(Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\")), "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyGifPlayer.mpv => File.Exists(Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\")), "plugins", "mpv", "mpv.exe")),
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

        public event EventHandler<string> WallpaperDirChanged;

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
                    if (Directory.Exists(Path.Combine(parentDir, "wallpapers")) &&
                        Directory.Exists(Path.Combine(parentDir, "SaveData", "wpdata")))
                    {
                        //User selected wrong directory, lively needs the SaveData folder also(root).
                        newDir = parentDir;
                    }
                }

                WallpaperDirectoryChangeOngoing = true;
                WallpaperDirectoryChangeCommand.NotifyCanExecuteChanged();
                //create destination directory's if not exist.
                Directory.CreateDirectory(Path.Combine(newDir, "wallpapers"));
                Directory.CreateDirectory(Path.Combine(newDir, "SaveData", "wptmp"));
                Directory.CreateDirectory(Path.Combine(newDir, "SaveData", "wpdata"));

                if (userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir)
                {
                    await Task.Run(() =>
                    {
                        FileOperations.DirectoryCopy(Path.Combine(WallpaperDirectory, "wallpapers"),
                            Path.Combine(newDir, "wallpapers"), true);
                        FileOperations.DirectoryCopy(Path.Combine(WallpaperDirectory, "SaveData", "wptmp"),
                            Path.Combine(newDir, "SaveData", "wptmp"), true);
                        FileOperations.DirectoryCopy(Path.Combine(WallpaperDirectory, "SaveData", "wpdata"),
                            Path.Combine(newDir, "SaveData", "wpdata"), true);
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
            UpdateConfigFile();
            WallpaperDirectory = userSettings.Settings.WallpaperDir;
            WallpaperDirChanged?.Invoke(this, newDir);

            if (!isDestEmptyDir)
            {
                //not deleting the root folder, what if the user selects a folder that is not used by Lively alone!
                var result1 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, "wallpapers"), 1000, 3000);
                var result2 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, "SaveData"), 0, 1000);
                if (!(result1 && result2))
                {
                    //TODO: Dialogue
                }
            }
        }

        #endregion //helper fns
    }
}
