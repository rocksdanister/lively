using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;
using livelywpf.Model;

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
        void Play();
        void Stop();
        void Close();
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
            throw new NotImplementedException();
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

        public void Show()
        {
            if (Player != null)
            {
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        public void Stop()
        {
            Player.StopPlayer();
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
            throw new NotImplementedException();
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
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
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
            throw new NotImplementedException();
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
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
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
            throw new NotImplementedException();
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
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        public void Stop()
        {
            Player.Stop();
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
                Proc.StandardInput.WriteLine("lively:terminate");
                Proc.Close();
            }
            catch {

                try
                {
                    //force terminate.
                    Proc.Kill();
                    Proc.Close();
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
                    Proc.OutputDataReceived += Proc_OutputDataReceived;
                    Proc.Start();
                    Proc.BeginOutputReadLine();
                }
                catch(Exception e) 
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = null });
                }
            }
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
    }

    #endregion web browsers

    #region program wallpapers
    //todo
    public class ExtPrograms : IWallpaper
    {
        public ExtPrograms(Process proc, IntPtr hwnd, LibraryModel model, LivelyScreen display)
        {
            this.HWND = hwnd;
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

        public void Close()
        {
            try
            {
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

        public void Show()
        {
            throw new NotImplementedException();
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
    }

    #endregion progarm wallpapers
}
