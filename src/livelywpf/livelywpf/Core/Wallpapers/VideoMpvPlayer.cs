using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace livelywpf.Core
{
    /// <summary>
    /// Original mpv videoplayer application.
    /// <br>References:</br>
    /// <br> https://github.com/mpv-player/mpv/blob/master/DOCS/man/ipc.rst </br>
    /// <br> https://mpv.io/manual/master/  </br>
    /// </summary>
    public class VideoMpvPlayer : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        IntPtr HWND { get; set; }
        Process Proc { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task processWaitTask;
        private readonly int timeOut;
        private readonly string ipcServerName;
        JObject livelyPropertiesData;
        string LivelyPropertyCopy { get; set; }

        public VideoMpvPlayer(string path, LibraryModel model, LivelyScreen display,
            WallpaperScaler scaler = WallpaperScaler.fill, StreamQualitySuggestion streamQuality = StreamQualitySuggestion.Highest)
        {
            LivelyPropertyCopy = null;
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
                        var wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, screenNumber);
                        Directory.CreateDirectory(wpdataFolder);

                        LivelyPropertyCopy = Path.Combine(wpdataFolder, "LivelyProperties.json");
                        if (!File.Exists(LivelyPropertyCopy))
                            File.Copy(model.LivelyPropertyPath, LivelyPropertyCopy);

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

            if (LivelyPropertyCopy != null)
            {
                livelyPropertiesData = Cef.LivelyPropertiesJSON.LoadLivelyProperties(LivelyPropertyCopy);
            }

            var scalerArg = scaler switch
            {
                WallpaperScaler.none => "--video-unscaled=yes",
                WallpaperScaler.fill => "--keepaspect=no",
                WallpaperScaler.uniform => "--keepaspect=yes",
                WallpaperScaler.uniformFill => "--panscan=1.0",
                _ => "--keepaspect=no",
            };

            ipcServerName = "mpvsocket" + Path.GetRandomFileName();
            string cmdArgs = 
                "--volume=0 " +
                //alternative: --loop-file=inf
                "--loop-file " +
                //do not close after media end
                "--keep-open " +
                //always create gui window
                "--force-window=yes " +
                //open window at (-9999,0)
                "--geometry=-9999:0 " +
                //allow screensaver
                "--stop-screensaver=no " +
                //alternative: --input-ipc-server=\\.\pipe\
                "--input-ipc-server=" + ipcServerName + " " +
                //stretch algorithm
                scalerArg + " " +
                //integer scaler for sharpness
                (model.LivelyInfo.Type == WallpaperType.gif ? "--scale=nearest " : " ") +
                //gpu decode preference
                (Program.SettingsVM.Settings.VideoPlayerHwAccel ? "--hwdec=auto " : "--hwdec=no ") +
                //file, stream path
                (model.LivelyInfo.Type == WallpaperType.videostream ? Helpers.StreamHelper.YoutubeDLMpvArgGenerate(streamQuality, path) : "\"" + path + "\"");

            ProcessStartInfo start = new ProcessStartInfo
            {               
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "mpv.exe"),
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv"),
                Arguments = cmdArgs,
            };

            Process proc = new Process()
            {
                StartInfo = start,
            };

            this.Proc = proc;
            this.Model = model;
            this.Display = display;
            this.timeOut = 20000;
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
            return HWND;
        }

        public string GetLivelyPropertyCopyPath()
        {
            return LivelyPropertyCopy;
        }

        public Process GetProcess()
        {
            return Proc;
        }

        public LivelyScreen GetScreen()
        {
            return Display;
        }

        public LibraryModel GetWallpaperData()
        {
            return Model;
        }

        public WallpaperType GetWallpaperType()
        {
            return Model.LivelyInfo.Type;
        }

        public void Pause()
        {
            SendMessage("{\"command\":[\"set_property\",\"pause\",true]}\n");
        }

        public void Play()
        {
            SendMessage("{\"command\":[\"set_property\",\"pause\",false]}\n");
        }

        public void SetVolume(int volume)
        {
            SendMessage("{\"command\":[\"set_property\",\"volume\"," + volume + "]}\n");
        }

        public void Resume()
        {

        }

        public void SendMessage(string msg)
        {
            try
            {
                if (msg.Contains("lively:customise", StringComparison.OrdinalIgnoreCase))
                {
                    var lpMsg = msg.Split(' ');
                    if (lpMsg.Length < 4)
                        return;
                    msg = GetLivelyProperty(lpMsg[1], lpMsg[2], lpMsg[3]);
                }

                if (msg != null)
                {
                    Helpers.PipeClient.SendMessage(ipcServerName, new string[] { msg });
                }
            }
            catch { }
        }


        private string GetLivelyProperty(string uiElement, string objectName, string msg)
        {
            // TODO: 
            // Having trouble passing double without decimal to SetPropertyDouble
            // Test and see if what all Lively controls are required based on available options: https://mpv.io/manual/master/
            // Maybe block some commands? like a blacklist

            string result = null;
            if (uiElement.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(msg, out int value))
                {
                    //value string in items array is passed instead of index like in cef livelyproperties.
                    result = GetMpvJsonPropertyString("command", "set_property", objectName, (string)livelyPropertiesData[objectName]["items"][value]);
                }
            }
            else if (uiElement.Equals("slider", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(msg, out double value))
                {
                    result = GetMpvJsonPropertyString("command", "set_property", objectName, msg);
                }
            }
            else if (uiElement.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(msg, out bool value))
                {
                    result = GetMpvJsonPropertyString("command", "set_property", objectName, value);
                }
            }
            else if (uiElement.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                //restore button press.
                if (objectName.Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase))
                {
                    //load new file.
                    livelyPropertiesData = Cef.LivelyPropertiesJSON.LoadLivelyProperties(GetLivelyPropertyCopyPath());
                    //restore new property values.
                    SetLivelyProperty(livelyPropertiesData);
                }
                else
                {
                    //unused
                }
            }
            return result;
        }

        private void SetLivelyProperty(JObject livelyProperty)
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
                            msg = GetMpvJsonPropertyString("command", "set_property", item.Key, (string)item.Value["value"]);
                        }
                        else if (uiElement.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                        {
                            msg = GetMpvJsonPropertyString("command", "set_property", item.Key, (bool)item.Value["value"]);
                        }
                        else if (uiElement.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                        {
                            //value string in items array is passed instead of index like in cef livelyproperties.
                            msg = GetMpvJsonPropertyString("command", "set_property", item.Key, (string)item.Value["items"][(int)item.Value["value"]]);
                        }

                        if (msg != null)
                        {
                            Helpers.PipeClient.SendMessage(ipcServerName, new string[] { msg });
                        }
                    }
                }
            }
            catch 
            { 
                //todo
            }
        }

        private string GetMpvJsonPropertyString(string commandName, params object[] parameters)
        {
            var script = new StringBuilder();
            script.Append("{\"");
            script.Append(commandName);
            script.Append("\":[");
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

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public async void Show()
        {
            if (Proc != null)
            {
                try
                {
                    Proc.Exited += Proc_Exited;
                    Proc.Start();
                    processWaitTask = Task.Run(() => HWND = WaitForProcesWindow().Result, ctsProcessWait.Token);
                    await processWaitTask;
                    if (HWND.Equals(IntPtr.Zero))
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
                        WindowOperations.BorderlessWinStyle(HWND);
                        WindowOperations.RemoveWindowFromTaskbar(HWND);
                        //Program ready!
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                        {
                            Success = true,
                            Error = null,
                            Msg = null
                        });
                        //Restore livelyproperties.json settings
                        SetLivelyProperty(livelyPropertiesData);
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

        private void Proc_Exited(object sender, EventArgs e)
        {
            Proc.Dispose();
            SetupDesktop.RefreshDesktop();
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

        public void Stop()
        {

        }

        public void Terminate()
        {
            try
            {
                Proc.Kill();
                Proc.Dispose();
            }
            catch { }
            SetupDesktop.RefreshDesktop();
        }
    }
}
