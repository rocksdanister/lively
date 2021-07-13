using ImageMagick;
using livelywpf.Core.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace livelywpf.Core
{
    /// <summary>
    /// Mpv videoplayer application.
    /// <br>References:</br>
    /// <br> https://github.com/mpv-player/mpv/blob/master/DOCS/man/ipc.rst </br>
    /// <br> https://mpv.io/manual/master/  </br>
    /// </summary>
    public class VideoMpvPlayer : IWallpaper
    {
        /// <summary>
        /// Mpv player json ipc command.
        /// </summary>
        private class MpvCommand
        {
            [JsonProperty("command")]
            public List<object> Command { get; } = new List<object>();
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        private IntPtr hwnd;
        private readonly Process _process;
        private readonly LibraryModel model;
        private LivelyScreen display;
        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task processWaitTask;
        private readonly int timeOut;
        private readonly string ipcServerName;
        private bool _isVideoStopped;
        private JObject livelyPropertiesData;
        private readonly string livelyPropertyCopyPath;
        private static int globalCount;
        private readonly int uniqueId;
        private bool isLoaded;

        public VideoMpvPlayer(string path, LibraryModel model, LivelyScreen display,
            WallpaperScaler scaler = WallpaperScaler.fill, StreamQualitySuggestion streamQuality = StreamQualitySuggestion.Highest, bool onScreenControl = false)
        {
            livelyPropertyCopyPath = null;
            if (model.LivelyPropertyPath != null)
            {
                //customisable wallpaper, livelyproperty.json is present.
                var dataFolder = Path.Combine(Program.WallpaperDir, "SaveData", "wpdata");
                try
                {
                    //extract last digits of the Screen class DeviceName, eg: \\.\DISPLAY4 -> 4
                    var screenNumber = display.DeviceNumber;
                    if (screenNumber != null)
                    {
                        //Create a directory with the wp foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                        //Further modifications are done to the copy file.
                        string wpdataFolder = null;
                        switch (Program.SettingsVM.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, screenNumber);
                                break;
                            case WallpaperArrangement.span:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "span");
                                break;
                            case WallpaperArrangement.duplicate:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "duplicate");
                                break;
                        }
                        Directory.CreateDirectory(wpdataFolder);
                        //copy the original file if not found..
                        livelyPropertyCopyPath = Path.Combine(wpdataFolder, "LivelyProperties.json");
                        if (!File.Exists(livelyPropertyCopyPath))
                        {
                            File.Copy(model.LivelyPropertyPath, livelyPropertyCopyPath);
                        }
                    }
                    else
                    {
                        //todo: fallback, use the original file (restore feature disabled.)
                    }
                }
                catch
                {
                    //todo: fallback, use the original file (restore feature disabled.)
                }
            }

            if (livelyPropertyCopyPath != null)
            {
                livelyPropertiesData = Cef.LivelyPropertiesJSON.LoadLivelyProperties(livelyPropertyCopyPath);
            }

            var scalerArg = scaler switch
            {
                WallpaperScaler.none => "--video-unscaled=yes",
                WallpaperScaler.fill => "--keepaspect=no",
                WallpaperScaler.uniform => "--keepaspect=yes",
                WallpaperScaler.uniformFill => "--panscan=1.0",
                //WallpaperScaler.auto => "--keepaspect-window=no",
                _ => "--keepaspect=no",
            };
            ipcServerName = "mpvsocket" + Path.GetRandomFileName();

            StringBuilder cmdArgs = new StringBuilder();
            //startup volume will be 0
            cmdArgs.Append("--volume=0 ");
            //disable window decorations
            //cmdArgs.Append("--no-border ");
            //alternative: --loop-file=inf
            cmdArgs.Append("--loop-file ");
            //do not close after media end
            cmdArgs.Append("--keep-open ");
            //open window at (-9999,0)
            cmdArgs.Append("--geometry=-9999:0 ");
            //always create gui window
            cmdArgs.Append("--force-window=yes ");
            //don't move the window when clicking
            cmdArgs.Append("--no-window-dragging ");
            //don't hide cursor after sometime.
            cmdArgs.Append("--cursor-autohide=no ");
            //allow windows screensaver
            cmdArgs.Append("--stop-screensaver=no ");
            //disable mpv default (built-in) key bindings
            cmdArgs.Append("--input-default-bindings=no ");
            //video stretch algorithm
            cmdArgs.Append(scalerArg + " ");
            //on-screen-controller visibility
            cmdArgs.Append(!onScreenControl ? "--no-osc " : " ");
            //alternative: --input-ipc-server=\\.\pipe\
            cmdArgs.Append("--input-ipc-server=" + ipcServerName + " ");
            //integer scaler for sharpness
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.gif ? "--scale=nearest " : " ");
            //gpu decode preference
            cmdArgs.Append(Program.SettingsVM.Settings.VideoPlayerHwAccel ? "--hwdec=auto-safe " : "--hwdec=no ");
            //screenshot location, important read: https://mpv.io/manual/master/#pseudo-gui-mode
            cmdArgs.Append("--screenshot-template=" + "\"" + Path.Combine(Program.AppDataDir, "temp", ipcServerName) + "\" --screenshot-format=jpg ");
            //file or online video stream path
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.videostream ? Helpers.StreamHelper.YoutubeDLMpvArgGenerate(streamQuality, path) : "\"" + path + "\"");

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "mpv.exe"),
                UseShellExecute = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv"),
                Arguments = cmdArgs.ToString(),
            };

            Process _process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = start,
            };

            this._process = _process;
            this.model = model;
            this.display = display;
            this.timeOut = 20000;

            //for logging purpose
            uniqueId = globalCount++;
        }

        public async void Close()
        {
            TaskProcessWaitCancel();
            while (!IsProcessWaitDone())
            {
                await Task.Delay(1);
            }

            //Not reliable, app may refuse to close(open dialogue window.. etc)
            //Proc.CloseMainWindow();
            Terminate();
        }

        public IntPtr GetHWND()
        {
            return hwnd;
        }

        public IntPtr GetHWNDInput()
        {
            return IntPtr.Zero;
        }

        public string GetLivelyPropertyCopyPath()
        {
            return livelyPropertyCopyPath;
        }

        public Process GetProcess()
        {
            return _process;
        }

        public LivelyScreen GetScreen()
        {
            return display;
        }

        public LibraryModel GetWallpaperData()
        {
            return model;
        }

        public WallpaperType GetWallpaperType()
        {
            return model.LivelyInfo.Type;
        }

        public void Play()
        {
            if (_isVideoStopped)
            {
                _isVideoStopped = false;
                //is this always the correct channel for main video?
                SendMessage("{\"command\":[\"set_property\",\"vid\",1]}\n");
            }
            SendMessage("{\"command\":[\"set_property\",\"pause\",false]}\n");
        }

        public void Pause()
        {
            SendMessage("{\"command\":[\"set_property\",\"pause\",true]}\n");
        }

        public void Stop()
        {
            _isVideoStopped = true;
            //video=no disable video but audio can still be played,
            //which is useful for 'play audio only' option in the future.
            SendMessage("{\"command\":[\"set_property\",\"vid\",\"no\"]}\n");
            Pause();
        }

        public void SetVolume(int volume)
        {
            SendMessage("{\"command\":[\"set_property\",\"volume\"," + JsonConvert.SerializeObject(volume) + "]}\n");
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            if (GetWallpaperType() != WallpaperType.picture)
            {
                var posStr = JsonConvert.SerializeObject(pos);
                switch (type)
                {
                    case PlaybackPosType.absolutePercent:
                        SendMessage("{\"command\":[\"seek\"," + posStr + ",\"absolute-percent\"]}\n");
                        break;
                    case PlaybackPosType.relativePercent:
                        SendMessage("{\"command\":[\"seek\"," + posStr + ",\"relative-percent\"]}\n");
                        break;
                }
            }
        }

        public async Task ScreenCapture(string filePath)
        {
            if (GetWallpaperType() == WallpaperType.gif)
            {
                await Task.Run(() =>
                {
                    //read first frame of gif image
                    using var image = new MagickImage(GetWallpaperData().FilePath);
                    if (image.Width < 1920)
                    {
                        //if the image is too small then resize to min: 1080p using integer scaling for sharpness.
                        image.FilterType = FilterType.Point;
                        image.Thumbnail(new Percentage(100 * 1920 / image.Width));
                    }
                    image.Write(Path.GetExtension(filePath) != ".jpg" ? filePath + ".jpg" : filePath);
                });
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                var imgPath = Path.Combine(Program.AppDataDir, "temp", ipcServerName + ".jpg");
                //monitor directory for screenshot, mpv only outputs message before capturing screenshot..
                using var watcher = new FileSystemWatcher();
                watcher.Path = Path.Combine(Program.AppDataDir, "temp");
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = "*.jpg";
                watcher.Changed += (s, e) =>
                {
                    if (Path.GetFileName(e.FullPath) == Path.GetFileName(imgPath) && e.ChangeType == WatcherChangeTypes.Changed)
                    {
                        //I was unable to set screenshot template via ipc :/
                        File.Move(imgPath, Path.GetExtension(filePath) != ".jpg" ? filePath + ".jpg" : filePath, true);
                        tcs.SetResult(true);
                    }
                };
                watcher.EnableRaisingEvents = true;
                //timeout, cancel after interval..
                using var timer = new System.Windows.Forms.Timer()
                {
                    Enabled = true,
                    Interval = 10000, //10sec
                };
                timer.Tick += (s, e) =>
                {
                    //time elapsed..
                    tcs.SetResult(false);
                };
                //request mpv to take screenshot (default is jpg)..
                SendMessage("{\"command\":[\"screenshot\",\"video\"]}\n");
                await tcs.Task;
            }
        }

        private void SetPlaybackProperties(JObject livelyProperty)
        {
            try
            {
                string msg;
                foreach (var item in livelyProperty)
                {
                    string uiElement = item.Value["type"].ToString();
                    if (!uiElement.Equals("button", StringComparison.OrdinalIgnoreCase) && !uiElement.Equals("label", StringComparison.OrdinalIgnoreCase))
                    {
                        msg = null;
                        if (uiElement.Equals("slider", StringComparison.OrdinalIgnoreCase))
                        {
                            msg = GetMpvCommand("set_property", item.Key, (string)item.Value["value"]);
                        }
                        else if (uiElement.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                        {
                            msg = GetMpvCommand("set_property", item.Key, (bool)item.Value["value"]);
                        }

                        if (msg != null)
                        {
                            Helpers.PipeClient.SendMessage(ipcServerName, msg);
                        }
                    }
                }
            }
            catch 
            { 
                //todo
            }
        }

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public async void Show()
        {
            if (_process != null)
            {
                try
                {
                    _process.Exited += Proc_Exited;
                    _process.OutputDataReceived += Proc_OutputDataReceived;
                    _process.Start();
                    _process.BeginOutputReadLine();
                    processWaitTask = Task.Run(() => hwnd = WaitForProcesWindow().Result, ctsProcessWait.Token);
                    await processWaitTask;
                    if (hwnd.Equals(IntPtr.Zero))
                    {
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                        {
                            Success = false,
                            Error = new Exception(Properties.Resources.LivelyExceptionGeneral),
                            Msg = "Process window handle is zero."
                        });
                    }
                    else
                    {
                        WindowOperations.BorderlessWinStyle(hwnd);
                        WindowOperations.RemoveWindowFromTaskbar(hwnd);
                        //Program ready!
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                        {
                            Success = true,
                            Error = null,
                            Msg = null
                        });
                        //Restore livelyproperties.json settings
                        SetPlaybackProperties(livelyPropertiesData);
                        //Wait a bit for properties to apply.
                        //Todo: check ipc mgs and do this properly.
                        await Task.Delay(69);
                        isLoaded = true;
                    }
                }
                catch (OperationCanceledException e1)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                    {
                        Success = false,
                        Error = e1,
                        Msg = "Program wallpaper terminated early/user cancel."
                    });
                }
                catch (InvalidOperationException e2)
                {
                    //No GUI, program failed to enter idle state.
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                    {
                        Success = false,
                        Error = e2,
                        Msg = "Program wallpaper crashed/closed already!"
                    });
                }
                catch (Exception e3)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                    {
                        Success = false,
                        Error = e3,
                        Msg = ":("
                    });
                }
            }
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.Info($"Mpv{uniqueId}: {e.Data}");
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            _process.OutputDataReceived -= Proc_OutputDataReceived;
            _process?.Dispose();
            SetupDesktop.RefreshDesktop();
        }

        #region process task

        /// <summary>
        /// Function to search for window of spawned program.
        /// </summary>
        private async Task<IntPtr> WaitForProcesWindow()
        {
            if (_process == null)
            {
                return IntPtr.Zero;
            }

            _process.Refresh();
            //waiting for program messageloop to be ready (GUI is not guaranteed to be ready.)
            while (_process.WaitForInputIdle(-1) != true)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
            }

            IntPtr wHWND = IntPtr.Zero;
            //Find process window.
            for (int i = 0; i < timeOut && _process.HasExited == false; i++)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
                if (!IntPtr.Equals((wHWND = GetProcessWindow(_process, true)), IntPtr.Zero))
                    break;
                await Task.Delay(1);
            }
            return wHWND;
        }

        /// <summary>
        /// Retrieve window handle of process.
        /// </summary>
        /// <param name="proc">Process to search for.</param>
        /// <param name="win32Search">Use win32 method to find window.</param>
        /// <returns></returns>
        private IntPtr GetProcessWindow(Process proc, bool win32Search = false)
        {
            if (_process == null)
                return IntPtr.Zero;

            if (win32Search)
            {
                return FindWindowByProcessId(proc.Id);
            }
            else
            {
                proc.Refresh();
                //Issue(.net core) MainWindowHandle zero: https://github.com/dotnet/runtime/issues/32690
                return proc.MainWindowHandle;
            }
        }

        private IntPtr FindWindowByProcessId(int pid)
        {
            IntPtr HWND = IntPtr.Zero;
            NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                _ = NativeMethods.GetWindowThreadProcessId(tophandle, out int cur_pid);
                if (cur_pid == pid)
                {
                    if (NativeMethods.IsWindowVisible(tophandle))
                    {
                        HWND = tophandle;
                        return false;
                    }
                }

                return true;
            }), IntPtr.Zero);

            return HWND;
        }

        /// <summary>
        /// Cancel waiting for pgm wp window to be ready.
        /// </summary>
        private void TaskProcessWaitCancel()
        {
            if (ctsProcessWait == null)
                return;

            ctsProcessWait.Cancel();
            ctsProcessWait.Dispose();
        }

        /// <summary>
        /// Check if started pgm ready(GUI window started).
        /// </summary>
        /// <returns>true: process ready/halted, false: process still starting.</returns>
        private bool IsProcessWaitDone()
        {
            var task = processWaitTask;
            if (task != null)
            {
                if ((task.IsCompleted == false
                || task.Status == TaskStatus.Running
                || task.Status == TaskStatus.WaitingToRun
                || task.Status == TaskStatus.WaitingForActivation))
                {
                    return false;
                }
                return true;
            }
            return true;
        }

        #endregion process task

        public void Terminate()
        {
            try
            {
                _process.Kill();
            }
            catch { }
            SetupDesktop.RefreshDesktop();
        }

        public void SendMessage(string msg)
        {
            try
            {
                Helpers.PipeClient.SendMessage(ipcServerName, msg);
            }
            catch { }
        }

        public void SendMessage(IpcMessage obj)
        {
            // TODO: 
            // Test and see if what all Lively controls are required based on available options: https://mpv.io/manual/master/
            // Maybe block some commands? like a blacklist
            try
            {
                string msg = null;
                switch (obj.Type)
                {
                    case MessageType.lp_slider:
                        var sl = (LivelySlider)obj;
                        if ((sl.Step % 1) != 0)
                        {
                            msg = GetMpvCommand("set_property", sl.Name, sl.Value);
                        }
                        else
                        {
                            //mpv is strongly typed; sending decimal value for integer commands fails..
                            msg = GetMpvCommand("set_property", sl.Name, Convert.ToInt32(sl.Value));
                        }
                        break;
                    case MessageType.lp_chekbox:
                        var chk = (LivelyCheckbox)obj;
                        msg = GetMpvCommand("set_property", chk.Name, chk.Value);
                        break;
                    case MessageType.lp_button:
                        var btn = (LivelyButton)obj;
                        if (btn.IsDefault)
                        {
                            //load new file.
                            livelyPropertiesData = Cef.LivelyPropertiesJSON.LoadLivelyProperties(GetLivelyPropertyCopyPath());
                            //restore new property values.
                            SetPlaybackProperties(livelyPropertiesData);
                        }
                        else { } //unused
                        break;
                    case MessageType.lp_dropdown:
                        //todo
                        break;
                    case MessageType.lp_textbox:
                        //todo
                        break;
                    case MessageType.lp_cpicker:
                        //todo
                        break;
                    case MessageType.lp_fdropdown:
                        //todo
                        break;
                }

                if (msg != null)
                {
                    SendMessage(msg);
                }
            }
            catch (OverflowException)
            {
                Logger.Error("Mpv{0}: Slider double -> int overlow", uniqueId); 
            }
            catch { }
        }

        public bool IsLoaded()
        {
            return isLoaded;
        }

        #region mpv util

        /*                                      - 1 iteration -
         *|        Method     |     Mean |     Error |    StdDev |  Gen 0   | Gen 1 | Gen 2 | Allocated |
         *|------------------:|---------:|----------:|----------:|---------:|------:|------:|----------:|
         *| GetMpvCommand     | 1.493 us | 0.0085 us | 0.0080 us | 0.5741   |     - |     - |      2 KB |
         *| GetMpvCommandStrb | 1.551 us | 0.0148 us | 0.0138 us | 1.7033   |     - |     - |      5 KB |
         *                                      - 100 iteration -
         *|        Method     |     Mean |   Error   |  StdDev   |    Gen 0 |Gen 1  | Gen 2 | Allocated |
         *|------------------:|---------:|----------:|----------:|---------:|------:|------:|----------:|
         *| GetMpvCommand     | 188.4 μs | 0.51 μs   | 0.48 μs   |  61.2793 |   -   |     - |    188 KB |
         *| GetMpvCommandStrb | 163.2 μs | 2.15 μs   | 2.01 μs   | 101.8066 |   -   |     - |    312 KB |
         */

        /// <summary>
        /// Creates serialized mpv ipc json string.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string GetMpvCommand(params object[] parameters)
        {
            var obj = new MpvCommand();
            obj.Command.AddRange(parameters);
            return JsonConvert.SerializeObject(obj) + Environment.NewLine;
        }

        /// <summary>
        /// Creates serialized mpv ipc json string.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string GetMpvCommandStrb(params object[] parameters)
        {
            var script = new StringBuilder();
            script.Append("{\"command\":[");
            for (int i = 0; i < parameters.Length; i++)
            {
                script.Append(JsonConvert.SerializeObject(parameters[i]));
                if (i < parameters.Length - 1)
                {
                    script.Append(", ");
                }
            }
            script.Append("]}\n");
            return script.ToString();
        }

        #endregion //mpv util
    }
}
