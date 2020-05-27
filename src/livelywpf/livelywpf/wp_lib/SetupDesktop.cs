using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Shapes;
using System.Windows.Threading;
using static livelywpf.SaveData;
using MessageBox = System.Windows.MessageBox;

namespace livelywpf
{
    /// <summary>
    /// Main static class that deals with adding & managing wallpaper's (wp). 
    /// </summary>
    public static class SetupDesktop
    {   
        //todo:- remove/reduce redundant/useless variables.
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// List of currently running wallpapers, will NOT check for existing wp before adding new wp entry.
        /// </summary>
        public static List<WallpaperLayout> wallpapers = new List<WallpaperLayout>();
        private static DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private static IntPtr  workerWOrig, progman, desktopHandle, shellHandle; //handle,
        private static int processID;
        /// <summary>
        /// Amount of time to wait(approx) for external application wp to launch(milliseconds)
        /// </summary>
        public static int wallpaperWaitTime = 30000; //~30sec
        private static Task taskAppWait;
        private static bool _isInitialized = false;
        //private static bool _cefReady = true;

        public enum EngineState
        {
            [Description("All Wallpapers Paused")]
            paused,
            [Description("Normal")]
            normal
        }
        private static EngineState engineState = EngineState.normal;

        public enum WallpaperType
        {
            [Description("Application")]
            app,
            [Description("Webpage")]
            web,
            [Description("Webpage Audio Visualiser")]
            web_audio,
            [Description("Webpage Link")] //"Type" tab only, not for "Library"! 
            url, 
            [Description("Bizhawk Emulator")]
            bizhawk,
            [Description("Unity Game")]
            unity,
            [Description("Godot Game")]
            godot,
            [Description("Video")]
            video,
            [Description("Animated Gif")]
            gif,
            [Description("Unity Audio Visualiser")]
            unity_audio,
            [Description("Video Streams")]
            video_stream
        }

        public static EngineState GetEngineState()
        {
            return engineState;
        }

        public static void SetEngineState(EngineState state)
        {
            engineState = state;
        }

        #region wp_internal_data
        public class WPBaseClass
        {
            public IntPtr Handle { get; set; } //wp window handle.
            public string DisplayID { get; set; } //Screen class displayDevice name.

            public WPBaseClass(IntPtr handle, string displayID)
            {
                this.Handle = handle;
                this.DisplayID = displayID;
            }
        }

        public class WMPlayer : WPBaseClass //windows mediafoundation
        {
            public MediaPlayer MP { get; private set; }

            public WMPlayer(IntPtr handle, string displayID, MediaPlayer mp) : base(handle, displayID)
            {
                this.MP = mp;
            }
        }

        public class MediaKit : WPBaseClass //mediakit, external codec
        {
            public Mediakit MP { get; private set; }
            public bool IsGif { get; private set; } //video & gif support.

            public MediaKit(IntPtr handle, string displayID, Mediakit mp, bool isGIF) : base(handle, displayID)
            {
                this.MP = mp;
                this.IsGif = isGIF;
            }
        }

        public class GIFWallpaper : WPBaseClass //xamlanimatedgif
        {
            public GifWindow Gif { get; private set; }

            public GIFWallpaper(IntPtr handle, string displayID, GifWindow gifPlayer) : base(handle, displayID)
            {
                this.Gif = gifPlayer;
            }
        }

        public class ExtProgram : WPBaseClass //unity, godot, any apps.
        {
            public Process Proc { get; private set; }
            public WallpaperType Type { get; private set; }
            public UInt32 SuspendCnt { get; set; }

            public ExtProgram(IntPtr handle, string displayID, Process process, WallpaperType type, UInt32 suspendCnt) : base(handle, displayID)
            {
                this.Proc = process;
                this.Type = type;
                this.SuspendCnt = suspendCnt;
            }
        }

        public class ExtVidPlayers : WPBaseClass //mpv, vlc etc
        {
            public Process Proc { get; private set; }
            public WallpaperType Type { get; private set; }
            public UInt32 SuspendCnt { get; set; }
            public ExtVidPlayers(IntPtr handle, string displayID, Process process, WallpaperType type, UInt32 suspendCnt) : base(handle, displayID)
            {
                this.Proc = process;
                this.Type = type;
                this.SuspendCnt = suspendCnt;
            }
        }

        public class CefProcess : WPBaseClass //external cefsharp.
        {
            public Process Proc { get; private set; }
            public WallpaperType Type { get; private set; }
            public string FilePath { get; private set; }
            public UInt32 SuspendCnt { get; set; } //currently unused
            public bool ShowPreviewWindow { get; private set; }

            public CefProcess(IntPtr handle, string displayID, Process process, WallpaperType type, UInt32 suspendCnt, string filePath, bool showPrevWindow) : base(handle, displayID)
            {
                this.Proc = process;
                this.Type = type;
                this.SuspendCnt = suspendCnt;
                this.FilePath = filePath;
                this.ShowPreviewWindow = showPrevWindow;
            }
        }

        public static List<ExtProgram> extPrograms = new List<ExtProgram>();
        public static List<ExtVidPlayers> extVidPlayers = new List<ExtVidPlayers>();
        public static List<GIFWallpaper> gifWallpapers = new List<GIFWallpaper>();
        public static List<MediaKit> mediakitPlayers = new List<MediaKit>();
        public static List<WMPlayer> wmPlayers = new List<WMPlayer>();
        public static List<CefProcess> webProcesses = new List<CefProcess>();
        public static Process bizhawkProc = null;
        //private mpv mpvForm = null;
        //public VideoWindow vlcForm = null;
        #endregion wp_internal_data

        #region wp_core
        private static IntPtr workerw; //spawned window for wp.
        public static CancellationTokenSource ctsMonitor = new CancellationTokenSource();
        static CancellationTokenSource ctsProcessWait = null;// = new CancellationTokenSource();

        private static IntPtr folderView;
        //credit: https://www.codeproject.com/Articles/856020/Draw-Behind-Desktop-Icons-in-Windows-plus
        /// <summary>
        /// Setup wallpaper & start the process monitoring.
        /// </summary>
        /// <param name="layout"></param>
        public static async void SetWallpaper(SaveData.WallpaperLayout layout, bool showPreviewWindow)
        {
            if(MainWindow.HighContrastFix) //todo:- last minute addition, should properly finish it later.
            {
                //_isInitialized = true;
            }
            else if ( SystemInformation.HighContrast )
            {
                Logger.Error("Failed to setup, high contrast mode!");
                MessageBox.Show(Properties.Resources.msgHighContrastFailure, Properties.Resources.txtLivelyErrorMsgTitle);
                return;
            }
            else if ( !_isInitialized)
            {
                // Fetch the Progman window
                progman = NativeMethods.FindWindow("Progman", null);

                IntPtr result = IntPtr.Zero;

                // Send 0x052C to Progman. This message directs Progman to spawn a 
                // WorkerW behind the desktop icons. If it is already there, nothing 
                // happens.
                NativeMethods.SendMessageTimeout(progman,
                                       0x052C,
                                       new IntPtr(0),
                                       IntPtr.Zero,
                                       NativeMethods.SendMessageTimeoutFlags.SMTO_NORMAL,
                                       1000,
                                       out result);
                // Spy++ output
                // .....
                // 0x00010190 "" WorkerW
                //   ...
                //   0x000100EE "" SHELLDLL_DefView
                //     0x000100F0 "FolderView" SysListView32
                // 0x00100B8A "" WorkerW       <-- This is the WorkerW instance we are after!
                // 0x000100EC "Program Manager" Progman

                workerw = IntPtr.Zero;

                // We enumerate all Windows, until we find one, that has the SHELLDLL_DefView 
                // as a child. 
                // If we found that window, we take its next sibling and assign it to workerw.
                NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc((tophandle, topparamhandle) =>
                {
                    IntPtr p = NativeMethods.FindWindowEx(tophandle,
                                                IntPtr.Zero,
                                                "SHELLDLL_DefView",
                                                IntPtr.Zero);

                    if (p != IntPtr.Zero)
                    {
                        // Gets the WorkerW Window after the current one.
                        workerw = NativeMethods.FindWindowEx(IntPtr.Zero,
                                                       tophandle,
                                                       "WorkerW",
                                                       IntPtr.Zero);
                    }

                    return true;
                }), IntPtr.Zero);

                if ( IntPtr.Equals(workerw, IntPtr.Zero) || workerw == null )
                {
                    Logger.Error("Failed to setup, workerw handle null!");
                    //todo: set the settings through code using SystemParametersInfo() - complication: microsoft uses registry to update the radio button UI in the Performance dialog, 
                    //which DOES not reflect actual applied settings! o_O..will have to edit registry too.
                    MessageBox.Show(Properties.Resources.msgWorkerWFailure, Properties.Resources.txtLivelyErrorMsgTitle);
                    return;
                }
                else
                {
                    _isInitialized = true;
                }
            }

            if(!_timerInitilaized)
            {
                InitializeTimer();
            }
            dispatcherTimer.Stop();

            IntPtr handle = new IntPtr();
            if (layout.Type == WallpaperType.video)
            {
                if (SaveData.config.VidPlayer == SaveData.VideoPlayer.mediakit)
                {
                    Mediakit mediakitPlayer = new Mediakit(layout.FilePath, 100);
                    mediakitPlayer.Show();
                    handle = new WindowInteropHelper(mediakitPlayer).Handle;

                    mediakitPlayers.Add(new MediaKit(handle, layout.DeviceName, mediakitPlayer, false));
                }
                else if (SaveData.config.VidPlayer == SaveData.VideoPlayer.windowsmp)
                {
                    MediaPlayer wmPlayer = new MediaPlayer(layout.FilePath, 100);
                    wmPlayer.Show();
                    handle = new WindowInteropHelper(wmPlayer).Handle;

                    wmPlayers.Add(new WMPlayer(handle, layout.DeviceName, wmPlayer));
                }
                else if( SaveData.config.VidPlayer == VideoPlayer.mpv )
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = App.PathData +"\\external\\mpv\\mpv.exe",
                        UseShellExecute = false,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(layout.FilePath),
                        Arguments = "\"" + layout.FilePath + "\"" + " --force-window=yes --loop-file --keep-open --hwdec=yes --no-keepaspect" //+" --wid "+workerw  //--mute=yes 
                    };


                    Process proc = new Process(); //this compiler disposable object warning should be a mistake, the referenced object is disposed when wp is closed.
                    proc = Process.Start(startInfo);
                    try
                    {
                        ctsProcessWait = new CancellationTokenSource();
                        taskAppWait = Task.Run(() => WaitForProcesWindow(layout.Type, proc), ctsProcessWait.Token);
                        await taskAppWait;
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Info("app terminated early/user cancel");
                        try
                        {
                            proc.Kill();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
                        }

                        dispatcherTimer.Start();
                        return;
                    }
                    catch(InvalidOperationException e1)//app likely crashed/closed already!
                    {
                        Logger.Info("app crashed/terminated early(2):" + e1.ToString());
                        try
                        {
                            proc.Kill();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
                        }

                        dispatcherTimer.Start();
                        return;
                    }
                    catch(Exception e2)
                    {
                        Logger.Info("unexpected error:" + e2.ToString());
                        try
                        {
                            proc.Kill();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
                        }

                        dispatcherTimer.Start();
                        return;
                    }

