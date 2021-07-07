using livelywpf.Core;
using livelywpf.Views;
using Octokit;
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
using System.Windows.Input;

namespace livelywpf
{
    public class SettingsViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public SettingsViewModel()
        {
            try
            {
                Settings = Helpers.JsonStorage<SettingsModel>.LoadData(Path.Combine(Program.AppDataDir, "Settings.json"));
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                Settings = new SettingsModel();
                UpdateConfigFile();
            }

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
            };

            var defaultLanguage = SearchSupportedLanguage(Settings.Language);
            if (defaultLanguage == null)
            {
                defaultLanguage = LanguageItems[0];
                Settings.Language = defaultLanguage.Codes[0]; //en
                UpdateConfigFile();
            }
            try
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Settings.Language);
            }
            catch (Exception e)
            {
                Logger.Error("Setting locale fail:" + e.Message);
            }
            SelectedLanguageItem = defaultLanguage;

            if (Program.IsMSIX)
            {
                _= WindowsStartup.StartupWin10(Settings.Startup);
                IsStartup = Settings.Startup;
            }
            else
            {
                //Ignoring the Settings.json savefile value, only checking the windows registry and user action on the ui.
                IsStartup = WindowsStartup.CheckStartupRegistry() == 1 || WindowsStartup.CheckStartupRegistry() == -1;
            }

            Settings.SelectedDisplay = ScreenHelper.GetScreen(Settings.SelectedDisplay.DeviceId, Settings.SelectedDisplay.DeviceName,
                        Settings.SelectedDisplay.Bounds, Settings.SelectedDisplay.WorkingArea, DisplayIdentificationMode.deviceId) ?? ScreenHelper.GetPrimaryScreen();

            SelectedTileSizeIndex = Settings.TileSize;
            SelectedAppFullScreenIndex = (int)Settings.AppFullscreenPause;
            SelectedAppFocusIndex = (int)Settings.AppFocusPause;
            SelectedBatteryPowerIndex = (int)Settings.BatteryPause;
            SelectedDisplayPauseRuleIndex = (int)Settings.DisplayPauseSettings;
            SelectedPauseAlgorithmIndex = (int)Settings.ProcessMonitorAlgorithm;
            SelectedVideoPlayerIndex = (int)Settings.VideoPlayer;
            VideoPlayerHWDecode = Settings.VideoPlayerHwAccel;
            SelectedGifPlayerIndex = (int)Settings.GifPlayer;
            SelectedWallpaperStreamQualityIndex = (int)Settings.StreamQuality;
            SelectedLivelyUIModeIndex = (int)Settings.LivelyGUIRendering;
            SelectedWallpaperInputMode = (int)Settings.InputForward;
            MouseMoveOnDesktop = Settings.MouseInputMovAlways;
            IsSysTrayIconVisible = Settings.SysTrayIcon;
            WebDebuggingPort = Settings.WebDebugPort;
            DetectStreamWallpaper = Settings.AutoDetectOnlineStreams;
            WallpaperDirectory = Settings.WallpaperDir;
            MoveExistingWallpaperNewDir = Settings.WallpaperDirMoveExistingWallpaperNewDir;
            GlobalWallpaperVolume = Settings.AudioVolumeGlobal;
            IsAudioOnlyOnDesktop = Settings.AudioOnlyOnDesktop;
            SelectedWallpaperScalingIndex = (int)Settings.WallpaperScaling;
            CefDiskCache = Settings.CefDiskCache;
            IsLockScreenAutoWallpaper = Settings.LockScreenAutoWallpaper;
            SelectedTaskbarThemeIndex = (int)Settings.SystemTaskbarTheme;
            IsDesktopAutoWallpaper = Settings.DesktopAutoWallpaper;
            IsDebugMenuVisible = Settings.DebugMenu;
            SelectedWebBrowserIndex = (int)Settings.WebBrowser;
            SelectedAppThemeIndex = (int)Settings.ApplicationTheme;
            SelectedScreensaverWaitIndex = (int)Settings.ScreensaverIdleWait;
            IsScreensaverLockOnResume = Settings.ScreensaverLockOnResume;
        }

        private SettingsModel _settings;
        public SettingsModel Settings
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
                OnPropertyChanged("Settings");
            }
        }

        public void UpdateConfigFile()
        {
            try
            {
                Helpers.JsonStorage<SettingsModel>.StoreData(Path.Combine(Program.AppDataDir, "Settings.json"), Settings);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
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
                OnPropertyChanged("IsStartup");
                if (Program.IsMSIX)
                {
                    _= WindowsStartup.StartupWin10(_isStartup);
                    if (Settings.Startup != _isStartup)
                    {
                        Settings.Startup = _isStartup;
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
                OnPropertyChanged("LanguageItems");
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
                OnPropertyChanged("SelectedLanguageItem");
                if (_selectedLanguageItem.Codes.FirstOrDefault(x => x == Settings.Language) == null)
                {
                    //Settings.IsRestart = true;
                    Settings.Language = _selectedLanguageItem.Codes[0];
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
                OnPropertyChanged("SelectedTileSizeIndex");

                if(Settings.TileSize != _selectedTileSizeIndex)
                {
                    Settings.TileSize = _selectedTileSizeIndex;
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
                OnPropertyChanged("SelectedLivelyUIModeIndex");

                //prevent running on startup etc.
                if (Settings.LivelyGUIRendering != (LivelyGUIState)value)
                {
                    Settings.LivelyGUIRendering = (LivelyGUIState)value;
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
                OnPropertyChanged("WallpaperDirectory");
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
                        param => WallpaperDirectoryChange(), param => !Program.IsMSIX && !WallpapeDirectoryChanging
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
                OnPropertyChanged("WallpapeDirectoryChanging");
            }
        }

        private bool _moveExistingWallpaperNewDir;
        public bool MoveExistingWallpaperNewDir
        {
            get { return _moveExistingWallpaperNewDir; }
            set
            {
                _moveExistingWallpaperNewDir = value;
                OnPropertyChanged("MoveExistingWallpaperNewDir");

                if (Settings.WallpaperDirMoveExistingWallpaperNewDir != _moveExistingWallpaperNewDir)
                {
                    Settings.WallpaperDirMoveExistingWallpaperNewDir = _moveExistingWallpaperNewDir;
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
                            param => FileOperations.OpenFolder(Settings.WallpaperDir)
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
                OnPropertyChanged("SelectedAppThemeIndex");

                //prevent running on startup etc.
                if (Settings.ApplicationTheme != (AppTheme)value)
                {
                    Settings.ApplicationTheme = (AppTheme)value;
                    UpdateConfigFile();

                    //Program.ApplicationThemeChange(Settings.ApplicationTheme);
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
            ApplicationRulesView app = new ApplicationRulesView();
            if (App.AppWindow.IsVisible)
            {
                app.Owner = App.AppWindow;
                app.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            app.ShowDialog();
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
                OnPropertyChanged("SelectedAppFullScreenIndex");

                if(Settings.AppFullscreenPause != (AppRulesEnum)_selectedAppFullScreenIndex)
                {
                    Settings.AppFullscreenPause = (AppRulesEnum)_selectedAppFullScreenIndex;
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
                OnPropertyChanged("SelectedAppFocusIndex");

                if(Settings.AppFocusPause != (AppRulesEnum)_selectedAppFocusIndex)
                {
                    Settings.AppFocusPause = (AppRulesEnum)_selectedAppFocusIndex;
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
                OnPropertyChanged("SelectedBatteryPowerIndex");

                if(Settings.BatteryPause != (AppRulesEnum)_selectedBatteryPowerIndex)
                {
                    Settings.BatteryPause = (AppRulesEnum)_selectedBatteryPowerIndex;
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
                OnPropertyChanged("SelectedDisplayPauseRuleIndex");

                if(Settings.DisplayPauseSettings != (DisplayPauseEnum)_selectedDisplayPauseRuleIndex)
                {
                    Settings.DisplayPauseSettings = (DisplayPauseEnum)_selectedDisplayPauseRuleIndex;
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
                OnPropertyChanged("SelectedPauseAlgorithmIndex");

                if(Settings.ProcessMonitorAlgorithm != (ProcessMonitorAlgorithm)_selectedPauseAlgorithmIndex)
                {
                    Settings.ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)_selectedPauseAlgorithmIndex;
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
                OnPropertyChanged("SelectedWallpaperScalingIndex");

                if (Settings.WallpaperScaling != (WallpaperScaler)_selectedWallpaperScalingIndex)
                {
                    Settings.WallpaperScaling = (WallpaperScaler)_selectedWallpaperScalingIndex;
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
                OnPropertyChanged("SelectedWallpaperInputMode");

                if (Settings.InputForward != (InputForwardMode)_selectedWallpaperInputMode)
                {
                    Settings.InputForward = (InputForwardMode)_selectedWallpaperInputMode;
                    UpdateConfigFile();
                }

                //todo: show msg to user desc whats happening.
                if (Settings.InputForward == InputForwardMode.mousekeyboard)
                {
                    Helpers.DesktopUtil.SetDesktopIconVisibility(false);
                }
                else
                {
                    Helpers.DesktopUtil.SetDesktopIconVisibility(Helpers.DesktopUtil.DesktopIconVisibilityDefault);
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
                OnPropertyChanged("SelectedVideoPlayerIndex");

                if (Settings.VideoPlayer != (LivelyMediaPlayer)_selectedVideoPlayerIndex)
                {
                    Settings.VideoPlayer = (LivelyMediaPlayer)_selectedVideoPlayerIndex;
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
                OnPropertyChanged("VideoPlayerHWDecode");
                if (Settings.VideoPlayerHwAccel != _videoPlayerHWDecode)
                {
                    Settings.VideoPlayerHwAccel = _videoPlayerHWDecode;
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
                OnPropertyChanged("SelectedGifPlayerIndex");
                if (Settings.GifPlayer != (LivelyGifPlayer)_selectedGifPlayerIndex)
                {
                    Settings.GifPlayer = (LivelyGifPlayer)_selectedGifPlayerIndex;
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
                OnPropertyChanged("SelectedWallpaperStreamQualityIndex");
                if(Settings.StreamQuality != (StreamQualitySuggestion)_selectedWallpaperStreamQualityIndex)
                {
                    Settings.StreamQuality = (StreamQualitySuggestion)_selectedWallpaperStreamQualityIndex;
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
                OnPropertyChanged("SelectedWebBrowserIndex");

                if (Settings.WebBrowser != (LivelyWebBrowser)_selectedWebBrowserIndex)
                {
                    Settings.WebBrowser = (LivelyWebBrowser)_selectedWebBrowserIndex;
                    UpdateConfigFile();

                    WallpaperRestart(WallpaperType.web);
                    WallpaperRestart(WallpaperType.webaudio);
                    WallpaperRestart(WallpaperType.url);
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
                OnPropertyChanged("MouseMoveOnDesktop");

                if (Settings.MouseInputMovAlways != _mouseMoveOnDesktop)
                {
                    Settings.MouseInputMovAlways = _mouseMoveOnDesktop;
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
                if(Settings.WebDebugPort != _webDebuggingPort)
                {
                    Settings.WebDebugPort = _webDebuggingPort;
                    UpdateConfigFile();
                }
                OnPropertyChanged("WebDebuggingPort");
            }
        }

        private bool _cefDiskCache;
        public bool CefDiskCache
        {
            get { return _cefDiskCache; }
            set
            {
                _cefDiskCache = value;
                if(Settings.CefDiskCache != _cefDiskCache)
                {
                    Settings.CefDiskCache = _cefDiskCache;
                    UpdateConfigFile();
                }
                OnPropertyChanged("CefDiskCache");
            }
        }

        private bool _detectStreamWallpaper;
        public bool DetectStreamWallpaper
        {
            get { return _detectStreamWallpaper; }
            set
            {
                _detectStreamWallpaper = value;
                if(Settings.AutoDetectOnlineStreams != _detectStreamWallpaper)
                {
                    Settings.AutoDetectOnlineStreams = _detectStreamWallpaper;
                    UpdateConfigFile();
                }
                OnPropertyChanged("DetectStreamWallpaper");
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
                if (Settings.AudioVolumeGlobal != _globalWallpaperVolume)
                {
                    Settings.AudioVolumeGlobal = _globalWallpaperVolume;
                    UpdateConfigFile();
                }
                OnPropertyChanged("GlobalWallpaperVolume");
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
                if (Settings.AudioOnlyOnDesktop != _isAudioOnlyOnDesktop)
                {
                    Settings.AudioOnlyOnDesktop = _isAudioOnlyOnDesktop;
                    UpdateConfigFile();
                }
                OnPropertyChanged("IsAudioOnlyOnDesktop");              
            }
        }

        #endregion //audio

        #region system

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
                OnPropertyChanged("IsLockScreenAutoWallpaper");
            }
        }

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
                if (Settings.DesktopAutoWallpaper != _isDesktopAutoWallpaper)
                {
                    Settings.DesktopAutoWallpaper = _isDesktopAutoWallpaper;
                    UpdateConfigFile();
                }
                OnPropertyChanged("IsDesktopAutoWallpaper");
            }
        }

        private bool taskbarThemeInit = false;
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
                if (!taskbarThemeInit)
                {
                    if ((TaskbarTheme)_selectedTaskbarThemeIndex != TaskbarTheme.none)
                    {
                        string pgm = null;
                        if ((pgm = Helpers.TransparentTaskbar.CheckIncompatiblePrograms()) == null)
                        {
                            Helpers.TransparentTaskbar.Instance.Start((TaskbarTheme)_selectedTaskbarThemeIndex);
                            taskbarThemeInit = true;
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
                    Helpers.TransparentTaskbar.Instance.Start((TaskbarTheme)_selectedTaskbarThemeIndex);
                }
                //save the data..
                if (Settings.SystemTaskbarTheme != (TaskbarTheme)_selectedTaskbarThemeIndex)
                {
                    Settings.SystemTaskbarTheme = (TaskbarTheme)_selectedTaskbarThemeIndex;
                    UpdateConfigFile();
                }
                OnPropertyChanged("SelectedTaskbarThemeIndex");
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
                    Helpers.ScreensaverService.Instance.StartIdleTimer(idleTime);
                }
                else
                {
                    Helpers.ScreensaverService.Instance.StopIdleTimer();
                }
                //save the data..
                if (Settings.ScreensaverIdleWait !=  (ScreensaverIdleTime)_selectedScreensaverWaitIndex)
                {
                    if (!Settings.ScreensaverOledWarning)
                    {
                        _ = Task.Run(() =>
                               System.Windows.MessageBox.Show(Properties.Resources.DescOledScreensaverNotice,
                                   Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information));
                        Settings.ScreensaverOledWarning = true;
                    }
                    Settings.ScreensaverIdleWait = (ScreensaverIdleTime)_selectedScreensaverWaitIndex;
                    UpdateConfigFile();
                }
                OnPropertyChanged("SelectedScreensaverWaitIndex");
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
                if (Settings.ScreensaverLockOnResume != _isScreensaverLockOnResume)
                {
                    Settings.ScreensaverLockOnResume = _isScreensaverLockOnResume;
                    UpdateConfigFile();
                }
                OnPropertyChanged("IsScreensaverLockOnResume");
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
                OnPropertyChanged("IsSysTrayIconVisible");
                TrayIconVisibilityChange?.Invoke(null, _isSysTrayIconVisible);
                if (Settings.SysTrayIcon != _isSysTrayIconVisible)
                {
                    Settings.SysTrayIcon = _isSysTrayIconVisible;
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
                OnPropertyChanged("IsDebugMenuVisible");
                if (Settings.DebugMenu != _isDebugMenuVisible)
                {
                    DebugMenuVisibilityChange?.Invoke(null, _isDebugMenuVisible);
                    Settings.DebugMenu = _isDebugMenuVisible;
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
                            param => NLogger.ExtractLogFiles()
                        );
                }
                return _extractLogCommand;
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
                LivelyGifPlayer.win10Img => true,
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
            var originalWallpapers = SetupDesktop.Wallpapers.FindAll(x => x.GetWallpaperType() == type);
            if (originalWallpapers.Count > 0)
            {
                SetupDesktop.TerminateWallpaper(type);
                foreach (var item in originalWallpapers)
                {
                    SetupDesktop.SetWallpaper(item.GetWallpaperData(), item.GetScreen());
                    if (Settings.WallpaperArrangement == WallpaperArrangement.span
                        || Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
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
                if(string.Equals(folderBrowserDialog.SelectedPath, Settings.WallpaperDir, StringComparison.OrdinalIgnoreCase))
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
                            var result = System.Windows.MessageBox.Show("Did you mean to select?\n" + parentDir +
                                "\nBoth 'SaveData' and 'wallpapers' folders are required by lively!",
                                Properties.Resources.TitlePleaseWait, 
                                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                            switch (result)
                            {
                                case MessageBoxResult.Yes:
                                    folderBrowserDialog.SelectedPath = parentDir;
                                    break;
                                case MessageBoxResult.No:
                                    //none
                                    break;
                            }
                        }
                    }

                    WallpapeDirectoryChanging = true;
                    WallpaperDirectoryChangeCommand.RaiseCanExecuteChanged();
                    //create destination directory's if not exist.
                    Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "wallpapers"));
                    Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wptmp"));
                    Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wpdata"));

                    if (Settings.WallpaperDirMoveExistingWallpaperNewDir)
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
                SetupDesktop.TerminateAllWallpapers();

                var previousDirectory = Settings.WallpaperDir;
                Settings.WallpaperDir = folderBrowserDialog.SelectedPath;
                UpdateConfigFile();
                WallpaperDirectory = Settings.WallpaperDir;
                Program.WallpaperDir = Settings.WallpaperDir;
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
