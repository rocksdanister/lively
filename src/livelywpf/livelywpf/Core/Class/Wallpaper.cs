using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using livelywpf.Model;
using NLog;

namespace livelywpf.Core
{
    #region interface

    public interface IWallpaper
    {
        WallpaperType GetWallpaperType();
        LibraryModel GetWallpaperData();
        IntPtr GetHWND();
        void SetHWND(IntPtr hwnd);
        Process GetProcess();
        void Show();
        void Pause();
        void Resume();
        void Play();
        void Stop();
        void Close();
        void Terminate();
        LivelyScreen GetScreen();
        void SetScreen(LivelyScreen display);
        void SendMessage(string msg);
        string GetLivelyPropertyCopyPath();
        event EventHandler<WindowInitializedArgs> WindowInitialized;
    }

    public class WindowInitializedArgs
    {
        public bool Success { get; set; }
        public Exception Error { get; set; }
        public string Msg { get; set; }
    }

    #endregion interface

    #region video players

    public class VideoPlayerVLC : IWallpaper
    {
        public VideoPlayerVLC(string filePath, LibraryModel model, LivelyScreen screen)
        {          
            Player = new VLCElement(filePath, model.LivelyInfo.Type == WallpaperType.videostream);
            this.Model = model;
            this.Display = screen;
        }

