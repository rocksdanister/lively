using ImageMagick;
using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers;
using Lively.Common.Helpers.IPC;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
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
        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task processWaitTask;
        private readonly int timeOut;
        private readonly string ipcServerName;
        private bool _isVideoStopped;
        private JObject livelyPropertiesData;
        private static int globalCount;
        private readonly int uniqueId;

        public string LivelyPropertyCopyPath { get; }

        public bool IsLoaded { get; private set; } = false;

        public Process Proc { get; }

        public WallpaperType Category => Model.LivelyInfo.Type;

        public ILibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => IntPtr.Zero;

        public IDisplayMonitor Screen { get; set; }

        public VideoMpvPlayer(string path,
            ILibraryModel model,
            IDisplayMonitor display,
            string livelyPropertyPath,
            WallpaperScaler scaler = WallpaperScaler.fill,
            bool hwAccel = true,
            bool onScreenControl = false,
            StreamQualitySuggestion streamQuality = StreamQualitySuggestion.Highest)
        {
            LivelyPropertyCopyPath = livelyPropertyPath;

            if (LivelyPropertyCopyPath != null)
            {
                try
                {
                    livelyPropertiesData = JsonUtil.ReadJObject(LivelyPropertyCopyPath);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
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
            var configDir = GetConfigDir();

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
            cmdArgs.Append(hwAccel ? "--hwdec=auto-safe " : "--hwdec=no ");
            //avoid global config file %APPDATA%\mpv\mpv.conf
            cmdArgs.Append(configDir is not null ? "--config-dir=" + "\"" + configDir + "\" " : "--no-config ");
            //screenshot location, important read: https://mpv.io/manual/master/#pseudo-gui-mode
            cmdArgs.Append("--screenshot-template=" + "\"" + Path.Combine(Constants.CommonPaths.TempDir, ipcServerName) + "\" --screenshot-format=jpg ");
            //file or online video stream path
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.videostream ? GetYtDlMpvArg(streamQuality, path) : "\"" + path + "\"");

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

            this.Proc = _process;
            this.Model = model;
            this.Screen = display;
            this.timeOut = 20000;

            //for logging purpose
            uniqueId = globalCount++;
        }

        private static string GetConfigDir()
        {
            //Priority list of configuration directories
            string[] dirs = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "portable_config"),
                Path.Combine(Constants.CommonPaths.TempVideoDir, "portable_config") 
            };
            return dirs.FirstOrDefault(x => Directory.Exists(x));
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

        public void SetMute(bool mute)
        {
            if (mute)
            {
                SendMessage("{\"command\":[\"set_property\",\"aid\",\"no\"]}\n");
            }
            else
            {
                //todo
            }
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            if (Category != WallpaperType.picture)
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
            if (Category == WallpaperType.gif)
            {
                await Task.Run(() =>
                {
                    //read first frame of gif image
                    using var image = new MagickImage(Model.FilePath);
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
                var imgPath = Path.Combine(Constants.CommonPaths.TempDir, ipcServerName + ".jpg");
                //monitor directory for screenshot, mpv only outputs message before capturing screenshot..
                using var watcher = new FileSystemWatcher();
                watcher.Path = Constants.CommonPaths.TempDir;
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
                            PipeClient.SendMessage(ipcServerName, msg);
                        }
                    }
                }
            }
            catch 
            { 
                //todo
            }
        }

        public async void Show()
        {
            if (Proc != null)
            {
                try
                {
                    Proc.Exited += Proc_Exited;
                    Proc.OutputDataReceived += Proc_OutputDataReceived;
                    Proc.Start();
                    Proc.BeginOutputReadLine();
                    processWaitTask = Task.Run((Func<IntPtr>)(() => this.Handle = WaitForProcesWindow().Result), ctsProcessWait.Token);
                    await processWaitTask;
                    if (Handle.Equals(IntPtr.Zero))
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
                        WindowOperations.BorderlessWinStyle(Handle);
                        WindowOperations.RemoveWindowFromTaskbar(Handle);
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
                        IsLoaded = true;
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
            Proc.OutputDataReceived -= Proc_OutputDataReceived;
            Proc?.Dispose();
            DesktopUtil.RefreshDesktop();
        }

        #region process task

        /// <summary>
        /// Function to search for window of spawned program.
        /// </summary>
        private async Task<IntPtr> WaitForProcesWindow()
        {
            if (Proc == null)
            {
                return IntPtr.Zero;
            }

            Proc.Refresh();
            //waiting for program messageloop to be ready (GUI is not guaranteed to be ready.)
            while (Proc.WaitForInputIdle(-1) != true)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
            }

            IntPtr wHWND = IntPtr.Zero;
            //Find process window.
            for (int i = 0; i < timeOut && Proc.HasExited == false; i++)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
                if (!IntPtr.Equals((wHWND = GetProcessWindow(Proc, true)), IntPtr.Zero))
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
            if (Proc == null)
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
                Proc.Kill();
            }
            catch { }
            DesktopUtil.RefreshDesktop();
        }

        private void SendMessage(string msg)
        {
            try
            {
                PipeClient.SendMessage(ipcServerName, msg);
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
                            try
                            {
                                //load new file.
                                livelyPropertiesData = JsonUtil.ReadJObject(LivelyPropertyCopyPath);
                                //restore new property values.
                                SetPlaybackProperties(livelyPropertiesData);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e.ToString());
                            }
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

        #region mpv util

        /*                                      - BenchmarkDotNet -
         *|        Method     |     Mean |     Error |    StdDev |  Gen 0   | Gen 1 | Gen 2 | Allocated |
         *|------------------:|---------:|----------:|----------:|---------:|------:|------:|----------:|
         *| GetMpvCommand     | 1.493 us | 0.0085 us | 0.0080 us | 0.5741   |     - |     - |      2 KB |
         *| GetMpvCommandStrb | 1.551 us | 0.0148 us | 0.0138 us | 1.7033   |     - |     - |      5 KB |
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

        private static string GetYtDlMpvArg(StreamQualitySuggestion qualitySuggestion, string link)
        {
            return link + qualitySuggestion switch
            {
                StreamQualitySuggestion.Lowest => " --ytdl-format=bestvideo[height<=144]+bestaudio/best",
                StreamQualitySuggestion.Low => " --ytdl-format=bestvideo[height<=240]+bestaudio/best",
                StreamQualitySuggestion.LowMedium => " --ytdl-format=bestvideo[height<=360]+bestaudio/best",
                StreamQualitySuggestion.Medium => " --ytdl-format=bestvideo[height<=480]+bestaudio/best",
                StreamQualitySuggestion.MediumHigh => " --ytdl-format=bestvideo[height<=720]+bestaudio/best",
                StreamQualitySuggestion.High => " --ytdl-format=bestvideo[height<=1080]+bestaudio/best",
                StreamQualitySuggestion.Highest => " --ytdl-format=bestvideo+bestaudio/best",
                _ => string.Empty,
            };
        }

        #endregion //mpv util
    }
}
