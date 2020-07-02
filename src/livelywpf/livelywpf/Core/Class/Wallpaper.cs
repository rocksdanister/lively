using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

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
        void Pause();
        void Play();
        void Stop();
        void Close();
        Screen GetScreen();
    }

    #endregion interface

    #region video players
    public class VideoPlayerWPF : IWallpaper
    {
        public VideoPlayerWPF(MediaElementWPF player, IntPtr hwnd, LibraryModel model, Screen display)
        {
            this.HWND = hwnd;
            this.Player = player;
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
            Player.Close();
        }

        public Screen GetScreen()
        {
            return Display;
        }
    }

    #endregion video players

    #region gif players
    public class GIFPlayerUWP : IWallpaper
    {
        public GIFPlayerUWP(GIFViewUWP player, IntPtr hwnd, LibraryModel model, Screen display)
        {
            this.HWND = hwnd;
            this.Player = player;
            this.Model = model;
            this.Display = display;
        }
        IntPtr HWND { get; set; }
        GIFViewUWP Player { get; set; }
        LibraryModel Model { get; set; }
        Screen Display { get; set; }
        public void Close()
        {
            Player.Close();
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
            throw new NotImplementedException();
            //Player.Pause();
        }

        public void Play()
        {
            Player.Play();
        }

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
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
        public WebProcess(Process proc, IntPtr hwnd, LibraryModel model, Screen display)
        {
            this.HWND = hwnd;
            this.Proc = proc;
            this.Model = model;
            this.Display = display;
        }
        IntPtr HWND { get; set; }
        Process Proc { get; set; }
        LibraryModel Model { get; set; }
        Screen Display { get; set; }

        public void Close()
        {
            //todo: send close msg through ipc instead.
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
            throw new NotImplementedException();
        }

        public void Play()
        {
            throw new NotImplementedException();
        }

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
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
    }

    #endregion progarm wallpapers
}