        IntPtr HWND { get; set; }
        VLCElement Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                Player.Close();
            }));
        }

        public IntPtr GetHWND()
        {
            return HWND;
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public Process GetProcess()
        {
            return null;
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
            Player.PausePlayer();
        }

        public void Play()
        {
            Player.PlayMedia();
        }

        public void SendMessage(string msg)
        {
            throw new NotImplementedException();
        }

        public void SetHWND(IntPtr hwnd)
        {
            HWND = hwnd;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public async void Show()
        {
            if (Player != null)
            {
                Player.Closed += Player_Closed;
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    Player.Show();
                }));
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            SetupDesktop.RefreshDesktop();
        }

        public void Stop()
        {
            Player.StopPlayer();
        }

        public void Terminate()
        {
            Close();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }
    }

    public class VideoPlayerMPV : IWallpaper
    {
        public VideoPlayerMPV(string filePath, LibraryModel model, LivelyScreen display)
        {
            Player = new MPVElement(filePath);
            this.Model = model;
            this.Display = display;
        }

        IntPtr HWND { get; set; }
        MPVElement Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public WallpaperType GetWallpaperType()
        {
            return Model.LivelyInfo.Type;
        }
        public LibraryModel GetWallpaperData()
        {
            return Model;
        }
        public IntPtr GetHWND()
        {
            return HWND;
        }
        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }
        public Process GetProcess()
        {
            return null;
        }
        public void Play()
        {
            Player.PlayMedia();
        }
        public void Pause()
        {
            Player.PausePlayer();
        }
        public void Stop()
        {
            Player.StopPlayer();
        }
        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                Player.Close();
            }));
        }

        public LivelyScreen GetScreen()
        {
            return Display;
        }

        public void Show()
        {
            if (Player != null)
            {
                Player.Closed += Player_Closed;
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            SetupDesktop.RefreshDesktop();
        }

        public void SendMessage(string msg)
        {
            throw new NotImplementedException();
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public void Terminate()
        {
            Close();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }
    }

    public class VideoPlayerWPF : IWallpaper
    {
        public VideoPlayerWPF(string filePath, LibraryModel model, LivelyScreen display)
        {
            Player = new MediaElementWPF(filePath);
            this.Model = model;
            this.Display = display;
        }

        IntPtr HWND { get; set; }
        MediaElementWPF Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public WallpaperType GetWallpaperType()
        {
            return WallpaperType.video;
        }
        public LibraryModel GetWallpaperData()
        {
            return Model;
        }
        public IntPtr GetHWND()
        {
            return HWND;
        }
        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }
        public Process GetProcess()
        {
            return null;
        }
        public void Play()
        {
            Player.PlayMedia();
        }
        public void Pause()
        {
            Player.PausePlayer();
        }
        public void Stop()
        {
            Player.StopPlayer();
        }
        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                Player.Close();
            }));
        }

        public LivelyScreen GetScreen()
        {
            return Display;
        }

        public void Show()
        {
            if(Player != null)
            {
                Player.Closed += Player_Closed;
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            SetupDesktop.RefreshDesktop();
        }

        public void SendMessage(string msg)
        {
            throw new NotImplementedException();
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public void Terminate()
        {
            Close();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }
    }

    public class VideoPlayerVLCExt : IWallpaper
    {
        public VideoPlayerVLCExt(string path, LibraryModel model, LivelyScreen display)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = "\"" + path + "\"",
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer", "libVLCPlayer.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer")
            };

            Process videoPlayerProc = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true
            };
            //webProcess.OutputDataReceived += WebProcess_OutputDataReceived;

            this.Proc = videoPlayerProc;
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
        //string LivelyPropertyCopy { get; set; }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {
            try
            {
                Proc.Refresh();
                Proc.StandardInput.WriteLine("lively:terminate");
                Proc.OutputDataReceived -= Proc_OutputDataReceived;
            }
            catch
            {
                try
                {
                    //force terminate.
                    Proc.Kill();
                    Proc.Close();
                    SetupDesktop.RefreshDesktop();
                }
                catch { }
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
            SendMessage("lively:vid-pause");
        }

        public void Play()
        {
            SendMessage("lively:vid-play");
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
                catch (Exception e)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = null });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Proc.Close();
            SetupDesktop.RefreshDesktop();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            bool status = true, initialized = false;
            Exception error = null;
            string msg = null;
            try
            {
                IntPtr handle = new IntPtr();
                //Retrieves the windowhandle of cefsubprocess, cefsharp is launching cef as a separate proces..
                //If you add the full pgm as child of workerw then there are problems (prob related sharing input queue)
                //Instead hiding the pgm window & adding cefrender window instead.
                msg = "libVLCPlayer Handle:" + e.Data;
                if (e.Data.Contains("HWND"))
                {
                    handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
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

            if (!initialized)
            {
                initialized = true;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
            }
            Proc.OutputDataReceived -= Proc_OutputDataReceived;
        }

        public void Stop()
        {
            throw new NotImplementedException();
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
            return null;
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
            throw new NotImplementedException();
        }
    }

    #endregion video players

    #region gif players

    public class GIFPlayerUWP : IWallpaper
    {
        public GIFPlayerUWP(string filePath, LibraryModel model, LivelyScreen display)
        {
            Player = new GIFViewUWP(filePath);
            this.Model = model;
            this.Display = display;
        }
        IntPtr HWND { get; set; }
        GIFViewUWP Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                Player.Close();
            }));
        }

        public IntPtr GetHWND()
        {
            return HWND;
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public Process GetProcess()
        {
            return null;
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
            return WallpaperType.gif;
        }

        public void Pause()
        {
            Player.Stop();
        }

        public void Play()
        {
            Player.Play();
        }

        public void SendMessage(string msg)
        {
            throw new NotImplementedException();
        }

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public void Show()
        {
            if(Player != null)
            {
                Player.Closed += Player_Closed;
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            SetupDesktop.RefreshDesktop();
        }

        public void Stop()
        {
            Player.Stop();
        }

        public void Terminate()
        {
            Close();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }
    }

    #endregion gif players

    #region web browsers

    public class WebProcess : IWallpaper
    {
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

            string cmdArgs;
            if (model.LivelyInfo.Type == WallpaperType.web)
            {
                //Fail to send empty string as arg; "debug" is set as optional variable in cmdline parser library.
                if (string.IsNullOrWhiteSpace(Program.SettingsVM.Settings.WebDebugPort))
                {
                    cmdArgs = "--url " + "\"" + path + "\"" + " --type local" + " --display " + "\"" + display + "\"" +
                        " --property " + "\"" + LivelyPropertyCopy + "\"";
                }
                else
                {
                    cmdArgs = "--url " + "\"" + path + "\"" + " --type local" + " --display " + "\"" + display + "\"" +
                      " --property " + "\"" + LivelyPropertyCopy + "\"" + " --debug " + Program.SettingsVM.Settings.WebDebugPort;
                }
            }
            else if (model.LivelyInfo.Type == WallpaperType.webaudio)
            {
                if (string.IsNullOrWhiteSpace(Program.SettingsVM.Settings.WebDebugPort))
                {
                    cmdArgs = "--url " + "\"" + path + "\"" + " --type local" + " --display " + "\"" + display + "\"" + " --audio true" +
                          " --property " + "\"" + LivelyPropertyCopy + "\"";
                }
                else
                {
                    cmdArgs = "--url " + "\"" + path + "\"" + " --type local" + " --display " + "\"" + display + "\"" + " --audio true" +
                        " --property " + "\"" + LivelyPropertyCopy + "\"" + " --debug " + Program.SettingsVM.Settings.WebDebugPort;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Program.SettingsVM.Settings.WebDebugPort))
                {
                    cmdArgs = "--url " + "\"" + path + "\"" + " --type online" + " --display " + "\"" + display + "\"";
                }
                else
                {
                    cmdArgs = "--url " + "\"" + path + "\"" + " --type online" + " --display " + "\"" + display + "\"" +
                       " --debug " + Program.SettingsVM.Settings.WebDebugPort;
                }
            }

            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = cmdArgs,
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "cef", "LivelyCefSharp.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "cef")
            };

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

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {
            try
            {
                Proc.Refresh();
                Proc.StandardInput.WriteLine("lively:terminate");
                Proc.OutputDataReceived -= Proc_OutputDataReceived;
            }
            catch {
                try
                {
                    //force terminate.
                    Proc.Kill();
                    Proc.Close();
                    SetupDesktop.RefreshDesktop();
                }
                catch { }
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
            Proc.Close();
            SetupDesktop.RefreshDesktop();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            bool status = true, initialized = false;
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

            if (!initialized)
            {
                initialized = true;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }

    #endregion web browsers

    #region program wallpapers

    public class ExtPrograms : IWallpaper
    {
        public ExtPrograms(string path, LibraryModel model, LivelyScreen display)
        {
            string cmdArgs;
            if (model.LivelyInfo.Type == WallpaperType.unity)
            {
                //-popupwindow removes from taskbar
                //-fullscreen disable fullscreen mode if set during compilation (lively is handling resizing window instead).
                //Alternative flags:
                //Unity attaches to workerw by itself; Problem: Process window handle is returning zero.
                //"-parentHWND " + workerw.ToString();// + " -popupwindow" + " -;
                cmdArgs = "-popupwindow -screen-fullscreen 0";
            }
            else
            {
                cmdArgs = model.LivelyInfo.Arguments;
            }

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                WorkingDirectory = System.IO.Path.GetDirectoryName(path),
                Arguments = cmdArgs,
            };

            Process proc = new Process()
            {
                StartInfo = start,
            };

            this.Proc = proc;
            this.Model = model;
            this.Display = display;
            SuspendCnt = 0;
        }
        IntPtr HWND { get; set; }
        Process Proc { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        public UInt32 SuspendCnt { get; set; }
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task processWaitTask;

        public async void Close()
        {
            TaskProcessWaitCancel();
            while(!IsProcessWaitDone())
            {
                await Task.Delay(1);
            }

            try
            {
                //Not reliable, app may refuse to close(dialogue window visible etc)
                //Proc.CloseMainWindow();
                Proc.Refresh();
                Proc.Kill();
                Proc.Close();
            }
            catch { }
        }

        public IntPtr GetHWND()
        {
            return HWND;
        }

        public Process GetProcess()
        {
            return Proc;
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
            try
            {
                ProcessSuspend.SuspendAllThreads(this);
                //thread buggy noise otherwise?!
                VolumeMixer.SetApplicationMute(Proc.Id, true);
            }
            catch { }
        }

        public void Play()
        {
            try
            {
                ProcessSuspend.ResumeAllThreads(this);
                //thread buggy noise otherwise?!
                VolumeMixer.SetApplicationMute(Proc.Id, false);
            }
            catch { }
        }

        public void SetHWND(IntPtr hwnd)
        {
            HWND = hwnd;
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public LivelyScreen GetScreen()
        {
            return Display;
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
                    if(HWND.Equals(IntPtr.Zero))
                    {
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = null, Msg = "Pgm handle is zero!" });
                    }
                    else
                    {
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null, Msg = null });
                    }
                }
                catch(OperationCanceledException e1)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e1, Msg = "Pgm terminated early/user cancel!" });
                    Close();
                }
                catch(InvalidOperationException e2)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e2, Msg = "Pgm crashed/closed already!" });
                    Close();
                }
                catch (Exception e3)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e3, Msg = null });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Proc.Close();
            SetupDesktop.RefreshDesktop();
        }

        #region process task

        //Issue(.net core) window handle zero: https://github.com/dotnet/runtime/issues/32690
        /// <summary>
        /// Function to search for window of spawned program.
        /// </summary>
        private async Task<IntPtr> WaitForProcesWindow()
        {
            if (Proc == null)
            {
                return IntPtr.Zero;
            }

            IntPtr configW = IntPtr.Zero;
            int i = 0;
            try
            {
                Proc.Refresh();
                //waiting for msgloop to be ready, gui not guaranteed to be ready!.
                while (Proc.WaitForInputIdle(-1) != true) 
                {
                    ctsProcessWait.Token.ThrowIfCancellationRequested();
                }
            }
            catch (InvalidOperationException)
            {
                //no gui, failed to enter idle state.
                throw new OperationCanceledException();
            }

            if (GetWallpaperType() == WallpaperType.godot)
            {
                while (i < Program.SettingsVM.Settings.WallpaperWaitTime && Proc.HasExited == false)
                {
                    i++;
                    configW = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Engine", null);
                    if (!IntPtr.Equals(configW, IntPtr.Zero))
                        break;
                    await Task.Delay(1);
                }
                return configW;
            }
            else if (GetWallpaperType() == WallpaperType.unity)
            {
                i = 0;
                //Player settings dialog of Unity, simulating play button click or search workerw if paramter given in argument.
                while (i < Program.SettingsVM.Settings.WallpaperWaitTime && Proc.HasExited == false)
                {
                    ctsProcessWait.Token.ThrowIfCancellationRequested();
                    i++;
                    if (!IntPtr.Equals(Proc.MainWindowHandle, IntPtr.Zero))
                        break;
                    await Task.Delay(1);
                }
                configW = NativeMethods.FindWindowEx(Proc.MainWindowHandle, IntPtr.Zero, "Button", "Play!");
                if (!IntPtr.Equals(configW, IntPtr.Zero))
                {
                    //simulate Play! button click. (Unity config window)
                    NativeMethods.SendMessage(configW, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                }

                await Task.Delay(1);
            }
            Proc.Refresh(); //update window-handle of unity config

            i = 0;
            //there does not seem to be a "proper" way to check whether mainwindow is ready.
            while (i < Program.SettingsVM.Settings.WallpaperWaitTime && Proc.HasExited == false)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
                i++;
                if (!IntPtr.Equals(Proc.MainWindowHandle, IntPtr.Zero))
                {
                    //moving the window out of screen.
                    //StaticPinvoke.SetWindowPos(proc.MainWindowHandle, 1, -20000, 0, 0, 0, 0x0010 | 0x0001); 
                    break;
                }
                await Task.Delay(1);
            }

            Proc.Refresh();
            if (Proc.MainWindowHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            else
                return Proc.MainWindowHandle;
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
                if((task.IsCompleted == false
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

        public void SendMessage(string msg)
        {
            throw new NotImplementedException();
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
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
            throw new NotImplementedException();
        }
    }

    #endregion progarm wallpapers
}
