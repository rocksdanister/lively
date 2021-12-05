using livelywpf.Core;
using livelywpf.Helpers;
using livelywpf.Helpers.Files;
using livelywpf.Helpers.MVVM;
using livelywpf.Helpers.Shell;
using livelywpf.Helpers.Startup;
using livelywpf.Helpers.Storage;
using livelywpf.Views;
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
using System.Windows.Forms;
using livelywpf.Models;
using livelywpf.Views.Dialogues;
using livelywpf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace livelywpf.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IScreensaverService screenSaver;
        private readonly IAppUpdaterService appUpdater;
        private readonly ITransparentTbService ttbService;

        public SettingsViewModel(IUserSettingsService 
            userSettings, 
            IDesktopCore desktopCore, 
            IScreensaverService screenSaver, 
            IAppUpdaterService appUpdater, 
            ITransparentTbService ttbService)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.screenSaver = screenSaver;
            this.appUpdater = appUpdater;
            this.ttbService = ttbService;

            //lang-codes: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
            LanguageItems = new ObservableCollection<LanguagesModel>()
            {
                new LanguagesModel("English(en)", new string[]{"en", "en-US"}),
                new LanguagesModel("日本語(ja)", new string[]{"ja", "ja-JP"}),
                new LanguagesModel("中文(zh-CN)", new string[]{"zh", "zh-Hans","zh-CN","zh-SG"}),
                new LanguagesModel("繁體中文(zh-Hant)", new string[]{ "zh-HK", "zh-MO", "zh-TW"}),
                new LanguagesModel("한국어(ko-KR)", new string[]{"ko", "ko-KR","ko-KP"}),
                new LanguagesModel("Pусский(ru)", new string[]{"ru", "ru-BY", "ru-KZ", "ru-KG", "ru-MD", "ru-RU","ru-UA"}),
                new LanguagesModel("Українська(uk)", new string[]{"uk", "uk-UA"}),
                new LanguagesModel("Español(es)", new string[]{"es"}),
                new LanguagesModel("Español(es-MX)", new string[]{"es-MX"}),
                new LanguagesModel("Italian(it)", new string[]{"it", "it-IT", "it-SM","it-CH","it-VA"}),
                new LanguagesModel("عربى(ar-AE)", new string[]{"ar"}),
                new LanguagesModel("فارسی(fa-IR)", new string[]{"fa-IR"}),
                new LanguagesModel("עִברִית(he-IL)", new string[]{"he", "he-IL"}),
                new LanguagesModel("Française(fr)", new string[]{"fr"}),
                new LanguagesModel("Deutsch(de)", new string[]{"de"}),
                new LanguagesModel("język polski(pl)", new string[]{"pl", "pl-PL"}),
                new LanguagesModel("Português(pt)", new string[]{"pt"}),
                new LanguagesModel("Português(pt-BR)", new string[]{"pt-BR"}),
                new LanguagesModel("Filipino(fil)", new string[]{"fil", "fil-PH"}),
                new LanguagesModel("Bahasa Indonesia(id)", new string[]{"id", "id-ID"}),
                new LanguagesModel("Magyar(hu)", new string[]{"hu", "hu-HU"}),
                new LanguagesModel("Svenska(sv)", new string[]{"sv","sv-AX", "sv-FI", "sv-SE"}),
                new LanguagesModel("Bahasa Melayu(ms)", new string[]{"ms", "ms-BN", "ms-MY"}),
                new LanguagesModel("Nederlands(nl-NL)", new string[]{"nl-NL"}),
                new LanguagesModel("Tiếng Việt(vi)", new string[]{"vi", "vi-VN"}),
                new LanguagesModel("Català(ca)", new string[]{"ca", "ca-AD", "ca-FR", "ca-IT", "ca-ES"}),
                new LanguagesModel("Türkçe(tr)", new string[]{"tr", "tr-CY", "tr-TR"}),
                new LanguagesModel("Cрпски језик(sr)", new string[]{"sr", "sr-Latn", "sr-Latn-BA", "sr-Latn-ME", "sr-Latn-RS", "sr-Latn-CS"}),
                new LanguagesModel("Српска ћирилица(sr-Cyrl)", new string[]{"sr-Cyrl", "sr-Cyrl-BA", "sr-Cyrl-ME", "sr-Cyrl-RS", "sr-Cyrl-CS"}),
                new LanguagesModel("Ελληνικά(el)", new string[]{"el", "el-GR", "el-CY"}),
                new LanguagesModel("हिन्दी(hi)", new string[]{"hi", "hi-IN"}),
                new LanguagesModel("Azerbaijani(az)", new string[]{"az", "az-Cyrl", "az-Cyrl-AZ"})
            };

            var defaultLanguage = SearchSupportedLanguage(userSettings.Settings.Language);
            if (defaultLanguage == null)
            {
                defaultLanguage = LanguageItems[0];
                userSettings.Settings.Language = defaultLanguage.Codes[0]; //en
                UpdateConfigFile();
            }
            try
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(userSettings.Settings.Language);
            }
            catch (Exception e)
            {
                Logger.Error("Setting locale fail:" + e.Message);
            }
            SelectedLanguageItem = defaultLanguage;

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
            SelectedTaskbarThemeIndex = (int)userSettings.Settings.SystemTaskbarTheme;
            IsDesktopAutoWallpaper = userSettings.Settings.DesktopAutoWallpaper;
            IsDebugMenuVisible = userSettings.Settings.DebugMenu;
            SelectedWebBrowserIndex = (int)userSettings.Settings.WebBrowser;
            SelectedAppThemeIndex = (int)userSettings.Settings.ApplicationTheme;
            SelectedScreensaverWaitIndex = (int)userSettings.Settings.ScreensaverIdleWait;
            IsScreensaverLockOnResume = userSettings.Settings.ScreensaverLockOnResume;
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
                OnPropertyChanged();
                if (Constants.ApplicationType.IsMSIX)
                {
                    _ = WindowsStartup.StartupWin10(_isStartup);
                    if (userSettings.Settings.Startup != _isStartup)
                    {
                        userSettings.Settings.Startup = _isStartup;
                        UpdateConfigFile();
                    }
                }
                else
                {
                    WindowsStartup.SetStartupRegistry(_isStartup);
                }
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
                    //Settings.IsRestart = true;
                    userSettings.Settings.Language = _selectedLanguageItem.Codes[0];
                    UpdateConfigFile();
                    //Program.RestartApplication();
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

        private RelayCommand _wallpaperDirectoryChangeCommand;
        public RelayCommand WallpaperDirectoryChangeCommand
        {
            get
            {
                if (_wallpaperDirectoryChangeCommand == null)
                {
                    _wallpaperDirectoryChangeCommand = new RelayCommand(
                        param => WallpaperDirectoryChange(), param => !Constants.ApplicationType.IsMSIX && !WallpapeDirectoryChanging
                        );
                }
                return _wallpaperDirectoryChangeCommand;
            }
        }

        private bool _wallpapeDirectoryChanging;
        public bool WallpapeDirectoryChanging
        {
            get { return _wallpapeDirectoryChanging; }
            set
            {
                _wallpapeDirectoryChanging = value;
                OnPropertyChanged();
            }
        }

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
        public RelayCommand OpenWallpaperDirectory
        {
            get
            {
                if (_openWallpaperDirectory == null)
                {
                    _openWallpaperDirectory = new RelayCommand(
                            param => FileOperations.OpenFolder(userSettings.Settings.WallpaperDir)
                        );
                }
                return _openWallpaperDirectory;
            }
        }

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

                    Program.ApplicationThemeChange(userSettings.Settings.ApplicationTheme);
                }
            }
        }

        #endregion general

        #region performance

        private RelayCommand _applicationRulesCommand;
        public RelayCommand ApplicationRulesCommand
        {
            get
            {
                if (_applicationRulesCommand == null)
                {
                    _applicationRulesCommand = new RelayCommand(
                            param => ShowApplicationRulesWindow()
                        );
                }
                return _applicationRulesCommand;
            }
        }

        private void ShowApplicationRulesWindow()
        {
            (new ApplicationRulesView()
            {
                Owner = App.Services.GetRequiredService<MainWindow>(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            }).ShowDialog();
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

                    WallpaperRestart(WallpaperType.videostream);
                    WallpaperRestart(WallpaperType.video);
                    WallpaperRestart(WallpaperType.gif);
                    WallpaperRestart(WallpaperType.picture);
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
                _selectedVideoPlayerIndex = CheckVideoPluginExists((LivelyMediaPlayer)value) ? value : (int)LivelyMediaPlayer.mpv;
                OnPropertyChanged();

                if (userSettings.Settings.VideoPlayer != (LivelyMediaPlayer)_selectedVideoPlayerIndex)
                {
                    userSettings.Settings.VideoPlayer = (LivelyMediaPlayer)_selectedVideoPlayerIndex;
                    UpdateConfigFile();
                    //VideoPlayerSwitch((LivelyMediaPlayer)_selectedVideoPlayerIndex);
                    WallpaperRestart(WallpaperType.video);
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
                    WallpaperRestart(WallpaperType.video);
                    WallpaperRestart(WallpaperType.videostream);
                    //if mpv player is also set as gif player..
                    WallpaperRestart(WallpaperType.gif);
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
                _selectedGifPlayerIndex = CheckGifPluginExists((LivelyGifPlayer)value) ? value : (int)LivelyGifPlayer.win10Img;
                OnPropertyChanged();
                if (userSettings.Settings.GifPlayer != (LivelyGifPlayer)_selectedGifPlayerIndex)
                {
                    userSettings.Settings.GifPlayer = (LivelyGifPlayer)_selectedGifPlayerIndex;
                    UpdateConfigFile();
                    WallpaperRestart(WallpaperType.gif);
                    WallpaperRestart(WallpaperType.picture);
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

                    WallpaperRestart(WallpaperType.videostream);
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

                    WallpaperRestart(WallpaperType.web);
                    WallpaperRestart(WallpaperType.webaudio);
                    WallpaperRestart(WallpaperType.url);
                    WallpaperRestart(WallpaperType.videostream);
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

        #endregion //system

        #region misc

        public event EventHandler<bool> TrayIconVisibilityChange;
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
                OnPropertyChanged();
                TrayIconVisibilityChange?.Invoke(null, _isSysTrayIconVisible);
                if (userSettings.Settings.SysTrayIcon != _isSysTrayIconVisible)
                {
                    userSettings.Settings.SysTrayIcon = _isSysTrayIconVisible;
                    UpdateConfigFile();
                }
            }
        }

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

        private RelayCommand _extractLogCommand;
        public RelayCommand ExtractLogCommand
        {
            get
            {
                if (_extractLogCommand == null)
                {
                    _extractLogCommand = new RelayCommand(
                            param => LogUtil.ExtractLogFiles()
                        );
                }
                return _extractLogCommand;
            }
        }

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

        #endregion //misc

        #region helper fns

        private static bool CheckVideoPluginExists(LivelyMediaPlayer mp)
        {
            return mp switch
            {
                LivelyMediaPlayer.wmf => true,
                LivelyMediaPlayer.libvlc => false, //depreciated
                LivelyMediaPlayer.libmpv => false, //depreciated
                LivelyMediaPlayer.libvlcExt => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer", "libVLCPlayer.exe")),
                LivelyMediaPlayer.libmpvExt => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyMediaPlayer.mpv => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "mpv.exe")), 
                LivelyMediaPlayer.vlc => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vlc", "vlc.exe")),
                _ => false,
            };
        }

        private static bool CheckGifPluginExists(LivelyGifPlayer gp)
        {
            return gp switch
            {
                LivelyGifPlayer.win10Img => false, //xaml island
                LivelyGifPlayer.libmpvExt => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyGifPlayer.mpv => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "mpv.exe")),
                _ => false,
            };
        }

        /// <summary>
        /// Checks LanguageItems and see if language with the given code exists.
        /// </summary>
        /// <param name="langCode">language code</param>
        /// <returns>Languagemodel if found; null otherwise.</returns>
        private LanguagesModel SearchSupportedLanguage(string langCode)
        {
            //return LanguageItems.FirstOrDefault(lang => lang.Codes.Contains(langCode));
            foreach (var lang in LanguageItems)
            {
                foreach (var code in lang.Codes)
                {
                    if (string.Equals(code, langCode, StringComparison.OrdinalIgnoreCase))
                    {
                        return lang;
                    }
                }
            }
            return null;
        }

        private void WallpaperRestart(WallpaperType type)
        {
            Logger.Info("Restarting wallpaper:" + type);
            var originalWallpapers = desktopCore.Wallpapers.Where(x => x.Category == type).ToList();
            if (originalWallpapers.Count > 0)
            {
                desktopCore.CloseWallpaper(type, true);
                foreach (var item in originalWallpapers)
                {
                    desktopCore.SetWallpaper(item.Model, item.Screen);
                    if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span
                        || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                    {
                        break;
                    }
                }
            }
        }

        public event EventHandler<string> LivelyWallpaperDirChange;
        private async void WallpaperDirectoryChange()
        {
            bool isDestEmptyDir = false;
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = Program.WallpaperDir
            };
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if(string.Equals(folderBrowserDialog.SelectedPath, userSettings.Settings.WallpaperDir, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                try
                {
                    var parentDir = Directory.GetParent(folderBrowserDialog.SelectedPath).ToString();
                    if (parentDir != null)
                    {
                        if (Directory.Exists(Path.Combine(parentDir, "wallpapers")) &&
                            Directory.Exists(Path.Combine(parentDir, "SaveData","wpdata")))
                        {
                            //User selected wrong directory, lively needs the SaveData folder also(root).
                            folderBrowserDialog.SelectedPath = parentDir;
                        }
                    }

                    WallpapeDirectoryChanging = true;
                    WallpaperDirectoryChangeCommand.RaiseCanExecuteChanged();
                    //create destination directory's if not exist.
                    Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "wallpapers"));
                    Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wptmp"));
                    Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wpdata"));

                    if (userSettings.Settings.WallpaperDirMoveExistingWallpaperNewDir)
                    {
                        await Task.Run(() =>
                        {
                            FileOperations.DirectoryCopy(Path.Combine(Program.WallpaperDir, "wallpapers"),
                                Path.Combine(folderBrowserDialog.SelectedPath, "wallpapers"), true);
                            FileOperations.DirectoryCopy(Path.Combine(Program.WallpaperDir, "SaveData", "wptmp"),
                                Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wptmp"), true);
                            FileOperations.DirectoryCopy(Path.Combine(Program.WallpaperDir, "SaveData", "wpdata"),
                                Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wpdata"), true);
                        });
                    }
                    else
                    {
                        isDestEmptyDir = true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Lively Folder Change Fail: " + e.Message);
                    System.Windows.MessageBox.Show("Failed to write to new directory:\n" + e.Message, Properties.Resources.TextError);
                    return;
                }
                finally
                {
                    WallpapeDirectoryChanging = false;
                    WallpaperDirectoryChangeCommand.RaiseCanExecuteChanged();
                }

                //exit all running wp's immediately
                desktopCore.CloseAllWallpapers(true);

                var previousDirectory = userSettings.Settings.WallpaperDir;
                userSettings.Settings.WallpaperDir = folderBrowserDialog.SelectedPath;
                UpdateConfigFile();
                WallpaperDirectory = userSettings.Settings.WallpaperDir;
                Program.WallpaperDir = userSettings.Settings.WallpaperDir;
                LivelyWallpaperDirChange?.Invoke(null, folderBrowserDialog.SelectedPath);

                if (!isDestEmptyDir)
                {
                    //not deleting the root folder, what if the user selects a folder that is not used by Lively alone!
                    var result1 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, "wallpapers"), 1000, 3000);
                    var result2 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, "SaveData"), 0, 1000);
                    if (!(result1 && result2))
                    {
                        System.Windows.MessageBox.Show("Failed to delete old wallpaper directory!\nTry deleting it manually.", Properties.Resources.TextError);
                    }
                }
                folderBrowserDialog.Dispose();
            }
        }

        #endregion //helper fns
    }
}
