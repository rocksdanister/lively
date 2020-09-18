using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;

namespace livelywpf.Core
{
    public class WebProcess : IWallpaper
    {
        //todo: Check this library out https://github.com/Tyrrrz/CliWrap
        public WebProcess(string path, LibraryModel model, LivelyScreen display)
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

            StringBuilder cmdArgs = new StringBuilder();
            cmdArgs.Append("--url " + "\"" + path + "\"" + " --display " + "\"" + display + "\"");
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.url ? " --type online" : " --type local" + " --property " + "\"" + LivelyPropertyCopy + "\"");
            //Fail to send empty string as arg; "debug" is set as optional variable in cmdline parser library.
            if (!string.IsNullOrWhiteSpace(Program.SettingsVM.Settings.WebDebugPort))
            {
                cmdArgs.Append(" --debug " + Program.SettingsVM.Settings.WebDebugPort);
            }

            //Disk cache is only needed for online websites (if enabled.)
            if (Program.SettingsVM.Settings.CefDiskCache && model.LivelyInfo.Type == WallpaperType.url)
            {
                cmdArgs.Append(" --cache " + "\"" + Path.Combine(Program.AppDataDir, "Cef", "cache", display.DeviceNumber) + "\"");
            }

            if (model.LivelyInfo.Type == WallpaperType.webaudio)
            {
                cmdArgs.Append(" --audio true");
            }

            if(!String.IsNullOrWhiteSpace(model.LivelyInfo.Arguments))
            {
                cmdArgs.Append(" " + model.LivelyInfo.Arguments);
            }

            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = cmdArgs.ToString(),
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "cef", "LivelyCefSharp.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "cef")
            };
            cmdArgs.Clear();

            Process webProcess = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true
            };
            //webProcess.OutputDataReceived += WebProcess_OutputDataReceived;

            this.Proc = webProcess;
            this.Model = model;
            this.Display = display;
        }
        IntPtr HWND { get; set; }
        Process Proc { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        /// <summary>
        /// copy of LivelyProperties.json file used to modify for current running screen.
        /// </summary>
        string LivelyPropertyCopy { get; set; }

        private bool Initialized { get; set; }
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {    
            try
            {
                Proc.Refresh();
                Proc.OutputDataReceived -= Proc_OutputDataReceived;
                Proc.StandardInput.WriteLine("lively:terminate");
                //Issue: Cef.shutdown() crashing when simultaneously closed.
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
            
        }

        public IntPtr GetHWND()
        {
            return HWND;
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
            //minimize browser.
            NativeMethods.ShowWindow(HWND, 6); 
            //SendMessage("lively-playback pause");
        }

        public void Play()
        {
            NativeMethods.ShowWindow(HWND, 1); //normal
            NativeMethods.ShowWindow(HWND, 5); //show
            //SendMessage("lively-playback play");
        }

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }

        public void Show()
        {
            if (Proc != null)
            {
                try
                {
                    Proc.Exited += Proc_Exited;
                    Proc.OutputDataReceived += Proc_OutputDataReceived;
                    Proc.Start();
                    Proc.BeginOutputReadLine();
                }
                catch(Exception e) 
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = null });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {       
            if(!Initialized)
            {
                //Exited with no error and without even firing OutputDataReceived; probably some external factor.
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = null, Msg = "Pgm exited early!" });
            }
            Proc.Close();
            SetupDesktop.RefreshDesktop();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            bool status = true;
            Exception error = null;
            string msg = null;
            try
            {
                IntPtr handle = new IntPtr();
                //Retrieves the windowhandle of cefsubprocess, cefsharp is launching cef as a separate proces..
                //If you add the full pgm as child of workerw then there are problems (prob related sharing input queue)
                //Instead hiding the pgm window & adding cefrender window instead.
                msg = "Cefsharp Handle:" + e.Data;
                if (e.Data.Contains("HWND"))
                {
                    handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
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
                    SetHWND(handle);
                }
            }
            catch (Exception ex)
            {
                status = false;
                error = ex;
            }
            finally
            {
                /*
                if(status)
                {
                    NativeMethods.SetWindowPos(GetHWND(), 1, -9999, 0, 0, 0,
                        (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE | (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE);
                }
                */
            }
            Initialized = true;
            WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
        }

        public void Stop()
        {
            //throw new NotImplementedException();
        }

        public void SendMessage(string msg)
        {
            if (Proc != null)
            {
                try
                {
                    Proc.StandardInput.WriteLine(msg);
                }
                catch { }
            }
        }

        public string GetLivelyPropertyCopyPath()
        {
            return LivelyPropertyCopy;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public void Terminate()
        {
            try
            {
                Proc.Kill();
                Proc.Close();
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
    }
}
