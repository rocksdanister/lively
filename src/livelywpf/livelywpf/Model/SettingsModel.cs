using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace livelywpf
{

    [Serializable]
    public class SettingsModel : ObservableObject
    {
        private string _appVersion;
        public string AppVersion
        {
            get
            {
                return _appVersion;
            }
            set
            {
                _appVersion = value;
                OnPropertyChanged("AppVersion");
            }
        }

        private string _language;
        public string Language
        {
            get
            {
                return _language;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    _language = "en-US";
                }
                else
                {
                    _language = value;
                }
                OnPropertyChanged("Language");
            }
        }

        private bool _startup;
        public bool Startup
        {
            get
            {
                return _startup;
            }
            set
            {
                _startup = value;
                OnPropertyChanged("Startup");
            }
        }

        private bool _generateTile;
        /// <summary>
        /// Add user opened wallpapers to library.
        /// </summary>
        public bool GenerateTile
        {
            get
            {
                return _generateTile;
            }
            set
            {
                _generateTile = value;
                OnPropertyChanged("GenerateTile");
            }
        }

        private bool _livelyZipGenerate;
        /// <summary>
        /// create lively .zip file for dropped wp's after importing to library.
        /// </summary>
        public bool LivelyZipGenerate
        {
            get
            {
                return _livelyZipGenerate;
            }
            set
            {
                _livelyZipGenerate = value;
                OnPropertyChanged("LivelyZipGenerate");
            }
        }

        private bool _waterMarkTile;
        /// <summary>
        /// Show wallpaper info icon topright of library tile.
        /// </summary>
        public bool WaterMarkTile
        {
            get
            {
                return _waterMarkTile;
            }
            set
            {
                _waterMarkTile = value;
                OnPropertyChanged("WaterMarkTile");
            }
        }

        private bool _isFirstRun;
        /// <summary>
        /// First time lively run.
        /// </summary>
        public bool IsFirstRun
        {
            get
            {
                return _isFirstRun;
            }
            set
            {
                _isFirstRun = value;
                OnPropertyChanged("IsFirstRun");
            }
        }

        private AppRulesEnum _appFocusPuase;
        public AppRulesEnum AppFocusPause
        {
            get
            {
                return _appFocusPuase;
            }
            set
            {
                _appFocusPuase = value;
                OnPropertyChanged("AppFocusPause");
            }
        }

        private AppRulesEnum _appFullscreenPause;
        public AppRulesEnum AppFullscreenPause
        {
            get
            {
                return _appFullscreenPause;
            }
            set
            {
                _appFullscreenPause = value;
                OnPropertyChanged("AppFullscreenPause");
            }
        }

        private AppRulesEnum _batteryPause;
        public AppRulesEnum BatteryPause
        {
            get
            {
                return _batteryPause;
            }
            set
            {
                _batteryPause = value;
                OnPropertyChanged("BatteryPause");
            }
        }

        private DisplayPauseEnum _displayPauseSettings;
        public DisplayPauseEnum DisplayPauseSettings
        {
            get
            {
                return _displayPauseSettings;
            }
            set
            {
                _displayPauseSettings = value;
                OnPropertyChanged("DisplayPauseSettings");
            }
        }

        private ProcessMonitorAlgorithm _processMonitorAlgorithm;
        public ProcessMonitorAlgorithm ProcessMonitorAlgorithm
        {
            get
            {
                return _processMonitorAlgorithm;
            }
            set
            {
                _processMonitorAlgorithm = value;
                OnPropertyChanged("ProcessMonitorAlgorithm");
            }
        }

        private bool _liveTile;
        /// <summary>
        /// Show animatd library tiles.
        /// </summary>
        public bool LiveTile
        {
            get
            {
                return _liveTile;
            }
            set
            {
                _liveTile = value;
                OnPropertyChanged("LiveTile");
            }
        }

        private System.Windows.Media.Stretch _scalerVideo;
        public System.Windows.Media.Stretch ScalerVideo
        {
            get
            {
                return _scalerVideo;
            }
            set
            {
                _scalerVideo = value;
                OnPropertyChanged("ScalerVideo");
            }
        }

        private System.Windows.Media.Stretch _scalerGif;
        public System.Windows.Media.Stretch ScalerGif
        {
            get
            {
                return _scalerGif;
            }
            set
            {
                _scalerGif = value;
                OnPropertyChanged("ScalerGif");
            }
        }

        private StreamQualitySuggestion _streamQuality;
        /// <summary>
        /// Video stream quality for youtube-dl, 0 - best(4k)
        /// </summary>
        public StreamQualitySuggestion StreamQuality
        {
            get
            {
                return _streamQuality;
            }
            set
            {
                _streamQuality = value;
                OnPropertyChanged("StreamQuality");
            }
        }

        private WallpaperArrangement _wallpaperArrangement;
        public WallpaperArrangement WallpaperArrangement
        {
            get
            {
                return _wallpaperArrangement;
            }
            set
            {
                _wallpaperArrangement = value;
                OnPropertyChanged("WallpaperArrangement");
            }
        }

        private string _savedURL;
        public string SavedURL
        {
            get
            {
                return _savedURL;
            }
            set
            {
                //todo validate url
                _savedURL = value;
                OnPropertyChanged("SavedURL");
            }
        }

        private string _ignoreUpdateTag;
        public string IgnoreUpdateTag
        {
            get
            {
                return _ignoreUpdateTag;
            }
            set
            {
                _ignoreUpdateTag = value;
                OnPropertyChanged("IgnoreUpdateTag");
            }
        }

        private int _processTimerInterval;
        /// <summary>
        /// Timer interval(in milliseconds), used to monitor running apps to determine pause/play of wp's.
        /// </summary>
        public int ProcessTimerInterval
        {
            get
            {
                return _processTimerInterval;
            }
            set
            {
                _processTimerInterval = value;
                OnPropertyChanged("ProcessTimerInterval");
            }
        }

        private int _wallpaperWaitTime;
        /// <summary>
        /// Timeout for application wallpaper startup (in milliseconds), lively will kill wp if gui is not ready within this timeframe.
        /// </summary>
        public int WallpaperWaitTime
        {
            get
            {
                return _wallpaperWaitTime;
            }
            set
            {
                _wallpaperWaitTime = value;
                OnPropertyChanged("WallpaperWaitTime");
            }
        }

        private bool _safeShutDown;
        public bool SafeShutdown
        {
            get
            {
                return _safeShutDown;
            }
            set
            {
                _safeShutDown = value;
                OnPropertyChanged("SafeShutdown");
            }
        }

        private bool _isRestart;
        public bool IsRestart
        {
            get
            {
                return _isRestart;
            }
            set
            {
                _isRestart = value;
                OnPropertyChanged("IsRestart");
            }
        }

        private InputForwardMode _inputForward;
        public InputForwardMode InputForward
        {
            get
            {
                return _inputForward;
            }
            set
            {
                _inputForward = value;
                OnPropertyChanged("InputForward");
            }
        }

        private bool _mouseInputMovAlways;
        /// <summary>
        /// True: Always forward mouse movement, even when foreground apps open;
        /// False: Only forward on desktop.
        /// </summary>
        public bool MouseInputMovAlways
        {
            get
            {
                return _mouseInputMovAlways;
            }
            set
            {
                _mouseInputMovAlways = value;
                OnPropertyChanged("MouseInputMovAlways");
            }
        }

        private int _tileSize;
        public int TileSize
        {
            get
            {
                return _tileSize;
            }
            set
            {
                _tileSize = value;
                OnPropertyChanged("TileSize");
            }
        }

        private DisplayIdentificationMode _displayIdentification;
        public DisplayIdentificationMode DisplayIdentification
        {
            get
            {
                return _displayIdentification;
            }
            set
            {
                _displayIdentification = value;
                OnPropertyChanged("DisplayIdentification");
            }
        }

        private LivelyMediaPlayer _videoPlayer;
        public LivelyMediaPlayer VideoPlayer
        {
            get { return _videoPlayer; }
            set
            {
                _videoPlayer = value;
                OnPropertyChanged("VideoPlayer");
            }
        }

        private bool _gifCapture;
        public bool GifCapture
        {
            get { return _gifCapture; }
            set
            {
                _gifCapture = value;
                OnPropertyChanged("GifCapture");
            }
        }
        /*
        //todo need to rewrite audio manager from scratch.
        public bool MuteVideo { get; set; }
        public bool MuteCef { get; set; } //unused, need to get processid of subprocess of cef.
        public bool MuteCefAudioIn { get; set; }
        public bool MuteMic { get; set; }
        public bool MuteAppWP { get; set; }
        public bool MuteGlobal { get; set; } //mute audio of all types of wp's
        public bool AlwaysAudio { get; set; } //play audio even when not on desktop

        // warning user of risk, count.
        public int WarningUnity { get; set; }
        public int WarningGodot { get; set; }
        public int WarningURL { get; set; }
        public int WarningApp { get; set; }

        public bool RunOnlyDesktop { get; set; } //run only when on desktop focus.
        public VideoPlayer VidPlayer { get; set; }
        public GIFPlayer GifPlayer { get; set; }
        */

        public SettingsModel()
        {
            SavedURL = "https://www.shadertoy.com/view/MsKcRh";

            ProcessMonitorAlgorithm = ProcessMonitorAlgorithm.foreground;
            WallpaperArrangement = WallpaperArrangement.per;
            AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Startup = false;
            IsFirstRun = true;
            AppFocusPause = AppRulesEnum.ignore;
            AppFullscreenPause = AppRulesEnum.pause;
            BatteryPause = AppRulesEnum.ignore;
            VideoPlayer = LivelyMediaPlayer.libvlc;
            /*
            VidPlayer = VideoPlayer.windowsmp;
            //CurrWallpaperPath = null;
            MPVPath = null;
            RunOnlyDesktop = false;
            AppTransparency = false;
            GifPlayer = GIFPlayer.xaml;
            */

            WallpaperWaitTime = 30000; // 30sec
            ProcessTimerInterval = 500; //reduce to 250 for quicker response.
            Language = CultureInfo.CurrentCulture.Name;
            StreamQuality = StreamQualitySuggestion.h720p;
            GenerateTile = true;
            LivelyZipGenerate = false;
            WaterMarkTile = true;
            IgnoreUpdateTag = null;

            //media scaling
            ScalerVideo = System.Windows.Media.Stretch.Fill;
            ScalerGif = System.Windows.Media.Stretch.Fill;
            GifCapture = true;
            /*
            WarningApp = 0;
            WarningUnity = 0;
            WarningGodot = 0;
            WarningURL = 0;
            */

            SafeShutdown = true;
            IsRestart = false;

            InputForward = InputForwardMode.mouse;
            MouseInputMovAlways = true;

            TileSize = 1;
            DisplayIdentification = DisplayIdentificationMode.screenLayout;
        }
    }
}