                    handle = proc.MainWindowHandle;
                    if (handle.Equals(IntPtr.Zero))
                    {
                        Logger.Info("Error: could not get windowhandle after waiting..");
                        try
                        {
                            proc.Kill();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
                        }
                        //This usually happens if the app took too long then the timeout specified. (virus scan, too big a application, busy hdd? ).
                        MessageBox.Show(Properties.Resources.msgAppTimeout, Properties.Resources.txtLivelyErrorMsgTitle);

                        dispatcherTimer.Start();
                        return;
                    }

                    extVidPlayers.Add(new ExtVidPlayers(handle, layout.DeviceName, proc, WallpaperType.video, 0));

                    BorderlessWinStyle(handle);
                    RemoveWindowFromTaskbar(handle);
                    SaveData.runningPrograms.Add(new SaveData.RunningProgram { ProcessName = proc.ProcessName, Pid = proc.Id });
                    SaveData.SaveRunningPrograms();

                }
                AddWallpaper(handle, layout, showPreviewWindow);
            }
            else if (layout.Type == WallpaperType.gif)
            {
                if (SaveData.config.GifPlayer == SaveData.GIFPlayer.xaml)
                {
                    GifWindow gifForm = new GifWindow(layout.FilePath);
                    gifForm.Show();
                    handle = new WindowInteropHelper(gifForm).Handle;

                    //gifWallpapers.Add(new GIFWallpaper { gif = gifForm, handle = this.handle, displayID = layout.displayName });
                    gifWallpapers.Add(new GIFWallpaper(handle, layout.DeviceName, gifForm));
                }
                else if (SaveData.config.GifPlayer == SaveData.GIFPlayer.mediakit)
                {
                    Mediakit mediakitPlayer = new Mediakit(layout.FilePath, 100);
                    mediakitPlayer.Show();
                    handle = new WindowInteropHelper(mediakitPlayer).Handle;

                    //mediakitPlayers.Add(new MediaKit { mp = mediakitPlayer, handle = this.handle, displayID = layout.displayName, isGIF = true });
                    mediakitPlayers.Add(new MediaKit(handle, layout.DeviceName, mediakitPlayer, true));
                }

                AddWallpaper(handle, layout, showPreviewWindow);
            }
            else if (layout.Type == WallpaperType.unity || layout.Type == WallpaperType.unity_audio)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = layout.FilePath,
                    UseShellExecute = false,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(layout.FilePath),
                    //startInfo.Arguments = "-parentHWND " + workerw.ToString();// + " -popupwindow" + " -;  //easier & hides the window on launch, for consistency avoiding this. todo: Problem #1:cant get process window handle directly.
                    Arguments = "-popupwindow -screen-fullscreen 0" //-popupwindow removes from taskbar, -fullscreen flag to disable fullscreen mode if set during compilation (lively is handling resizing).
                };

                Process proc = new Process();
                proc = Process.Start(startInfo);
                try
                {
                    ctsProcessWait = new CancellationTokenSource();
                    taskAppWait = Task.Run(() => WaitForProcesWindow(layout.Type, proc), ctsProcessWait.Token);
                    await taskAppWait;

                }
                catch (OperationCanceledException)
                {
                    Logger.Info("app terminated early, user cancel/no-gui");
                    Debug.WriteLine("app terminated early, user cancel");
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }
                catch (InvalidOperationException e1)//app likely crashed/closed already!
                {
                    Logger.Info("app crashed/terminated early(2):" + e1.ToString());
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }
                catch (Exception e2) //unexpected error.
                {
                    Logger.Info("unexpected error:" + e2.ToString());
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }

                handle = proc.MainWindowHandle;
                if (handle.Equals(IntPtr.Zero))
                {
                    Logger.Info("Error: could not get windowhandle after waiting..");
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }
                    //This usually happens if the app took too long then the timeout specified. (virus scan, too big a application, busy hdd? ).
                    MessageBox.Show(Properties.Resources.msgAppTimeout, Properties.Resources.txtLivelyErrorMsgTitle);

                    dispatcherTimer.Start();
                    return;
                }

                extPrograms.Add(new ExtProgram(handle, layout.DeviceName, proc, layout.Type, 0));
                RemoveWindowFromTaskbar(handle);

                AddWallpaper(handle, layout, showPreviewWindow);

                //saving to list of pgms to kill in the event lively crashes.
                SaveData.runningPrograms.Add(new SaveData.RunningProgram { ProcessName = proc.ProcessName, Pid = proc.Id });
                SaveData.SaveRunningPrograms();
            }
            else if (layout.Type == WallpaperType.app || layout.Type == WallpaperType.video_stream) //video stream is using mpv player with youtube-dl
            {
                ProcessStartInfo startInfo;
                if (layout.Type == WallpaperType.video_stream)
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = App.PathData + "\\external\\mpv\\mpv.exe",
                        UseShellExecute = false,
                        WorkingDirectory = App.PathData + "\\external\\mpv",
                        Arguments = layout.Arguments
                    };
                }
                else
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = layout.FilePath,
                        UseShellExecute = false,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(layout.FilePath),
                        Arguments = layout.Arguments
                    };
                }

                Process proc = new Process(); //this compiler disposable object warning should be a mistake, the referenced object is disposed when wp is closed.
                proc = Process.Start(startInfo);
                try
                {
                    ctsProcessWait = new CancellationTokenSource();
                    taskAppWait = Task.Run(() => WaitForProcesWindow(layout.Type, proc), ctsProcessWait.Token);
                    await taskAppWait;
                }
                catch (OperationCanceledException)
                {
                    Logger.Info("app terminated early, user cancel");
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }
                catch (InvalidOperationException e1)//app likely crashed/closed already!
                {
                    Logger.Info("app crashed/terminated early(2):" + e1.ToString());
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }
                catch (Exception e2) //unexpected error.
                {
                    Logger.Info("unexpected error:" + e2.ToString());
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }

                handle = proc.MainWindowHandle;
                if (handle.Equals(IntPtr.Zero))
                {
                    Logger.Info("Error: could not get windowhandle after waiting..");
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }
                    //This usually happens if the app took too long then the timeout specified. (virus scan, too big a application, busy hdd? ).
                    MessageBox.Show(Properties.Resources.msgAppTimeout, Properties.Resources.txtLivelyErrorMsgTitle);
   
                    dispatcherTimer.Start();
                    return;
                }

                extPrograms.Add(new ExtProgram(handle, layout.DeviceName, proc, layout.Type, 0));

                BorderlessWinStyle(handle);
                RemoveWindowFromTaskbar(handle);

                AddWallpaper(handle, layout, showPreviewWindow);

                SaveData.runningPrograms.Add(new SaveData.RunningProgram { ProcessName = proc.ProcessName, Pid = proc.Id });
                SaveData.SaveRunningPrograms();

            }
            else if (layout.Type == WallpaperType.web || layout.Type == WallpaperType.url || layout.Type == WallpaperType.web_audio)
            {
                ProcessStartInfo start1 = new ProcessStartInfo();
                if (layout.Type == WallpaperType.web)
                {
                    start1.Arguments = "--url "+"\"" + layout.FilePath + "\"" + " --type local" +" --display "+ "\"" + layout.DeviceName + "\"" +
                                                            " --property "+ "\"" + System.IO.Path.Combine(App.PathData, "SaveData", "wpdata") +"\"";
                }
                else if (layout.Type == WallpaperType.web_audio)
                {
                    //start1.Arguments = "\"" + layout.FilePath + "\"" + @" local" + @" audio";
                    start1.Arguments = "--url " + "\"" + layout.FilePath + "\"" + " --type local" + " --display " + "\"" + layout.DeviceName + "\"" + " --audio true" + 
                                                                                    " --property " + "\"" + System.IO.Path.Combine(App.PathData, "SaveData", "wpdata") + "\"";
                }
                else
                {
                    //start1.Arguments = layout.FilePath + @" online";
                    start1.Arguments = "--url " + "\"" + layout.FilePath + "\"" + " --type online" + " --display " + "\"" + layout.DeviceName + "\"";
                }
                start1.FileName = System.IO.Path.Combine(App.PathData , "external","cef","LivelyCefSharp.exe");
                start1.RedirectStandardInput = true;
                start1.RedirectStandardOutput = true;
                start1.UseShellExecute = false;
                start1.WorkingDirectory = System.IO.Path.Combine(App.PathData, "external", "cef");

                Process webProcess = new Process();
                webProcess = Process.Start(start1);
                webProcess.EnableRaisingEvents = true;
                webProcess.OutputDataReceived += WebProcess_OutputDataReceived;
                //webProcess.Exited += WebProcess_Exited; //todo: see closeallwallpapers()
                webProcess.BeginOutputReadLine();

                //webProcesses.Add(new CefProcess { proc = webProcess, displayID = layout.displayName, type = layout.type, handle = IntPtr.Zero, suspendCnt = 0 });
                webProcesses.Add(new CefProcess(handle, layout.DeviceName, webProcess, layout.Type, 0, layout.FilePath, showPreviewWindow));

                SaveData.runningPrograms.Add(new SaveData.RunningProgram { ProcessName = webProcess.ProcessName, Pid = webProcess.Id });
                SaveData.SaveRunningPrograms();
            }
            else if (layout.Type == WallpaperType.godot)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = layout.FilePath,
                    UseShellExecute = false,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(layout.FilePath),
                    //Arguments = "--fullscreen"
                };
                Process proc = new Process();
                proc = Process.Start(startInfo);

                //await WaitForProcesWindow();
                try
                {
                    ctsProcessWait = new CancellationTokenSource();
                    taskAppWait = Task.Run(() => handle = WaitForProcesWindow(layout.Type, proc).Result, ctsProcessWait.Token);
                    await taskAppWait;
                }
                catch (OperationCanceledException)
                {
                    Logger.Info("app terminated early, user cancel/no-gui");
                    Debug.WriteLine("app terminated early, user cancel");
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }
                catch (InvalidOperationException e1)//app likely crashed/closed already!
                {
                    Logger.Info("app crashed/terminated early(2):" + e1.ToString());
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }
                catch (Exception e2) //unexpected error.
                {
                    Logger.Info("unexpected error:" + e2.ToString());
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }

                    dispatcherTimer.Start();
                    return;
                }

                if (handle.Equals(IntPtr.Zero))
                {
                    Logger.Info("Error: could not get windowhandle after waiting..");
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }
                    //This usually happens if the app took too long then the timeout specified. (virus scan, too big a application, busy hdd? ).
                    MessageBox.Show(Properties.Resources.msgAppTimeout, Properties.Resources.txtLivelyErrorMsgTitle);

                    dispatcherTimer.Start();
                    return;
                }

                extPrograms.Add(new ExtProgram(handle, layout.DeviceName, proc, layout.Type, 0));
                BorderlessWinStyle(handle);
                RemoveWindowFromTaskbar(handle);

                AddWallpaper(handle, layout, showPreviewWindow);

                SaveData.runningPrograms.Add(new SaveData.RunningProgram { ProcessName = proc.ProcessName, Pid = proc.Id });
                SaveData.SaveRunningPrograms();

            }
            else if (layout.Type == WallpaperType.bizhawk)
            {
                //loaded with custom configfile: global inputhooks, fullscreen, run in background.
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = layout.FilePath,
                    UseShellExecute = false,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(layout.FilePath),
                    Arguments = "--config=" + App.PathData + @"\BizhawkConfig.ini"
                };

                //Process proc = new Process();
                if (bizhawkProc == null)
                {
                    bizhawkProc = new Process();
                    bizhawkProc = Process.Start(startInfo);
                }

                try
                {
                    ctsProcessWait = new CancellationTokenSource();
                    taskAppWait = Task.Run(() => WaitForProcesWindow(layout.Type, bizhawkProc), ctsProcessWait.Token);
                    await taskAppWait;
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("bizhawk terminated early, user cancel");

                    dispatcherTimer.Start();
                    return;
                }


                handle = bizhawkProc.MainWindowHandle;
                if (IntPtr.Equals(handle, IntPtr.Zero))
                {
                    Logger.Info("Error: could not get windowhandle after waiting..");
                    try
                    {
                        bizhawkProc.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }
                    MessageBox.Show(Properties.Resources.msgAppTimeout, Properties.Resources.txtLivelyErrorMsgTitle);

                    dispatcherTimer.Start();
                    return;
                }

                BorderlessWinStyle(handle);
                RemoveWindowFromTaskbar(handle);

                AddWallpaper(handle, layout, showPreviewWindow);
                //StaticPinvoke.ShowWindow(handle, 3); //maximise

                SaveData.runningPrograms.Add(new SaveData.RunningProgram { ProcessName = bizhawkProc.ProcessName, Pid = bizhawkProc.Id });
                SaveData.SaveRunningPrograms();
            }
            else
            {
                Logger.Error("Unkown wallpapertype:" + layout.Type);

                dispatcherTimer.Start();
                return;
            }

            if(layout.Type == WallpaperType.video_stream)
            {
                layout.Arguments = null; //no need to store it, since it is being generated everytime.
            }
            wallpapers.Add(layout);
            SaveData.SaveWallpaperLayout();

            dispatcherTimer.Start();
        }
        #endregion wp_core

        #region wp_add
        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private static async void AddWallpaper(IntPtr handle, WallpaperLayout layout, bool showPreviewWindow)
        {
            if (showPreviewWindow && SaveData.config.GenerateTile)
            {
                await ShowPreviewDialogSTAThread(layout, handle);
            }

            string display = layout.DeviceName;
            if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
            {
                SpanWallpaper(handle);
                return;
            }

            //bottom-most window instead of behind-icon
            if (MainWindow.HighContrastFix)
            {
                foreach (var displayItem in Screen.AllScreens)
                {
                    if (display == displayItem.DeviceName)
                    {
                        SetWindowBottomMost(handle);
                        if (!NativeMethods.SetWindowPos(handle, 1, displayItem.Bounds.X, displayItem.Bounds.Y, (displayItem.WorkingArea.Width), (displayItem.WorkingArea.Height), 0 | 0x0010))
                        {
                            LogWin32Error("setwindowpos(1) fail addwallpaper(),");
                        }
                    }
                }
            }
            else
            {
                foreach (var displayItem in Screen.AllScreens)
                {
                    if (display == displayItem.DeviceName)
                    {
                        NativeMethods.RECT prct = new NativeMethods.RECT();
                        NativeMethods.POINT topLeft;
                        //StaticPinvoke.POINT bottomRight;

                        Logger.Info("Sending WP -> " + displayItem);
                        if (!NativeMethods.SetWindowPos(handle, 1, displayItem.Bounds.X, displayItem.Bounds.Y, (displayItem.Bounds.Width), (displayItem.Bounds.Height), 0 | 0x0010))
                        {
                            LogWin32Error("setwindowpos(2) fail AddWallpaper(),");
                        }

                        //ScreentoClient is no longer used, this supports windows mirrored mode also, calculate new relative position of window w.r.t parent.
                        NativeMethods.MapWindowPoints(handle, workerw, ref prct, 2);
                        //LogWin32Error("MapWindowPts addwallpaper(),");

                        SetParentWorkerW(handle);
                        //Position the wp window relative to the new parent window(workerw).
                        if (!NativeMethods.SetWindowPos(handle, 1, prct.Left, prct.Top, (displayItem.Bounds.Width), (displayItem.Bounds.Height), 0 | 0x0010))
                        {
                            LogWin32Error("setwindowpos(3) fail addwallpaper(),");
                        }

                        #region logging
                        NativeMethods.GetWindowRect(handle, out prct);
                        //Debug.WriteLine("current Window Coordinates: " + prct.Left + " " + prct.Right + " " + displayItem.Bounds.Width + " " + displayItem.Bounds.Height);
                        Logger.Info("Relative Coordinates of WP -> " + prct.Left + " " + prct.Right + " " + displayItem.Bounds.Width + " " + displayItem.Bounds.Height);
                        topLeft.X = prct.Left;
                        topLeft.Y = prct.Top;
                        NativeMethods.ScreenToClient(workerw, ref topLeft);
                        //Debug.WriteLine("current Window Coordinates(corrected): " + topLeft.X + " " + topLeft.Y + " " + displayItem.Bounds.Width + " " + displayItem.Bounds.Height);
                        Logger.Info("Coordinate wrt to screen ->" + topLeft.X + " " + topLeft.Y + " " + displayItem.Bounds.Width + " " + displayItem.Bounds.Height);
                        #endregion logging.
                        break;
                    }
                }
            }

            SetFocus(true);
            RefreshDesktop();
            //some websites don't have resizing events, reloading page after fullscreen to force resize.
            if (layout.Type == WallpaperType.web || layout.Type == WallpaperType.web_audio || layout.Type == WallpaperType.url)
            {
                var cefBrowser = webProcesses.Find(x => x.Handle.Equals(handle));
                cefBrowser.Proc.StandardInput.WriteLine("Reload");
            }
        }

        public static Task ShowPreviewDialogSTAThread(WallpaperLayout layout, IntPtr wallpaperHandle)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {  
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        PreviewWallpaper previewWindow = new PreviewWallpaper(wallpaperHandle, layout);
                        if (App.W != null)
                        {
                            previewWindow.Owner = App.W;
                            previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        }
                        previewWindow.ShowDialog();
                    }));
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                    Logger.Error(e.ToString());
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        /// <summary>
        /// Spans wp across all screens.
        /// </summary>
        private static void SpanWallpaper(IntPtr handle)
        {
            NativeMethods.RECT prct = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(workerw, out prct); //get spawned workerw rectangle data.
            SetParentWorkerW(handle);

            if(!NativeMethods.SetWindowPos(handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0 | 0x0010)) //fill wp into the whole workerw area.
            {
                LogWin32Error("setwindowpos fail SpanWallpaper(),");
            }

            SetFocus(true);
            RefreshDesktop();
        }
        #endregion wp_add

        #region wp_refresh
        /// <summary>
        /// Refresh wp span dimension.
        /// </summary>
        /// <param name="handle"></param>
        private static void SpanUpdate(IntPtr handle)
        {
            Logger.Info("Span wp rect Updating!");
            NativeMethods.RECT prct = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(workerw, out prct); //get spawned workerw rectangle data.
            NativeMethods.SetWindowPos(handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0 | 0x0010); //fill wp into the whole workerw area.
        }

        /// <summary>
        /// Update/Refresh all currently running wp's size & position.
        /// </summary>
        public static void UpdateAllWallpaperRect()
        {
            if(MainWindow.Multiscreen || MainWindow.HighContrastFix) //bug:wp disappearing, probably overlapping one another. todo:- fix, I think its due to the new setwindowpos introduced for displayID window.
            {
                Logger.Debug("wp rect adjustment disabled for multiscreen/highcontrast due to bug/incomplete, skipping!");
                return;
            }

            NativeMethods.RECT prct = new NativeMethods.RECT();
            int i = 0;
            //todo:- very lazy code..
            foreach (var item in Screen.AllScreens)
            {
                if( (i = extPrograms.FindIndex(x => x.DisplayID == item.DeviceName)) != -1)
                {
                    if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                    {
                        SpanUpdate(extPrograms[i].Handle);
                        break;
                    }

                    DisplayID displayID = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                    {
                        Opacity = 0
                    };
                    displayID.Show();
                    NativeMethods.MapWindowPoints(new WindowInteropHelper(displayID).Handle, workerw, ref prct, 2);
                    displayID.Close();
                    NativeMethods.SetWindowPos(extPrograms[i].Handle, 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010);
                    continue;
                }

                if ((i = extVidPlayers.FindIndex(x => x.DisplayID == item.DeviceName)) != -1)
                {
                    if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                    {
                        SpanUpdate(extVidPlayers[i].Handle);
                        break;
                    }

                    DisplayID displayID = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                    {
                        Opacity = 0
                    };
                    displayID.Show();
                    NativeMethods.MapWindowPoints(new WindowInteropHelper(displayID).Handle, workerw, ref prct, 2);
                    displayID.Close();
                    NativeMethods.SetWindowPos(extVidPlayers[i].Handle, 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010);
                    continue;
                }

                if ((i = gifWallpapers.FindIndex(x => x.DisplayID == item.DeviceName)) != -1)
                {
                    if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                    {
                        SpanUpdate(gifWallpapers[i].Handle);
                        break;
                    }

                    DisplayID displayID = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                    {
                        Opacity = 0
                    };
                    displayID.Show();
                    NativeMethods.MapWindowPoints(new WindowInteropHelper(displayID).Handle, workerw, ref prct, 2);
                    displayID.Close();

                    NativeMethods.SetWindowPos(gifWallpapers[i].Handle, 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010);
                    continue;
                }

                if ((i = mediakitPlayers.FindIndex(x => x.DisplayID == item.DeviceName)) != -1)
                {
                    if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                    {
                        SpanUpdate(mediakitPlayers[i].Handle);
                        break;
                    }

                    DisplayID displayID = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                    {
                        Opacity = 0
                    };
                    displayID.Show();
                    NativeMethods.MapWindowPoints(new WindowInteropHelper(displayID).Handle, workerw, ref prct, 2);
                    displayID.Close();

                    NativeMethods.SetWindowPos(mediakitPlayers[i].Handle, 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010);
                    continue;
                }

                if ((i = wmPlayers.FindIndex(x => x.DisplayID == item.DeviceName)) != -1)
                {
                    if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                    {
                        SpanUpdate(wmPlayers[i].Handle);
                        break;
                    }

                    DisplayID displayID = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                    {
                        Opacity = 0
                    };
                    displayID.Show();
                    NativeMethods.MapWindowPoints(new WindowInteropHelper(displayID).Handle, workerw, ref prct, 2);
                    displayID.Close();
                    //StaticPinvoke.MapWindowPoints(wmPlayers[i].handle, workerw, ref prct, 2);
                    NativeMethods.SetWindowPos(wmPlayers[i].Handle, 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010);
                    continue;
                }

                if ((i = webProcesses.FindIndex(x => x.DisplayID == item.DeviceName)) != -1)
                {
                    if (SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                    {
                        SpanUpdate(webProcesses[i].Handle);
                        break;
                    }

                    DisplayID displayID = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                    {
                        Opacity = 0
                    };
                    displayID.Show();
                    NativeMethods.MapWindowPoints(new WindowInteropHelper(displayID).Handle, workerw, ref prct, 2);
                    displayID.Close();

                    NativeMethods.SetWindowPos(webProcesses[i].Handle, 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010);
                    continue;
                }
            }
            RefreshDesktop();
        }
        #endregion wp_refreh

        #region cefsharp_events
        //public static IntPtr cefIntermediate;
        //public static IntPtr cefRenderWidget;
        /// <summary>
        /// STDOUT redirect message event of cefsharp browser process.
        /// </summary>
        private static void WebProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                IntPtr handle = new IntPtr();
                //Retrieves the windowhandle of cefsubprocess, cefsharp is launching cef as a separate proces..if you add the full pgm as child of workerw then there are problems (prob related sharing input queue)
                //Instead hiding the pgm window & adding cefrender window instead.
                Logger.Info("Cefsharp Handle:- " + e.Data);
                if (e.Data.Contains("HWND")) 
                {
                    var currProcess = webProcesses.Find(x => x.Proc == sender);

                    handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
                    //note-handle: WindowsForms10.Window.8.app.0.141b42a_r9_ad1

                    //hidin other windows, no longer required since I'm doing it in cefsharp pgm itself.
                    NativeMethods.ShowWindow(currProcess.Proc.MainWindowHandle, 0);

                    //WARNING:- If you put the whole cefsharp window, workerw crashes and refuses to start again on next startup!!, this is a workaround.
                    handle = NativeMethods.FindWindowEx(handle, IntPtr.Zero, "Chrome_WidgetWin_0", null);
                    //cefRenderWidget = StaticPinvoke.FindWindowEx(handle, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", null);
                    //cefIntermediate = StaticPinvoke.FindWindowEx(handle, IntPtr.Zero, "Intermediate D3D Window", null);
                    
                    if (IntPtr.Equals(handle, IntPtr.Zero))//unlikely.
                    {
                        try
                        {
                            currProcess.Proc.Kill();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error getting webview handle:- " + ex.ToString());
                            Debug.WriteLine("web handle 0:- " + ex.Message + " " + ex.StackTrace);
                        }

                        return;
                    }
                    
                    //webViewHandle = handle;
                    currProcess.Handle = handle;
                    //experiment
                    //SetParentWorkerW(handle);
                    //SetParent(handle, IntPtr.Zero);

                    //layout data is only used for drag & drop files, to create preview screen. 
                    AddWallpaper(handle, new WallpaperLayout() 
                                { 
                                    DeviceName = currProcess.DisplayID,
                                    FilePath = currProcess.FilePath,
                                    Arguments = null,
                                    Type = currProcess.Type 
                                }, currProcess.ShowPreviewWindow);
                }
            }
            catch (Exception)
            {
                //todo
            }

        }

        /// <summary>
        /// cefsharp browser exit event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WebProcess_Exited(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();

            int i;
            if ((i = webProcesses.FindIndex(x => x.Proc.HasExited == true)) != -1)
            {
                try
                {
                    webProcesses[i].Proc.Close();
                }
                catch(NullReferenceException ex)
                {
                    Logger.Info("browser proc already closed:-" + ex.ToString());
                }
                webProcesses.RemoveAt(i);
            }

            dispatcherTimer.Start();
            RefreshDesktop();
        }
        #endregion cefsharp_events

        #region wp_wait
        /// <summary>
        /// Check if started pgm wp is ready(GUI window started).
        /// </summary>
        /// <returns>true: process ready/halted, false: process still starting.</returns>
        public static int IsProcessWaitDone()
        {
            var task = taskAppWait;
            if (task != null)
            {
                if ((task.IsCompleted == false
                    || task.Status == TaskStatus.Running
                    || task.Status == TaskStatus.WaitingToRun
                    || task.Status == TaskStatus.WaitingForActivation
                    ))
                {
                    return 0;
                }
                return 1;
            }
            return 1;
        }

        /// <summary>
        /// Cancel waiting for pgm wp window to be ready.
        /// </summary>
        public static void TaskProcessWaitCancel()
        {
            if (ctsProcessWait == null)
                return;

            ctsProcessWait.Cancel();
            /*
            while (!taskAppWait.IsCanceled && !taskAppWait.IsCompleted)
            {

            }
            */
            ctsProcessWait.Dispose();
            ctsProcessWait = null;
        }

        private const int BM_CLICK = 0x00F5; //left-click
        /// <summary>
        /// Logic to search for window-handle of spawned pgm wp process.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static async Task<IntPtr> WaitForProcesWindow(WallpaperType type, Process proc)
        {
            if (proc == null)
            {
                return IntPtr.Zero;
            }

            IntPtr configW = IntPtr.Zero; 
            int i = 0;
            try
            {
                while (proc.WaitForInputIdle(-1) != true) //waiting for msgloop to be ready, gui not guaranteed to be ready.
                {
                    ctsProcessWait.Token.ThrowIfCancellationRequested();
                }
            }
            catch(InvalidOperationException) //no gui, failed to enter idle state.
            {
                _ = Task.Run(() => (MessageBox.Show(Properties.Resources.msgAppGUIFailure, Properties.Resources.txtLivelyErrorMsgTitle, 
                                                     MessageBoxButton.OK, MessageBoxImage.Error)));
                throw new OperationCanceledException();            
            }

            if (type == WallpaperType.godot)
            {
                while (i < wallpaperWaitTime && proc.HasExited == false) //15sec
                {
                    ctsProcessWait.Token.ThrowIfCancellationRequested();

                    //Debug.WriteLine(type + " waiting for handle(godot): " + i / 2f + "(s)" + " mainhandle: " + proc.MainWindowHandle + " is maximised" + NativeMethods.IsZoomed(proc.MainWindowHandle));
                    i++;
                    configW = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Engine", null);
                    if (!IntPtr.Equals(configW, IntPtr.Zero))
                        break;
                    //Task.Delay(500).Wait(); // 500x20 ~10sec
                    await Task.Delay(1);
                }
                //handle = configW;
                return configW;
            }
            else if (type == WallpaperType.unity || type == WallpaperType.unity_audio)
            {
                /*
                IntPtr tmpProc = IntPtr.Zero;
                i = 0;           
                while(i < 15 && proc.HasExited == false)
                {
                    tmpProc = StaticPinvoke.FindWindowEx(workerw, IntPtr.Zero, "UnityWndClass", proc.ProcessName);
                    Debug.WriteLine("process name:- " + proc.ProcessName);
                    if (!IntPtr.Equals(tmpProc, IntPtr.Zero))
                    {

                        int id;
                        StaticPinvoke.GetWindowThreadProcessId(tmpProc, out id);
                        if (proc == Process.GetProcessById(id))
                        {
                            proc.Refresh();
                            return;
                        }
                        else
                        {
                            tmpProc = StaticPinvoke.FindWindowEx(workerw, tmpProc, "UnityWndClass", proc.ProcessName);
                        }
                        return;
                    }
                    await Task.Delay(500);
                }
                */

                //Player settings dialog of Unity, simulating play button click or search workerw if paramter given in argument.
                i = 0;
                while (i < wallpaperWaitTime && proc.HasExited == false) //~30sec
                {
                    ctsProcessWait.Token.ThrowIfCancellationRequested();
                    Debug.WriteLine(type + " waiting for config-handle(unity): " + i + "(s)" + " mainhandle: " + proc.MainWindowHandle + " is maximised" + NativeMethods.IsZoomed(proc.MainWindowHandle));
                    i++;

                    if (!IntPtr.Equals(proc.MainWindowHandle, IntPtr.Zero))
                        break;
                    //Task.Delay(500).Wait(); // 10sec
                    await Task.Delay(1); 
                }
                configW = NativeMethods.FindWindowEx(proc.MainWindowHandle, IntPtr.Zero, "Button", "Play!");
                if (!IntPtr.Equals(configW, IntPtr.Zero))
                    NativeMethods.SendMessage(configW, BM_CLICK, IntPtr.Zero, IntPtr.Zero); //simulate Play! button click. (Unity config window)

                await Task.Delay(1);
            }
            proc.Refresh(); //update window-handle of unity config

            //there does not seem to be a "proper" way to check whether mainwindow is ready.
            i = 0;
            while (i < wallpaperWaitTime && proc.HasExited == false)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
                /*
                if (ctsProcessWait.IsCancellationRequested)
                {
                    return false;
                }
                */

                Debug.WriteLine(type + " waiting for handle(app): " + i / 2f + "(s)" + " mainhandle: " + proc.MainWindowHandle + " is maximised" + NativeMethods.IsZoomed(proc.MainWindowHandle) + " cancelation: " + ctsProcessWait.IsCancellationRequested);
                i++;
                if (!IntPtr.Equals(proc.MainWindowHandle, IntPtr.Zero))
                {
                    //moving the window out of screen.
                    //StaticPinvoke.SetWindowPos(proc.MainWindowHandle, 1, -20000, 0, 0, 0, 0x0010 | 0x0001); 
                    break;
                }
                //Task.Delay(500).Wait(); // 500x20 ~10sec
                await Task.Delay(1);
            }

            Debug.WriteLine(type + " waiting for handle:(app outside) " + i / 2f + "(s)" + " mainhandle: " + proc.MainWindowHandle + " is maximised" + NativeMethods.IsZoomed(proc.MainWindowHandle));
            proc.Refresh();
            if (proc.MainWindowHandle == IntPtr.Zero)
            {
                Logger.Info("Error: could not get windowhandle after waiting..");
                //MessageBox.Show("Error: could not get windowhandle after waiting..");
                return IntPtr.Zero;
            }
            else
                return proc.MainWindowHandle;
        }
        #endregion wp_wait

        #region thread_monitor_pause/play

        //todo:- remove/reduce redundant/useless variables.
        static IntPtr hWnd, shell_tray;
        static Process currProcess;
        //static string currProcessName;
        static System.Drawing.Rectangle screenBounds;
        static NativeMethods.RECT appBounds;
        static string currDisplay;

        /// <summary>
        /// Timer event, checks for running pgm & determine wp playback behavior.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ProcessMonitor(object sender, EventArgs e)
        {
            if (!_isInitialized)
            {
                dispatcherTimer.Stop();
                return;
            }

            if(engineState == EngineState.paused)
            {
                Pause.SuspendWallpaper(true);
                return;
            }

            if(SaveData.config.BatteryPause == AppRulesEnum.pause)
            {
                //on battery
                if (System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline)
                {
                    Pause.SuspendWallpaper(true);
                    return;
                }
            }

            if(SaveData.config.ProcessMonitorAlgorithm == SaveData.ProcessMonitorAlgorithm.foreground)
            {
                //light, reliable & quick; have some limitations when smaller foreground window opened on top of already maximised window, this will fail detection.
                ForegroundPauseFn();
            }
            else if (SaveData.config.ProcessMonitorAlgorithm == SaveData.ProcessMonitorAlgorithm.all) //TODO:- rewrite with EnumWindowsProc instead of going through all running Processes, might eliminate the issue's I'm having.(ps: i was wrong, nvrmind xD)
            {
                //todo:- this algorithm is incomplete for multiple-screen, have to finish it. (I dont like what I'm doing here).
                if(MainWindow.Multiscreen)
                {
                    /*
                    //IntPtr shellWindow = NativeMethods.GetShellWindow();
                    visibleWindows.Clear();
                    NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc(UpdateVisibleWindowsAllScreens), IntPtr.Zero);
                    foreach (var item in visibleWindows)
                    {
                        if (screens.Exists(x => x.DeviceName == MapWindowToMonitor(item)))
                        {
                            try
                            {
                                NativeMethods.GetWindowThreadProcessId(item, out processID);
                                currProcess = Process.GetProcessById(processID);
                                currProcessName = currProcess.ProcessName;
                            }
                            catch
                            {
                                //ignore, admin process etc
                                continue;
                            }

                            AllAppPauseFn(currProcess, screens.Find(x => x.DeviceName == MapWindowToMonitor(item)).DeviceName);
                            screens.Remove(screens.Find(x => x.DeviceName == MapWindowToMonitor(item)));
                        }
                    }
                    */

                    List<Screen> screens = Screen.AllScreens.ToList();
                    //lots of hack, many unwanted windows keep detecting even with all these checks (my Check_EX_Style() method might be wrong).
                    //problem is isZoomed fixes all the problems, except some games are not detected as maximised(eg: dota 2 windowed mode), so I have to check the windowsize using isZoomedCustom I made which also picks up unwanted windows.
                    //I decided to fix that issue by checking the window style which created limited success?
                    foreach (var item in Process.GetProcesses()) 
                    {
                        if (NativeMethods.IsWindowVisible(item.MainWindowHandle))
                        {
                            if ( !IntPtr.Equals(shellHandle,item.MainWindowHandle) && !NativeMethods.IsIconic(item.MainWindowHandle) && NativeMethods.GetWindowTextLength(item.MainWindowHandle) != 0 && 
                                !String.IsNullOrEmpty(item.ProcessName) && !IntPtr.Equals(item.MainWindowHandle, IntPtr.Zero) && !Check_EX_Style(item.MainWindowHandle, 0x08000000L) && !Check_EX_Style(item.MainWindowHandle, 0x00200000L)
                                                     && NativeMethods.IsZoomed(item.MainWindowHandle) || IsZoomedCustom(item.MainWindowHandle, null))//IsZoomedCustomAllDisplays(item.MainWindowHandle))
                            {
                                //things that popup when checking windowsize close to screensize, rip x_x
                                if (item.ProcessName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) || item.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)
                                                        || item.ProcessName.Equals("SystemSettings", StringComparison.OrdinalIgnoreCase) || item.ProcessName.Equals("MicrosoftEdgeCP", StringComparison.OrdinalIgnoreCase)
                                                        || item.ProcessName.Equals("video.ui", StringComparison.OrdinalIgnoreCase) || item.ProcessName.Equals("NVIDIA Share", StringComparison.OrdinalIgnoreCase)
                                                        || item.ProcessName.Equals("MicrosoftEdge", StringComparison.OrdinalIgnoreCase) || item.ProcessName.Equals("WindowsInternal.ComposableShell.Experiences.TextInput.InputApp", StringComparison.OrdinalIgnoreCase)
                                                        )
                                {
                                    continue;
                                }

                                try
                                {
                                    if (screens.Exists(x => x.DeviceName == MapWindowToMonitor(item.MainWindowHandle)))
                                    {
                                        AllAppPauseFn(item, screens.Find(x => x.DeviceName == MapWindowToMonitor(item.MainWindowHandle)).DeviceName);
                                        screens.Remove(screens.Find(x => x.DeviceName == MapWindowToMonitor(item.MainWindowHandle)));
                                    }
                                }
                                catch (ArgumentNullException)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    /*
                    hWnd = workerWOrig;
                    try
                    {
                        NativeMethods.GetWindowThreadProcessId(hWnd, out processID);
                        currProcess = Process.GetProcessById(processID);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("getting processname failure");
                        //ignore, admin process etc
                        return;
                    }
                    */
                    foreach (var item in screens)
                    {
                        Pause.ResumeWallpaper(false, item.DeviceName);
                        //AllAppPauseFn(currProcess, item.DeviceName);
                    }
                    
                }
                else //single screen, all-process pause.
                {                   
                    //interate through all the running processes, skipping unwanted ones based on condition ( all trial and error, needs a lot of further testing).
                    foreach (var item in Process.GetProcesses())
                    {
                        if (NativeMethods.IsWindowVisible(item.MainWindowHandle))
                        {
                            if (!String.IsNullOrEmpty(item.ProcessName) && !IntPtr.Equals(item.MainWindowHandle, IntPtr.Zero) && !Check_EX_Style(item.MainWindowHandle, 0x08000000L) && !Check_EX_Style(item.MainWindowHandle, 0x00200000L)
                                   && IsZoomedCustom(item.MainWindowHandle, Screen.PrimaryScreen.DeviceName) || NativeMethods.IsZoomed(item.MainWindowHandle))
                            {
                                //dirty fix, different explorer process being detected on desktop click wtf?!
                                if (item.ProcessName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) //|| item.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)
                                         || item.ProcessName.Equals("SystemSettings", StringComparison.OrdinalIgnoreCase) || item.ProcessName.Equals("MicrosoftEdgeCP", StringComparison.OrdinalIgnoreCase)
                                         || item.ProcessName.Equals("video.ui", StringComparison.OrdinalIgnoreCase) || item.ProcessName.Equals("NVIDIA Share", StringComparison.OrdinalIgnoreCase)
                                         || item.ProcessName.Equals("MicrosoftEdge", StringComparison.OrdinalIgnoreCase) || item.ProcessName.Equals("WindowsInternal.ComposableShell.Experiences.TextInput.InputApp", StringComparison.OrdinalIgnoreCase)
                                         )
                                    continue;

                                AllAppPauseFn(item);
                                //once a maximised/fullscreen window is found, exit(since only one screen, the only thing left to do is pause the wallpaper.)
                                return;
                            }
                        }
                    }
                    /*
                    visibleWindows.Clear();
                    NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc(UpdateVisibleWindows), IntPtr.Zero);
                    foreach (var item in visibleWindows)
                    {
                        try
                        {
                            NativeMethods.GetWindowThreadProcessId(item, out processID);
                            currProcess = Process.GetProcessById(processID);
                            currProcessName = currProcess.ProcessName;
                        }
                        catch
                        {
                            //ignore, admin process etc
                            continue;
                        }
                        AllAppPauseFn(currProcess);
                        //once a maximised/fullscreen window is found, exit(since only one screen, the only thing left to do is pause the wallpaper.)
                        return;
                    }
                    */

                    //if no fullscreen/maximised window is found, then check the foreground window for wallpaper unpausing, muting audio etc.
                    ForegroundPauseFn(); 
                }
            }
        }

        #region pause_all
        private static void AllAppPauseFn(Process proc, string display = null)
        {
            #region Exceptions & Fixes
            ProcessMonitorFixes();
            try
            {
                hWnd = proc.MainWindowHandle;

                if (currProcess.ProcessName.Equals("rainmeter", StringComparison.OrdinalIgnoreCase) || proc.ProcessName.Equals("emuhawk", StringComparison.OrdinalIgnoreCase) || proc.ProcessName.Equals("livelywpf", StringComparison.OrdinalIgnoreCase) ||
                        proc.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase) || proc.ProcessName.Equals("shellexperiencehost", StringComparison.OrdinalIgnoreCase) ||  //visual studio, notification tray etc
                        (proc.ProcessName.Equals("searchui", StringComparison.OrdinalIgnoreCase)) )  //startmenu search..

                {
                    hWnd = workerWOrig;
                }

                //application rule.
                for (int i = 0; i < SaveData.appRules.Count; i++)
                {
                    if (currProcess.ProcessName.Equals(SaveData.appRules[i].AppName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (SaveData.appRules[i].Rule == SaveData.AppRulesEnum.ignore) //always run when this process is foreground window, even when maximized
                        {
                            hWnd = workerWOrig; //assume desktop, to force unpause/wakeup of rePaper.
                            //Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            //return;
                        }
                        else if (SaveData.appRules[i].Rule == SaveData.AppRulesEnum.pause) //sleep
                        {
                            Pause.SuspendWallpaper(true);
                            return;
                        }
                        else if (SaveData.appRules[i].Rule == SaveData.AppRulesEnum.kill)
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    }
                }

            }
            catch 
            {
                //Debug.WriteLine(ex.Message + " " + ex.StackTrace);
                return;
            }
            #endregion

            if (hWnd != null && !hWnd.Equals(IntPtr.Zero))
            {
                desktopHandle = NativeMethods.GetDesktopWindow();
                shellHandle = NativeMethods.GetShellWindow();
                //Check we haven't picked up the desktop or the shell
                if (!(hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)))
                {
                    if (MainWindow.Multiscreen == false || SaveData.config.DisplayPauseSettings == SaveData.DisplayPauseEnum.all)
                    {
                        if (IntPtr.Equals(hWnd, workerWOrig)) //win10
                        {
                            Pause.ResumeWallpaper(true);
                        }
                        else if (IntPtr.Equals(hWnd, progman)) //win7
                        {
                            Pause.ResumeWallpaper(true);
                        }
                        else if (IntPtr.Equals(shell_tray, IntPtr.Zero) != true && IntPtr.Equals(hWnd, shell_tray) == true) //systrayhandle
                        {
                            Pause.ResumeWallpaper(false);
                        }
                        else if (NativeMethods.IsZoomed(hWnd)) // if window is maximised.
                        {
                            if (SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            }
                            else
                                Pause.SuspendWallpaper(true);
                            //Pause.SuspendWallpaper(true);
                        }
                        else if (IsZoomedCustom(hWnd, Screen.PrimaryScreen.DeviceName)) // isZoomed fails for games etc
                        {
                            if (SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            }
                            else
                                Pause.SuspendWallpaper(true);
                            //Pause.SuspendWallpaper(true);
                        }
                        else //window is not greater >90%
                        {
                            if (SaveData.config.AppFocusPause == SaveData.AppRulesEnum.pause)
                            {
                                Pause.SuspendWallpaper(true);
                            }
                            else
                            {
                                Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            }
                        }
                    }
                    else
                    {
                        currDisplay = display;
                        if (IntPtr.Equals(hWnd, workerWOrig)) //win10
                        {
                            Pause.ResumeWallpaper(true, currDisplay);
                        }
                        else if (IntPtr.Equals(hWnd, progman)) //win7
                        {
                            Pause.ResumeWallpaper( true, currDisplay);
                        }
                        else if (IntPtr.Equals(shell_tray, IntPtr.Zero) != true && IntPtr.Equals(hWnd, shell_tray) == true) //systrayhandle
                        {
                            Pause.ResumeWallpaper( false, currDisplay);
                        }
                        else if (NativeMethods.IsZoomed(hWnd)) // if window is maximised.
                        {
                            if (SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false, currDisplay); //resume with audio disabled etc
                            }
                            else
                                Pause.SuspendWallpaper(true, currDisplay);
                            //Pause.SuspendWallpaper(true, currDisplay);
                        }
                        else if (IsZoomedCustom(hWnd, currDisplay)) // isZoomed fails for games etc
                        {
                            if (SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false, currDisplay); //resume with audio disabled etc
                            }
                            else
                                Pause.SuspendWallpaper(true, currDisplay);
                            //Pause.SuspendWallpaper(true, currDisplay);
                        }
                        else //window is not greater >90%
                        {
                            if (SaveData.config.AppFocusPause == SaveData.AppRulesEnum.pause)
                            {
                                //Debug.WriteLine("ALLAPPFN:" + ">90%, appfocus = pause");
                                Pause.SuspendWallpaper(true, currDisplay);
                            }
                            else
                            {
                                //Debug.WriteLine("ALLAPPFN:" + ">90%, appfocus != pause");
                                Pause.SuspendWallpaper(false, currDisplay); //resume with audio disabled etc
                            }
                        }
                    }
                }
            }
        }
        #endregion pause_all

        #region pause_foreground
        private static void ForegroundPauseFn()//(IntPtr hWnd, string currProcessName)
        {
            
            hWnd = NativeMethods.GetForegroundWindow();
            try
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out processID);
                currProcess = Process.GetProcessById(processID);
                //currProcessName = currProcess.ProcessName;
            }
            catch 
            {
                Debug.WriteLine("getting processname failure, ignoring");
                //ignore, admin process etc
                return;
            }
            //Debug.WriteLine("FOREGROUND PROCESS:- " + currProcess.ProcessName);
            
            #region Exceptions & Fixes
            ProcessMonitorFixes();
            try
            {
                if (String.IsNullOrEmpty(currProcess.ProcessName))
                {
                    return;
                }

                for (int i = 0; i < SaveData.appRules.Count; i++)
                {
                    if (currProcess.ProcessName.Equals(SaveData.appRules[i].AppName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (SaveData.appRules[i].Rule == SaveData.AppRulesEnum.ignore) //always run when this process is foreground window, even when maximized
                        {
                            hWnd = workerWOrig; //assume desktop, to force unpause/wakeup of rePaper.
                            //Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            //return;
                        }
                        else if (SaveData.appRules[i].Rule == SaveData.AppRulesEnum.pause) //sleep
                        {
                            Pause.SuspendWallpaper(true);
                            return;
                        }
                        else if (SaveData.appRules[i].Rule == SaveData.AppRulesEnum.kill)
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    }
                }

                /*
                //todo: use classname instead of processname for checking exception cases.
                #region classname       
                int nRet;
                // Pre-allocate 256 characters, since this is the maximum class name length.
                StringBuilder ClassName = new StringBuilder(256);
                //Get the window class name
                nRet = NativeMethods.GetClassName(hWnd, ClassName, ClassName.Capacity);
                if (nRet != 0)
                {
                    Debug.WriteLine("classfetch success:" + ClassName.ToString());
                }
                else
                    Debug.WriteLine("classfetch failed");
                #endregion classname
                */

                if (currProcess.ProcessName.Equals("rainmeter", StringComparison.OrdinalIgnoreCase) || currProcess.ProcessName.Equals("emuhawk", StringComparison.OrdinalIgnoreCase) || currProcess.ProcessName.Equals("livelywpf", StringComparison.OrdinalIgnoreCase) ||
                        currProcess.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase) || currProcess.ProcessName.Equals("shellexperiencehost", StringComparison.OrdinalIgnoreCase) ||  //visual studio, notification tray etc
                        (currProcess.ProcessName.Equals("searchui", StringComparison.OrdinalIgnoreCase)) || currProcess.ProcessName.Equals("livelycefsharp", StringComparison.OrdinalIgnoreCase))  //startmenu search..
                {

                    hWnd = workerWOrig;
                }

            }
            catch 
            {
                //Debug.WriteLine(ex.Message + " " + ex.StackTrace);
                return;
            }

            #endregion Exceptions & Fixes

            if (hWnd != null && !hWnd.Equals(IntPtr.Zero))
            {
                desktopHandle = NativeMethods.GetDesktopWindow();
                shellHandle = NativeMethods.GetShellWindow();
                //Check we haven't picked up the desktop or the shell
                if (!(hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)))
                {                
                    if (MainWindow.Multiscreen == false || SaveData.config.DisplayPauseSettings == SaveData.DisplayPauseEnum.all) //pause all wp's when any window is maximised.
                            //|| (MainWindow.Multiscreen && SaveData.config.WallpaperArrangement == WallpaperArrangement.span) )//assuming single wp for span, so just pause "everything"
                    {
                        if (IntPtr.Equals(hWnd, workerWOrig)) //win10
                        {
                            Pause.ResumeWallpaper(true);
                        }
                        else if (IntPtr.Equals(hWnd, progman)) //win7
                        {
                            Pause.ResumeWallpaper(true);
                        }
                        else if (IntPtr.Equals(shell_tray, IntPtr.Zero) != true && IntPtr.Equals(hWnd, shell_tray) == true) //systrayhandle
                        {
                            Pause.ResumeWallpaper(false);
                        }
                        else if (NativeMethods.IsZoomed(hWnd)) // if window is maximised.
                        {
                            if(SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            }
                            else
                                Pause.SuspendWallpaper(true);
                        }
                        else if (IsZoomedCustom(hWnd, Screen.PrimaryScreen.DeviceName) )//&& !StaticPinvoke.IsIconic(hWnd)) // isZoomed fails for games etc
                        {
                            if (SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            }
                            else
                                Pause.SuspendWallpaper(true);
                        }
                        else //window is not greater >90%
                        {                            
                            if (SaveData.config.AppFocusPause == SaveData.AppRulesEnum.pause)
                            {
                                Pause.SuspendWallpaper(true);
                            }
                            else
                            {
                                Pause.SuspendWallpaper(false); //resume with audio disabled etc
                            }
                        }
                    }
                    else //multiscreen wp pause algorithm, for per-monitor pause rule.
                    {
                        if ((currDisplay = MapWindowToMonitor(hWnd)) != null)
                        {
                            //unpausing the rest of wp's, fix for limitation for this algorithm.
                            foreach (var item in Screen.AllScreens)
                            {
                                if(item.DeviceName != currDisplay)
                                    Pause.ResumeWallpaper(true, item.DeviceName);
                            }                          
                        }
                        else
                        {
                            //can happen if no display connected?!
                            return;
                        }

                        if (IntPtr.Equals(hWnd, workerWOrig)) //win10
                        {
                            Pause.ResumeWallpaper(true, currDisplay);
                        }
                        else if (IntPtr.Equals(hWnd, progman)) //win7
                        {
                            Pause.ResumeWallpaper(true, currDisplay);
                        }
                        else if (IntPtr.Equals(shell_tray, IntPtr.Zero) != true && IntPtr.Equals(hWnd, shell_tray) == true) //systrayhandle
                        {
                            Pause.ResumeWallpaper(false, currDisplay);
                        }
                        else if(SaveData.config.WallpaperArrangement == WallpaperArrangement.span)
                        {
                            if(IsZoomedSpan(hWnd))
                            {
                                Pause.SuspendWallpaper(true, Screen.PrimaryScreen.DeviceName);
                            }
                            else //window is not greater >90%
                            {
                                if (SaveData.config.AppFocusPause == SaveData.AppRulesEnum.pause)
                                {
                                    Pause.SuspendWallpaper(true, Screen.PrimaryScreen.DeviceName);
                                }
                                else
                                {
                                    Pause.SuspendWallpaper(false, Screen.PrimaryScreen.DeviceName); //resume with audio disabled etc
                                }
                            }
                        }
                        else if (NativeMethods.IsZoomed(hWnd)) // if window is maximised.
                        {
                            if (SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false, currDisplay);
                            }
                            else
                                Pause.SuspendWallpaper(true, currDisplay);

                        }
                        else if (IsZoomedCustom(hWnd, currDisplay)) // isZoomed fails for games etc which does not set the maximised flag? ( dota2 in windowed mode etc)
                        {
                            if (SaveData.config.AppFullscreenPause == SaveData.AppRulesEnum.ignore)
                            {
                                Pause.SuspendWallpaper(false, currDisplay);
                            }
                            else
                                Pause.SuspendWallpaper(true, currDisplay);

                        }
                        else //window is not greater >90%
                        {
                            if (SaveData.config.AppFocusPause == SaveData.AppRulesEnum.pause)
                            {
                                Pause.SuspendWallpaper(true, currDisplay);
                            }
                            else
                            {
                                Pause.SuspendWallpaper(false, currDisplay); //resume with audio disabled etc
                            }
                        }
                    }
                }
            }
        }
        #endregion pause_foreground

        //private const int GWL_EXSTYLE = -0x14;
        private static bool Check_EX_Style(IntPtr hw, long ex_style)//todo:- I think this is wrong, fix it.
        {
            var style = NativeMethods.GetWindowLongPtr(hw, (int)NativeMethods.GWL.GWL_EXSTYLE);
            if (((long)style & ex_style) >= 1)
                return true;
            else
                return false;
        }

        /// <summary>
        /// This fn checks if hWnd window size is >95% for the given display.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="display"></param>
        /// <returns>True if windowsize is greater.</returns>
        private static bool IsZoomedCustom(IntPtr hWnd, string display)
        {
            try
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out processID);
                currProcess = Process.GetProcessById(processID);
            }
            catch
            {

                Debug.WriteLine("getting processname failure, skipping isZoomedCustom()");
                //ignore, admin process etc
                return false;
            }

            NativeMethods.GetWindowRect(hWnd, out appBounds);
            screenBounds = System.Windows.Forms.Screen.FromHandle(hWnd).Bounds;
            if ((appBounds.Bottom - appBounds.Top) >= screenBounds.Height * .95f && (appBounds.Right - appBounds.Left) >= screenBounds.Width * .95f) // > if foreground app 95% working-area( - taskbar of monitor)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Checks if the hWnd dimension is spanned across all displays.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        private static bool IsZoomedSpan(IntPtr hWnd)
        {
            try
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out processID);
                currProcess = Process.GetProcessById(processID);
            }
            catch
            {

                Debug.WriteLine("getting processname failure, skipping isZoomedCustom()");
                //ignore, admin process etc
                return false;
            }

            NativeMethods.GetWindowRect(hWnd, out appBounds);
            //Debug.WriteLine("app:" + (appBounds.Bottom - appBounds.Top) +" " + (appBounds.Right - appBounds.Left) + "\nvirtual:" + SystemInformation.VirtualScreen.Height + " " + SystemInformation.VirtualScreen.Width);
            if ((appBounds.Bottom - appBounds.Top) >= SystemInformation.VirtualScreen.Height * .95f && (appBounds.Right - appBounds.Left) >= SystemInformation.VirtualScreen.Width * .95f) // > if foreground app 95% working-area( - taskbar of monitor)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Finds out which displaydevice the given application is residing.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static string MapWindowToMonitor(IntPtr handle)
        {
            try
            {
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                return screen.DeviceName;
            }
            catch
            {
                //what if there is no display connected? docs not listing anything weird.
                return null;
            }
            /*
            NativeMethods.MonitorInfoEx monitor = new NativeMethods.MonitorInfoEx();
            monitor.Init();
            //monitor
            IntPtr id = NativeMethods.MonitorFromWindow(handle, NativeMethods.MONITOR_DEFAULTTONULL);

            NativeMethods.GetMonitorInfo(id, ref monitor);
            //Debug.WriteLine("monitor:- " + monitor.DeviceName);
            return monitor.DeviceName;
            */
        }

        private static void ProcessMonitorFixes()
        {
            //todo: use hooks to check if the handles are closed, ie WM_CLOSE, WM_DESTROY msg ( cannot use NativeMethods.IsWindow() as it is unsafe/unreliable).

            if (IntPtr.Equals(workerWOrig, IntPtr.Zero))
            { 
                Logger.Info("searching workerWOrig..");
                var folderView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (folderView == IntPtr.Zero)
                {
                    //If the desktop isn't under Progman, cycle through the WorkerW handles and find the correct one
                    do
                    {
                        workerWOrig = NativeMethods.FindWindowEx(NativeMethods.GetDesktopWindow(), workerWOrig, "WorkerW", null);
                        folderView = NativeMethods.FindWindowEx(workerWOrig, IntPtr.Zero, "SHELLDLL_DefView", null);
                    } while (folderView == IntPtr.Zero && workerWOrig != IntPtr.Zero);
                }
            }

            if (IntPtr.Equals(shell_tray, IntPtr.Zero)) //start
            {
                Logger.Info("searching again shell_tray..");
                shell_tray = NativeMethods.FindWindow("Shell_TrayWnd", null);
            }
        }

        #region obsolete
        private static List<IntPtr> visibleWindows = new List<IntPtr>();
        [Obsolete("Using managed code instead, not EnumWindowProc")]
        private static bool IsWindowVisible(IntPtr hWnd, IntPtr lParam)
        {
            // Check if window is active/visible.
            if (NativeMethods.IsWindowVisible(hWnd))
            {
                if (!IntPtr.Equals(shellHandle, hWnd) && !NativeMethods.IsIconic(hWnd) && NativeMethods.GetWindowTextLength(hWnd) != 0
                                    && !IntPtr.Equals(hWnd, IntPtr.Zero) && NativeMethods.IsZoomed(hWnd) || IsZoomedCustomAllDisplays(hWnd))
                {
                    visibleWindows.Add(hWnd);
                }
            }
            return true;
        }

        [Obsolete("using managed code instead, call IsZoomedCustom()")]
        /// <summary>
        /// Checks if window is >95% in any of all  the displays connected.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        private static bool IsZoomedCustomAllDisplays(IntPtr hWnd)
        {
            foreach (var item in Screen.AllScreens)
            {
                if (IsZoomedCustom(hWnd, item.DeviceName))
                    return true;
            }
            return false;
        }

        [Obsolete("Don't remember when/why this was made lol")]
        private static IntPtr[] GetProcessWindows(int process)
        {
            IntPtr[] apRet = (new IntPtr[256]);
            int iCount = 0;
            IntPtr pLast = IntPtr.Zero;
            do
            {
                pLast = NativeMethods.FindWindowEx(IntPtr.Zero, pLast, null, null);
                int iProcess_;
                NativeMethods.GetWindowThreadProcessId(pLast, out iProcess_);
                if (iProcess_ == process) apRet[iCount++] = pLast;
            } while (pLast != IntPtr.Zero);
            System.Array.Resize(ref apRet, iCount);
            return apRet;
        }

        #endregion obsolete

        #endregion thread_monitor_pause/play

        #region wp_close_funtions
        /// <summary>
        /// Close wallpaper running on given monitor devicename.
        /// </summary>
        /// <param name="diplayDevice"></param>
        public static void CloseWallpaper(string diplayDevice)
        {
            dispatcherTimer.Stop();

            int i = 0;
            if ((i = SetupDesktop.wallpapers.FindIndex(x => x.DeviceName == diplayDevice)) != -1)
            {
                wallpapers.RemoveAt(i);
                SaveData.SaveWallpaperLayout();
            }

            if ( (i = mediakitPlayers.FindIndex( x => x.DisplayID == diplayDevice)) != -1)
            {
                mediakitPlayers[i].MP.StopPlayer();
                mediakitPlayers[i].MP.Close();
                //mediakitPlayers[i].mp = null;
                mediakitPlayers.RemoveAt(i);
                //return;
            }

            if ((i = wmPlayers.FindIndex(x => x.DisplayID == diplayDevice)) != -1)
            {
                wmPlayers[i].MP.StopPlayer();
                wmPlayers[i].MP.Close();
                //wmPlayers[i].mp = null;
                wmPlayers.RemoveAt(i);
                //return;
            }

            if ((i = gifWallpapers.FindIndex(x => x.DisplayID == diplayDevice)) != -1)
            {
                gifWallpapers[i].Gif.Close();
                //gifWallpapers[i].gif = null;
                gifWallpapers.RemoveAt(i);
                //return;
            }

            try
            {
                if ((i = extPrograms.FindIndex(x => x.DisplayID == diplayDevice)) != -1)
                {
                    if (extPrograms[i].Proc != null)
                    {
                        if (extPrograms[i].Proc.HasExited == false)
                        {
                            extPrograms[i].Proc.Kill();
                            extPrograms[i].Proc.Close();
                            //setup.proc.Dispose();
                            //extPrograms[i].proc = null;
                        }
                    }
                    extPrograms.RemoveAt(i);
                }
                //return;
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            try
            {
                if ((i = extVidPlayers.FindIndex(x => x.DisplayID == diplayDevice)) != -1)
                {
                    if (extVidPlayers[i].Proc != null)
                    {
                        if (extVidPlayers[i].Proc.HasExited == false)
                        {
                            extVidPlayers[i].Proc.Kill();
                            extVidPlayers[i].Proc.Close();
                            //setup.proc.Dispose();
                            //extPrograms[i].proc = null;
                        }
                    }
                    extVidPlayers.RemoveAt(i);
                }
                //return;
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            try
            {
                if ((i = webProcesses.FindIndex(x => x.DisplayID == diplayDevice)) != -1)
                {
                    webProcesses[i].Proc.OutputDataReceived -= WebProcess_OutputDataReceived;
                    //webProcesses[i].Proc.StandardInput.WriteLine("Terminate");
                    //webProcesses[i].Proc.Close(); //exit event is disposing it.

                    webProcesses[i].Proc.Refresh();
                    if (webProcesses[i].Proc != null)
                    {
                        if (webProcesses[i].Proc.HasExited == false)
                        {
                            webProcesses[i].Proc.Kill();
                            webProcesses[i].Proc.Close();
                        }
                    }
                    //MessageBox.Show(webProcesses[i].ToString());
                    webProcesses.RemoveAt(i);
                    //webProcesses.RemoveAt(i); //exit event is removing it.
                    //return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("web handle 0:- " + ex.Message + " " + ex.StackTrace);
            }

            dispatcherTimer.Start();
            RefreshDesktop();
        }

        /// <summary>
        /// Close all wp's of a particular type.
        /// </summary>
        /// <param name="type">wallpaper type.</param>       
        public static void CloseAllWallpapers(WallpaperType type)
        {
            dispatcherTimer.Stop();
            wallpapers.RemoveAll(x => x.Type == type);
            SaveData.SaveWallpaperLayout();

            if (type == WallpaperType.video)
            {
                var result = mediakitPlayers.FindAll(x => x.IsGif == false);
                foreach (var item in result)
                {
                    item.MP.StopPlayer();
                    item.MP.Close();
                    //item.mp = null;
                }
                //mediakitPlayers.Clear();
                mediakitPlayers.RemoveAll(x => x.IsGif == false);

                foreach (var item in wmPlayers)
                {
                    //Debug.WriteLine("Disposing windowsmediaplayer:- " + item.handle + " " + item.displayID);
                    item.MP.StopPlayer();
                    item.MP.Close();
                }
                wmPlayers.Clear();

                try
                {
                    foreach (var item in extVidPlayers)
                    {
                        if (item.Proc != null)
                        {
                            //Debug.WriteLine("currProces" + proc.ProcessName);
                            if (item.Proc.HasExited == false)
                            {
                                item.Proc.Kill();
                                item.Proc.Close(); //calls dispose also.
                                //setup.proc.Dispose();
                                //item.proc = null;
                            }
                        }
                    }
                    extVidPlayers.Clear();
                }
                catch (Exception e)
                {
                    Logger.Info("Disposeerror:- " + e.ToString());
                }
            }
            else if (type == WallpaperType.web || type == WallpaperType.url || type == WallpaperType.web_audio)
            {
                try
                {
                    foreach (var item in webProcesses)
                    {
                        item.Proc.OutputDataReceived -= WebProcess_OutputDataReceived;
                        //item.Proc.Exited -= WebProcess_Exited;
                        item.Proc.Refresh();
                        if (item.Proc != null)
                        {
                            if (item.Proc.HasExited == false)
                            {
                                item.Proc.Kill();
                                item.Proc.Close();
                            }
                        }
                    }
                    webProcesses.Clear();
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine(ex.ToString());
                    Logger.Info(ex.ToString());
                }
                /*
                var result = webProcesses.FindAll(x => x.Type == type);
                foreach (var item in result)
                {
                    try
                    {
                        item.Proc.OutputDataReceived -= WebProcess_OutputDataReceived;
                        //item.Proc.StandardInput.WriteLine("Terminate");
                        //item.Proc.Close(); //exit event is disposing it.
                        //webProcess = null;

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("web handle 0:- " + ex.Message + " " + ex.StackTrace);
                    }
                }
                //webProcesses.RemoveAll(x => x.Type == SetupDesktop.WallpaperType.web); //exit event is removing it.
                */
            }
            else if (type == WallpaperType.godot || type == WallpaperType.unity || type == WallpaperType.unity_audio 
                                                    || type == WallpaperType.app || type == WallpaperType.video_stream)
            {
                var result = extPrograms.FindAll(x => x.Type == type);
                try
                {
                    foreach (var item in extPrograms)
                    {
                        if (item.Proc != null)
                        {
                            //Debug.WriteLine("currProces" + proc.ProcessName);
                            if (item.Proc.HasExited == false)
                            {
                                item.Proc.Kill();
                                item.Proc.Close(); //calls dispose also.
                                //setup.proc.Dispose();
                                //item.proc = null;
                            }
                        }
                    }
                    extPrograms.RemoveAll(x => x.Type == type);
                }
                catch (Exception e)
                {
                    Logger.Info("Disposeerror:- "+ e.ToString());
                }
            }
            else if(type == WallpaperType.gif)
            {
                var result = mediakitPlayers.FindAll(x => x.IsGif == true);
                foreach (var item in result)
                {
                    item.MP.StopPlayer();
                    item.MP.Close();
                    //item.mp = null;
                }
                //mediakitPlayers.Clear();
                mediakitPlayers.RemoveAll(x => x.IsGif == true);

                foreach (var item in gifWallpapers)
                {
                    item.Gif.Close();
                    //item.gif = null;
                }
                gifWallpapers.Clear();
            }

            dispatcherTimer.Start();

            RefreshDesktop();
        }

        /// <summary>
        /// Closes all open wp's
        /// </summary>
        /// <param name="applicationExit">if false, clear disk savedata for wp layout</param>
        public static void CloseAllWallpapers(bool applicationExit = false)
        {
            var _timerStatus = dispatcherTimer.IsEnabled;
            if (_timerStatus)
                dispatcherTimer.Stop();

            if (!applicationExit)
            {
                wallpapers.Clear();
                SaveData.SaveWallpaperLayout();
            }

            if (bizhawkProc != null)
            {
                bizhawkProc.Kill();
            }

            try
            {
                foreach (var item in webProcesses)
                {
                    // todo:- Close the browser properly, using WM_CLOSE or IPC message instead.
                    /*
                    item.Proc.OutputDataReceived -= WebProcess_OutputDataReceived;
                    item.Proc.StandardInput.WriteLine("Terminate");
                    if (applicationExit)
                    {
                        item.Proc.Exited -= WebProcess_Exited;
                        item.Proc.WaitForExit(5000); //blocking thread.
                        if (!item.Proc.HasExited)
                            item.Proc.Kill();
                        item.Proc.Close();
                    }
                    */
                    item.Proc.OutputDataReceived -= WebProcess_OutputDataReceived;
                    //item.Proc.Exited -= WebProcess_Exited;
                    item.Proc.Refresh();
                    if (item.Proc != null)
                    {
                        if (item.Proc.HasExited == false)
                        {
                            item.Proc.Kill();
                            item.Proc.Close();
                        }
                    }
                }
                webProcesses.Clear();
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
                Logger.Info(ex.ToString());
            }

            try
            {
                foreach (var item in extPrograms)
                {
                    item.Proc.Refresh();
                    if (item.Proc != null)
                    {
                        if (item.Proc.HasExited == false)
                        {
                            item.Proc.Kill();
                            item.Proc.Close();
                            //setup.proc.Dispose();
                            //item.proc = null;
                        }
                    }
                }
                extPrograms.Clear();
            }
            catch(Exception e)
            {
                Logger.Info("Disposeerror:- " + e.ToString());
            }

            try
            {
                foreach (var item in extVidPlayers)
                {
                    item.Proc.Refresh();
                    if (item.Proc != null)
                    {
                        if (item.Proc.HasExited == false)
                        {
                            //item.Proc.CloseMainWindow();
                            item.Proc.Kill();
                            item.Proc.Close();
                            //setup.proc.Dispose();
                            //item.proc = null;
                        }
                    }
                }
                extVidPlayers.Clear();
            }
            catch (Exception e)
            {
                Logger.Info("Disposeerror:- " + e.ToString());
            }

            foreach (var item in mediakitPlayers)
            {
                item.MP.StopPlayer();
                item.MP.Close();
                //item.mp = null;
            }
            mediakitPlayers.Clear();

            foreach (var item in wmPlayers)
            {
                item.MP.StopPlayer();
                item.MP.Close();
            }
            wmPlayers.Clear();

            foreach (var item in gifWallpapers)
            {
                item.Gif.Close();
                //item.gif = null;            
            }
            gifWallpapers.Clear();


            //ext programs running
            SaveData.runningPrograms.Clear();
            SaveData.SaveRunningPrograms();

            if (_timerStatus && !applicationExit)
                dispatcherTimer.Start();

            RefreshDesktop();
        }
        #endregion

        #region everything_else

        public static void SendCustomiseMsgtoWallpaper(string displayDevice)
        {
            try
            {
                foreach (var item in webProcesses)
                {
                    if (item.Type == WallpaperType.url)
                        continue;

                    if (displayDevice.Equals(item.DisplayID, StringComparison.Ordinal))
                    {
                        item.Proc.StandardInput.WriteLine("lively-customise " + item.DisplayID);
                        break; //todo: decide what to do multiscreen
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
                Logger.Info(ex.ToString());
            }
        }

        public static void SendCustomiseMsgtoWallpaper2(string filePath)
        {
            try
            {
                foreach (var item in webProcesses)
                {
                    if (item.Type == WallpaperType.url)
                        continue;

                    if (filePath.Equals(item.FilePath, StringComparison.Ordinal))
                    {
                        item.Proc.StandardInput.WriteLine("lively-customise " + item.DisplayID);
                        break; 
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
                Logger.Info(ex.ToString());
            }
        }

        private static bool _timerInitilaized = false;
        /// <summary>
        /// Setup running Process monitor timer fn.
        /// </summary>
        public static void InitializeTimer()
        {        
            if (!_timerInitilaized)
            {
                _timerInitilaized = true;
                dispatcherTimer.Tick += new EventHandler(ProcessMonitor);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, SaveData.config.ProcessTimerInterval);
            }           
        }

        public static void RemoveWindowFromTaskbar(IntPtr handle)
        {
            var styleNewWindowExtended =
                   (Int64)NativeMethods.WindowStyles.WS_EX_NOACTIVATE
                   | (Int64)NativeMethods.WindowStyles.WS_EX_TOOLWINDOW;

            // update window styles
            NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (-20), (IntPtr)styleNewWindowExtended);
        }

        /// <summary>
        /// Removes window border & some menuitems. Won't remove everything in apps with custom UI system.
        /// Flags Credit: https://github.com/Codeusa/Borderless-Gaming
        /// If there is an issue with me using the flags just let me know I will remove it.
        /// </summary>
        /// <param name="handle"></param>
        private static void BorderlessWinStyle(IntPtr handle)
        {
            // Get window styles
            var styleCurrentWindowStandard = NativeMethods.GetWindowLongPtr(handle, (-16));
            var styleCurrentWindowExtended = NativeMethods.GetWindowLongPtr(handle, (-20));

            // Compute new styles (XOR of the inverse of all the bits to filter)
            var styleNewWindowStandard =
                              styleCurrentWindowStandard.ToInt64()
                              & ~(
                                    (Int64)NativeMethods.WindowStyles.WS_CAPTION // composite of Border and DialogFrame          
                                  | (Int64)NativeMethods.WindowStyles.WS_THICKFRAME
                                  | (Int64)NativeMethods.WindowStyles.WS_SYSMENU
                                  | (Int64)NativeMethods.WindowStyles.WS_MAXIMIZEBOX // same as TabStop
                                  | (Int64)NativeMethods.WindowStyles.WS_MINIMIZEBOX // same as Group
                              );


            var styleNewWindowExtended =
                styleCurrentWindowExtended.ToInt64()
                & ~(
                      (Int64)NativeMethods.WindowStyles.WS_EX_DLGMODALFRAME
                    | (Int64)NativeMethods.WindowStyles.WS_EX_COMPOSITED
                    | (Int64)NativeMethods.WindowStyles.WS_EX_WINDOWEDGE
                    | (Int64)NativeMethods.WindowStyles.WS_EX_CLIENTEDGE
                    | (Int64)NativeMethods.WindowStyles.WS_EX_LAYERED
                    | (Int64)NativeMethods.WindowStyles.WS_EX_STATICEDGE
                    | (Int64)NativeMethods.WindowStyles.WS_EX_TOOLWINDOW
                    | (Int64)NativeMethods.WindowStyles.WS_EX_APPWINDOW
                );

            // update window styles
            NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (-16), (IntPtr)styleNewWindowStandard);
            NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (-20), (IntPtr)styleNewWindowExtended);

            // remove the menu and menuitems and force a redraw
            var menuHandle = NativeMethods.GetMenu(handle);
            if (menuHandle != IntPtr.Zero)
            {
                var menuItemCount = NativeMethods.GetMenuItemCount(menuHandle);

                for (var i = 0; i < menuItemCount; i++)
                {
                    NativeMethods.RemoveMenu(menuHandle, 0, NativeMethods.MF_BYPOSITION | NativeMethods.MF_REMOVE);
                }
                NativeMethods.DrawMenuBar(handle);
            }
        }

        /// <summary>
        /// Force redraw desktop, clears wp persisting on screen even after close.
        /// </summary>
        public static void RefreshDesktop()
        {
            //todo:- right now I'm just telling windows to change wallpaper with a null value of zero size, there has to be a PROPER way to do this.
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, null, NativeMethods.SPIF_UPDATEINIFILE);
        }

        /// <summary>
        /// Adds the wp as child of spawned desktop-workerw window.
        /// </summary>
        /// <param name="windowHandle">handle of wp</param>
        public static void SetParentWorkerW(IntPtr windowHandle)
        {
            if (System.Environment.OSVersion.Version.Major == 6 && System.Environment.OSVersion.Version.Minor == 1) //windows 7
            {
                if(!workerw.Equals(progman)) //this should fix the win7 wp disappearing issue.
                    NativeMethods.ShowWindow(workerw, (uint)0);

                IntPtr ret = NativeMethods.SetParent(windowHandle, progman);
                if(ret.Equals(IntPtr.Zero))
                {
                    LogWin32Error("failed to set parent(win7),");
                }
                //workerw is assumed as progman in win7, this is untested with all fn's: addwallpaper(), wp pause, resize events.. (I don't have win7 system with me).
                workerw = progman;
            }
            else
            {
                IntPtr ret = NativeMethods.SetParent(windowHandle, workerw);
                if (ret.Equals(IntPtr.Zero))
                {
                    LogWin32Error("failed to set parent,");
                }
            }            
        }

        public static void SetParentSafe(IntPtr child, IntPtr parent)
        {
            IntPtr ret = NativeMethods.SetParent(child, parent);
            if (ret.Equals(IntPtr.Zero))
            {
                LogWin32Error("failed to set custom parent,");
            }
        }

        /// <summary>
        /// Sets the window as bottom-most, no-activate window.
        /// </summary>
        /// <param name="windowHandle"></param>
        private static void SetWindowBottomMost(IntPtr windowHandle)
        {
            NativeMethods.SetWindowPos(windowHandle, 1, 0, 0, 0, 0, 0x0002 | 0x0010 | 0x0001); // SWP_NOMOVE ,SWP_NOACTIVATE,SWP_NOSIZE & Bottom most.
        }

        /// <summary>
        /// Focus fix, otherwise when new applicaitons launch fullscreen wont giveup window handle once SetParent() is called.
        /// </summary>
        public static void SetFocus(bool focusLively = true)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                //IntPtr progman = NativeMethods.FindWindow("Progman", null);
                NativeMethods.SetForegroundWindow(progman); //change focus from the started window//application.
                NativeMethods.SetFocus(progman);

                IntPtr livelyWindow = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                if (!livelyWindow.Equals(IntPtr.Zero) && NativeMethods.IsWindowVisible(livelyWindow) && focusLively)  //todo:- not working for cefsharp wp launch, why?
                {
                    NativeMethods.SetForegroundWindow(livelyWindow);
                    NativeMethods.SetFocus(livelyWindow);
                }
            }));
     
        }

        public static void RestartWallpapers()
        {
            dispatcherTimer.Stop();
            var bcp = wallpapers.ToList();
            wallpapers.Clear();
            CloseAllWallpapers();

            foreach (var item in bcp)
            {
                SetupDesktop.SetWallpaper(new WallpaperLayout() { Arguments = item.Arguments, DeviceName = item.DeviceName, FilePath = item.FilePath, Type = item.Type }, false);
            }

            dispatcherTimer.Start();
        }

        public static void LogWin32Error(string msg = null)
        {
            //todo: throw win32 exception.
            int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            if (err != 0)
            {
                Logger.Error(msg + " HRESULT:" + err);
            }
        }

        /// <summary>
        /// Is foreground desktop.
        /// </summary>
        /// <returns></returns>
        public static bool IsDesktop()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            if (IntPtr.Equals(hWnd, workerWOrig))
            {
                return true;
            }
            else if (IntPtr.Equals(hWnd, progman))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion everything_else
    }
}
