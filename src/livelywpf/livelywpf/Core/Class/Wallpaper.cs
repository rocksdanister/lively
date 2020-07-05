using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

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
        Screen GetScreen();
    }

    #endregion interface

    #region video players

    public class VideoPlayerMPV : IWallpaper
    {
        public VideoPlayerMPV(string filePath, LibraryModel model, Screen display)
        {
            Player = new MPVElement(filePath);
            this.Model = model;
            this.Display = display;
        }

        IntPtr HWND { get; set; }
        MPVElement Player { get; set; }
        LibraryModel Model { get; set; }
        Screen Display { get; set; }
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

        public Screen GetScreen()
        {
            return Display;
        }

        public void Show()
        {
            if (Player != null)
            {
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
            }
        }
    }

    public class VideoPlayerWPF : IWallpaper
    {
        public VideoPlayerWPF(string filePath, LibraryModel model, Screen display)
        {
            Player = new MediaElementWPF(filePath);
            this.Model = model;
            this.Display = display;
        }

        IntPtr HWND { get; set; }
        MediaElementWPF Player { get; set; }
        LibraryModel Model { get; set; }
        Screen Display { get; set; }
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

        public Screen GetScreen()
        {
            return Display;
        }

        public void Show()
        {
            if(Player != null)
            {
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
            }
        }
    }

    #endregion video players

    #region gif players
    public class GIFPlayerUWP : IWallpaper
    {
        public GIFPlayerUWP(string filePath, LibraryModel model, Screen display)
        {
            Player = new GIFViewUWP(filePath);
            this.Model = model;
            this.Display = display;
        }
        IntPtr HWND { get; set; }
        GIFViewUWP Player { get; set; }
        LibraryModel Model { get; set; }
        Screen Display { get; set; }
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

        public Process GetProcess()
        {
            throw new NotImplementedException();
        }

        public Screen GetScreen()
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

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }

        public void Show()
        {
            if(Player != null)
            {
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
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
        public WebProcess(string path, LibraryModel model, Screen display)
        {
            string cmdArgs;
            if (model.LivelyInfo.Type == WallpaperType.web)
            {
                cmdArgs = "--url " + "\"" + path + "\"" + " --type local" + " --display " + "\"" + display + "\"" +
                              " --property " + "\"" + System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper", "SaveData", "wpdata") + "\"";
            }
            else if (model.LivelyInfo.Type == WallpaperType.webaudio)
            {
                cmdArgs = "--url " + "\"" + path + "\"" + " --type local" + " --display " + "\"" + display + "\"" + " --audio true" +
                      " --property " + "\"" + System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper", "SaveData", "wpdata") + "\"";
            }
            else
            {
                cmdArgs = "--url " + "\"" + path + "\"" + " --type online" + " --display " + "\"" + display + "\"";
            }

            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = cmdArgs,
                FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper", "external", "cef", "LivelyCefSharp.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper", "external", "cef")
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
        Screen Display { get; set; }

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

        public Screen GetScreen()
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
        }

        public void Play()
        {
            NativeMethods.ShowWindow(HWND, 1); //normal
            NativeMethods.ShowWindow(HWND, 5); //show
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
                    Proc.Start();
                    Proc.BeginOutputReadLine();
                }
                catch { }
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }

    #endregion web browsers

    #region program wallpapers
    public class ExtPrograms : IWallpaper
    {
        public ExtPrograms(Process proc, IntPtr hwnd, LibraryModel model, Screen display)
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
        Screen Display { get; set; }
        public UInt32 SuspendCnt { get; set; }
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

        public Screen GetScreen()
        {
            return Display;
        }

        public void Show()
        {
            throw new NotImplementedException();
        }
    }

    #endregion progarm wallpapers
}
