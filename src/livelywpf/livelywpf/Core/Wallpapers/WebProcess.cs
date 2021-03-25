using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;

namespace livelywpf.Core
{
    public class WebProcess : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //todo: Check this library out https://github.com/Tyrrrz/CliWrap
        private IntPtr hwnd;
        private readonly Process _process;
        private readonly LibraryModel model;
        private LivelyScreen display;
        private readonly string livelyPropertyCopyPath;
        private bool _initialized;
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public WebProcess(string path, LibraryModel model, LivelyScreen display)
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
                        var wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, screenNumber);
                        Directory.CreateDirectory(wpdataFolder);

                        livelyPropertyCopyPath = Path.Combine(wpdataFolder, "LivelyProperties.json");
                        if (!File.Exists(livelyPropertyCopyPath))
                            File.Copy(model.LivelyPropertyPath, livelyPropertyCopyPath);
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

            StringBuilder cmdArgs = new StringBuilder();
            cmdArgs.Append(" --url " + "\"" + path + "\"");
            cmdArgs.Append(" --display " + "\"" + display + "\"");
            cmdArgs.Append(" --property " + "\"" + livelyPropertyCopyPath + "\"");
            cmdArgs.Append(" --volume " + Program.SettingsVM.Settings.AudioVolumeGlobal);
            cmdArgs.Append(" --geometry " + display.Bounds.Width + "x" + display.Bounds.Height);
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.webaudio ? " --audio true" : " --audio false");
            cmdArgs.Append(!string.IsNullOrWhiteSpace(model.LivelyInfo.Arguments) ? " " + model.LivelyInfo.Arguments : " ");
            cmdArgs.Append(!string.IsNullOrWhiteSpace(Program.SettingsVM.Settings.WebDebugPort) ? " --debug " + Program.SettingsVM.Settings.WebDebugPort : " ");
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.url || model.LivelyInfo.Type == WallpaperType.videostream ? " --type online" : " --type local");
            cmdArgs.Append(Program.SettingsVM.Settings.CefDiskCache && model.LivelyInfo.Type == WallpaperType.url ? " --cache " + "\"" + Path.Combine(Program.AppDataDir, "Cef", "cache", display.DeviceNumber) + "\"" : " ");

            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = cmdArgs.ToString(),
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "cef", "LivelyCefSharp.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "cef")
            };

            Process webProcess = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true
            };

            this._process = webProcess;
            this.model = model;
            this.display = display;
        }

        public void Close()
        {
            //Issue: Cef.shutdown() crashing when multiple instance is closed simulataneously.
            Terminate();
            /*
            try
            {
                Proc.Refresh();
                Proc.StandardInput.WriteLine("lively:terminate");
                //TODO: Make it Async function.
                if (!Proc.WaitForExit(4000))
                {
                    Terminate();
                }
            }
            catch
            {
                Terminate();
            }
            */
        }

        public IntPtr GetHWND()
        {
            return hwnd;
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

        public void Pause()
        {
            //minimize browser.
            NativeMethods.ShowWindow(GetHWND(), (uint)NativeMethods.SHOWWINDOW.SW_SHOWMINNOACTIVE);
            //SendMessage("lively-playback pause");
        }

        public void Play()
        {
            //show minimized browser.
            NativeMethods.ShowWindow(GetHWND(), (uint)NativeMethods.SHOWWINDOW.SW_SHOWNOACTIVATE);
            //SendMessage("lively-playback play");
            //WallpaperRectFix();
        }

        private void WallpaperRectFix()
        {
            if (VerifyWindowRect(GetHWND(), GetScreen()))
            {
                Logger.Info("Correcting wp rect!");
                if (!NativeMethods.SetWindowPos(GetHWND(), 1, 0, 0, GetScreen().Bounds.Width, GetScreen().Bounds.Height, 0 | 0x0010 | 0x0002))
                {
                    //todo: log
                }
            }
        }

        private static bool VerifyWindowRect(IntPtr hWnd, LivelyScreen screen)
        {
            try
            {
                System.Drawing.Rectangle screenBounds;
                NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT appBounds);
                screenBounds = System.Windows.Forms.Screen.FromHandle(hWnd).Bounds;
                return ((appBounds.Bottom - appBounds.Top) != screen.Bounds.Height || (appBounds.Right - appBounds.Left) != screen.Bounds.Width);
            }
            catch
            {
                return false;
            }
        }

        public void Show()
        {
            if (_process != null)
            {
                try
                {
                    _process.Exited += Proc_Exited;
                    _process.OutputDataReceived += Proc_OutputDataReceived;
                    _process.Start();
                    _process.BeginOutputReadLine();
                }
                catch (Exception e)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = "Failed to start process." });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                //Exited with no error and without even firing OutputDataReceived; probably some external factor.
                WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                {
                    Success = false,
                    Error = new Exception(Properties.Resources.LivelyExceptionGeneral),
                    Msg = "Process exited before giving HWND."
                });
            }
            _process.OutputDataReceived -= Proc_OutputDataReceived;
            _process.Dispose();
            SetupDesktop.RefreshDesktop();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!String.IsNullOrEmpty(e.Data))
            {
                if (e.Data.Contains("HWND"))
                {
                    bool status = true;
                    Exception error = null;
                    string msg = null;
                    try
                    {
                        msg = e.Data;
                        var handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
                        //note-handle: WindowsForms10.Window.8.app.0.141b42a_r9_ad1

                        //hidin other windows, no longer required since I'm doing it in cefsharp pgm itself.
                        NativeMethods.ShowWindow(GetProcess().MainWindowHandle, 0);

                        //WARNING:- If you put the whole cefsharp window, workerw crashes and refuses to start again on next startup!!, this is a workaround.
                        handle = NativeMethods.FindWindowEx(handle, IntPtr.Zero, "Chrome_WidgetWin_0", null);
                        //cefRenderWidget = StaticPinvoke.FindWindowEx(handle, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", null);
                        //cefIntermediate = StaticPinvoke.FindWindowEx(handle, IntPtr.Zero, "Intermediate D3D Window", null);

                        if (IntPtr.Equals(handle, IntPtr.Zero))//unlikely.
                        {
                            status = false;
                        }
                        hwnd = handle;
                    }
                    catch (Exception ex)
                    {
                        status = false;
                        error = ex;
                    }
                    finally
                    {
                        if (!_initialized)
                        {
                            WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
                        }
                        _initialized = true;
                    }
                }
                Logger.Info("CEF:" + e.Data);
            }
        }

        public void Stop()
        {
            //throw new NotImplementedException();
        }

        public void SendMessage(string msg)
        {
            if (_process != null)
            {
                try
                {
                    _process.StandardInput.WriteLine(msg);
                }
                catch { }
            }
        }

        public string GetLivelyPropertyCopyPath()
        {
            return livelyPropertyCopyPath;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public void Terminate()
        {
            try
            {
                _process.Kill();
                _process.Dispose();
            }
            catch { }
            SetupDesktop.RefreshDesktop();
        }

        public void Resume()
        {
            //throw new NotImplementedException();
        }

        public void SetVolume(int volume)
        {
            /*
            try
            {
                if (Proc != null)
                {
                    //VolumeMixer.SetApplicationVolume(Proc.Id, volume);
                    SetProcessAndChildrenVolume(Proc.Id, volume);
                }
            }
            catch { }
            */
        }

        private void SetProcessAndChildrenVolume(int pid, int volume)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                SetProcessAndChildrenVolume(Convert.ToInt32(mo["ProcessID"]), volume);
            }
            VolumeMixer.SetApplicationVolume(Process.GetProcessById(pid).Id, volume);
        }

        private void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }

            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            { /* process already exited */ }
        }

        public void SetPlaybackPos(float pos)
        {
            if (pos == 0)
            {
                SendMessage("lively:reload");
            }
        }
    }
}