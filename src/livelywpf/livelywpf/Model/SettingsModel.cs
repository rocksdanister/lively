using livelywpf.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace livelywpf
{

    [Serializable]
    public class SettingsModel : ObservableObject
    {
        public string AppVersion { get; set; }
        public string Language { get; set; }
        public bool Startup { get; set; }
        /// <summary>
        /// Add user opened wallpapers to library.
        /// </summary>
        public bool GenerateTile { get; set; }

        public bool LivelyZipGenerate { get; set; }
        /// <summary>
        /// Show wallpaper info icon topright of library tile.
        /// </summary>
        public bool WaterMarkTile { get; set; }
        public bool IsFirstRun { get; set; }
        public AppRulesEnum AppFocusPause { get; set; }

        public AppRulesEnum AppFullscreenPause { get; set; }
        public AppRulesEnum BatteryPause { get; set; }

        public DisplayPauseEnum DisplayPauseSettings { get; set; }
        public ProcessMonitorAlgorithm ProcessMonitorAlgorithm { get; set; }
        /// <summary>
        /// Show animatd library tiles.
        /// </summary>
        public bool LiveTile { get; set; }
        public System.Windows.Media.Stretch ScalerVideo { get; set; }
        public System.Windows.Media.Stretch ScalerGif { get; set; }
        //public StreamQualitySuggestion StreamQuality { get; set; }
        public WallpaperArrangement WallpaperArrangement { get; set; }
        public string SavedURL { get; set; }
        public string IgnoreUpdateTag { get; set; }
        /// <summary>
        /// Timer interval(in milliseconds), used to monitor running apps to determine pause/play of wp's.
        /// </summary>
        public int ProcessTimerInterval { get; set; }
        /// <summary>
        /// Timeout for application wallpaper startup (in milliseconds), lively will kill wp if gui is not ready within this timeframe.
        /// </summary>
        public int WallpaperWaitTime { get; set; }
        public bool SafeShutdown { get; set; }
        public bool IsRestart { get; set; }
        public InputForwardMode InputForward { get; set; }
        /// <summary>
        /// True: Always forward mouse movement, even when foreground apps open;
        /// False: Only forward on desktop.
        /// </summary>
        public bool MouseInputMovAlways { get; set; }
        public int TileSize { get; set; }
        public DisplayIdentificationMode DisplayIdentification { get; set; }
        public LivelyMediaPlayer VideoPlayer { get; set; }
        public LivelyMediaPlayer StreamVideoPlayer { get; set; }
        public bool GifCapture { get; set; }
        public livelywpf.Core.LivelyScreen SelectedDisplay { get; set; }
        public LivelyGUIState LivelyGUIRendering { get; set; }
        public string WallpaperDir { get; set; }
        public bool WallpaperDirMoveExistingWallpaperNewDir { get; set; }
        public bool SysTrayIcon { get; set; }
        public bool AutoDetectOnlineStreams { get; set; }
        /// <summary>
        /// Cefsharp debug port.
        /// </summary>
        public string WebDebugPort { get; set; }
        public int WallpaperBundleVersion { get; set; }
        public StreamQualitySuggestion StreamQuality {get; set;}
        //private 
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
            VideoPlayer = LivelyMediaPlayer.libmpvExt;
            StreamVideoPlayer = LivelyMediaPlayer.libmpvExt;

            WallpaperWaitTime = 30000; // 30sec
            ProcessTimerInterval = 500; //reduce to 250 for quicker response.
            Language = CultureInfo.CurrentCulture.Name;
            StreamQuality = StreamQualitySuggestion.Highest;
            GenerateTile = true;
            LivelyZipGenerate = false;
            WaterMarkTile = true;
            IgnoreUpdateTag = null;

            //media scaling
            ScalerVideo = System.Windows.Media.Stretch.Fill;
            ScalerGif = System.Windows.Media.Stretch.Fill;
            GifCapture = true;

            SafeShutdown = true;
            IsRestart = false;

            InputForward = InputForwardMode.mouse;
            MouseInputMovAlways = true;

            TileSize = 1;
            DisplayIdentification = DisplayIdentificationMode.screenLayout;
            SelectedDisplay = ScreenHelper.GetPrimaryScreen();
            LivelyGUIRendering = LivelyGUIState.lite;
            //Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper");
            WallpaperDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lively Wallpaper", "Library");
            WallpaperDirMoveExistingWallpaperNewDir = false;
            SysTrayIcon = true;
            WebDebugPort = string.Empty;
            AutoDetectOnlineStreams = true;
            WallpaperBundleVersion = -1;
        }
    }
}
