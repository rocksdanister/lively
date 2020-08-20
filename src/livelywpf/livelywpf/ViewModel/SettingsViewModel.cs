using livelywpf.Core;
using livelywpf.Views;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            Settings = SettingsJSON.LoadConfig(Path.Combine(Program.AppDataDir, "Settings.json"));
            if (Settings == null)
            {
                Settings = new SettingsModel();
                SettingsJSON.SaveConfig(Path.Combine(Program.AppDataDir, "Settings.json"), Settings);
            }

            //lang-codes: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
            LanguageItems = new ObservableCollection<LanguagesModel>()
            {
                    new LanguagesModel("English(en)", new string[]{"en", "en-US"}),
                    new LanguagesModel("中文(zh-CN)", new string[]{"zh", "zh-Hans","zh-CN","zh-SG"}), 
                    new LanguagesModel("한국어(ko-KR)", new string[]{"ko", "ko-KR","ko-KP"}),
                    new LanguagesModel("Pусский(ru)", new string[]{"ru", "ru-BY", "ru-KZ", "ru-KG", "ru-MD", "ru-RU","ru-UA"}), 
                    new LanguagesModel("Українська(uk)", new string[]{"uk", "uk-UA"}),
                    new LanguagesModel("Español(es)", new string[]{"es"}),
                    new LanguagesModel("Italian(it)", new string[]{"it", "it-IT", "it-SM","it-CH","it-VA"}),
                    new LanguagesModel("عربى(ar-AE)", new string[]{"ar"}),
                    new LanguagesModel("Française(fr)", new string[]{"fr"}),
                    new LanguagesModel("Deutsche(de)", new string[]{"de"}),
                    new LanguagesModel("portuguesa(pt)", new string[]{"pt"}),
                    new LanguagesModel("portuguesa(pt-BR)", new string[]{"pt-BR"}),
                    new LanguagesModel("Filipino(fil)", new string[]{"fil","fil-PH"}),
                    new LanguagesModel("Magyar(hu)", new string[]{"hu","hu-HU"}),
            };
            SelectedLanguageItem = SearchSupportedLanguage(Settings.Language);

            var startupStatus = WindowsStartup.CheckStartupRegistry();
            if (startupStatus)
            {
                IsStartup = true;
            }
            else
            {
                IsStartup = false;
                //delete the wrong key if any.
                WindowsStartup.SetStartupRegistry(false);
            }

            if(ScreenHelper.GetScreen().FindIndex(x => ScreenHelper.ScreenCompare(x, Settings.SelectedDisplay, DisplayIdentificationMode.screenLayout)) == -1)
            {
                //Previous screen missing, use current primary screen.
                Settings.SelectedDisplay = ScreenHelper.GetPrimaryScreen();
                UpdateConfigFile();
            }

            SelectedTileSizeIndex = Settings.TileSize;
            SelectedAppFullScreenIndex = (int)Settings.AppFullscreenPause;
            SelectedAppFocusIndex = (int)Settings.AppFocusPause;
            SelectedBatteryPowerIndex = (int)Settings.BatteryPause;
            SelectedDisplayPauseRuleIndex = (int)Settings.DisplayPauseSettings;
            SelectedPauseAlgorithmIndex = (int)Settings.ProcessMonitorAlgorithm;
            SelectedVideoPlayerIndex = (int)Settings.VideoPlayer;
            SelectedLivelyUIModeIndex = (int)Settings.LivelyGUIRendering;
            SelectedWallpaperInputMode = (int)Settings.InputForward;
            MouseMoveOnDesktop = Settings.MouseInputMovAlways;
            IsSysTrayIconVisible = Settings.SysTrayIcon;
            WebDebuggingPort = Settings.WebDebugPort;
            DetectStreamWallpaper = Settings.AutoDetectOnlineStreams;
            WallpaperDirectory = Settings.WallpaperDir;
            MoveExistingWallpaperNewDir = Settings.WallpaperDirMoveExistingWallpaperNewDir;
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
            //testing
            SettingsJSON.SaveConfig(Path.Combine(Program.AppDataDir, "Settings.json"), Settings);
        }

        /// <summary>
        /// Checks LanguageItems and see if language with the given code exists.
        /// </summary>
        /// <param name="langCode">language code</param>
        /// <returns>Languagemodel if found; null otherwise.</returns>
        private LanguagesModel SearchSupportedLanguage(string langCode)
        {
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

                WindowsStartup.SetStartupRegistry(value);
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
                if(LanguageItems.Contains(value))
                {
                    _selectedLanguageItem = value;
                }
                else
                {
                    //en-US
                    _selectedLanguageItem = LanguageItems[0];
                }
                OnPropertyChanged("SelectedLanguageItem");

                if(_selectedLanguageItem.Codes.FirstOrDefault(x => x == Settings.Language) == null)
                {
                    //Settings.IsRestart = true;
                    Settings.Language = _selectedLanguageItem.Codes[0];
                    UpdateConfigFile();
                    //Program.RestartApplication();

                    //todo: temporary only, change it to better.
                    System.Windows.MessageBox.Show(Properties.Resources.DescriptionPleaseRestartLively, Properties.Resources.TitleAppName);
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

                //todo: argumentoutofrange exception
                Settings.TileSize = value;
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

        public event EventHandler<string> LivelyWallpaperDirChange;
        private RelayCommand _wallpaperDirectoryChangeCommand;
        public RelayCommand WallpaperDirectoryChangeCommand
        {
            get
            {
                if (_wallpaperDirectoryChangeCommand == null)
                {
                    _wallpaperDirectoryChangeCommand = new RelayCommand(
                        param => WallpaperDirectoryChange(), param => !WallpapeDirectoryChanging
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

        private async void WallpaperDirectoryChange()
        {
            bool isLivelyDir = false;
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    WallpapeDirectoryChanging = true;
                    WallpaperDirectoryChangeCommand.RaiseCanExecuteChanged();

                    if (Directory.Exists(Path.Combine(folderBrowserDialog.SelectedPath, "wallpapers")) &&
                        Directory.Exists(Path.Combine(folderBrowserDialog.SelectedPath, "SaveData")) ||
                        !Settings.WallpaperDirMoveExistingWallpaperNewDir)
                    {
                        //if directory exists, do not copy existing files.
                        isLivelyDir = true;
                        Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "wallpapers"));
                        Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wptmp"));
                        Directory.CreateDirectory(Path.Combine(folderBrowserDialog.SelectedPath, "SaveData", "wpdata"));
                    }
                    else
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

                //close all running wp's.
                SetupDesktop.CloseAllWallpapers();

                var previousDirectory = Settings.WallpaperDir;
                Settings.WallpaperDir = folderBrowserDialog.SelectedPath;
                UpdateConfigFile();
                WallpaperDirectory = Settings.WallpaperDir;
                Program.WallpaperDir = Settings.WallpaperDir;
                LivelyWallpaperDirChange?.Invoke(null, folderBrowserDialog.SelectedPath);

                if (!isLivelyDir)
                {
                    //not deleting the root folder, what if the user selects a folder that is not used by Lively alone!
                    var result1 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, "wallpapers"), 1000, 3000);
                    var result2 = await FileOperations.DeleteDirectoryAsync(Path.Combine(previousDirectory, "SaveData"), 0, 1000);
                    if (!(result1 && result2))
                    {
                        System.Windows.MessageBox.Show("Failed to delete old wallpaper directory!\nTry deleting it manually.", "Error");
                    }
                }
                folderBrowserDialog.Dispose();
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
            ApplicationRulesView app = new ApplicationRulesView
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = App.AppWindow,
            };
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

                //todo: argumentoutofrange exception
                Settings.AppFullscreenPause = (AppRulesEnum)value;
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

                //todo: argumentoutofrange exception
                Settings.AppFocusPause = (AppRulesEnum)value;
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

                //todo: argumentoutofrange exception
                Settings.BatteryPause = (AppRulesEnum)value;
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

                //todo: argumentoutofrange exception
                Settings.DisplayPauseSettings = (DisplayPauseEnum)value;
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

                //todo: argumentoutofrange exception
                Settings.ProcessMonitorAlgorithm = (ProcessMonitorAlgorithm)value;
            }
        }

        #endregion performance

        #region wallpaper

        private int _selectedWallpaperInputMode;
        public int SelectedWallpaperInputMode
        {
            get { return _selectedWallpaperInputMode; }
            set
            {
                _selectedWallpaperInputMode = value;
                SetupDesktop.WallpaperInputForward((InputForwardMode)_selectedWallpaperInputMode);
                OnPropertyChanged("SelectedWallpaperInputMode");
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
                if(_selectedVideoPlayerIndex != value)
                {
                    VideoPlayerSwitch((LivelyMediaPlayer)value);
                }
                _selectedVideoPlayerIndex = value;
                OnPropertyChanged("SelectedVideoPlayerIndex");

                //todo: argumentoutofrange exception
                Settings.VideoPlayer = (LivelyMediaPlayer)value;
                UpdateConfigFile();
            }
        }

        private void VideoPlayerSwitch(LivelyMediaPlayer player)
        {
            List<LibraryModel> wpCurr = new List<LibraryModel>();
            foreach (var item in SetupDesktop.Wallpapers)
            {
                if(item.GetWallpaperType() == WallpaperType.video)
                    wpCurr.Add(item.GetWallpaperData());
            }
            SetupDesktop.CloseWallpaper(WallpaperType.video);

            //todo: restart wpCurr
        }

        private bool _mouseMoveOnDesktop;
        public bool MouseMoveOnDesktop
        {
            get { return _mouseMoveOnDesktop; }
            set
            {
                _mouseMoveOnDesktop = value;
                Settings.MouseInputMovAlways = _mouseMoveOnDesktop;
                OnPropertyChanged("MouseMoveOnDesktop");
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

        public bool _detectStreamWallpaper;
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
                Settings.SysTrayIcon = _isSysTrayIconVisible;
                OnPropertyChanged("IsSysTrayIconVisible");
                TrayIconVisibilityChange?.Invoke(null, _isSysTrayIconVisible);
            }
        }

        #endregion //misc
    }
}
