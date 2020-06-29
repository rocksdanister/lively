using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace livelywpf.Core
{
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
    }

    #region video players
    public class VideoPlayerWPF : IWallpaper
    {
        public VideoPlayerWPF(MediaElementWPF player, IntPtr hwnd, LibraryModel model = null)
        {
            this.HWND = hwnd;
            this.Player = player;
            this.Model = model;
        }

        IntPtr HWND { get; set; }
        MediaElementWPF Player { get; set; }
        LibraryModel Model { get; set; }
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
    }

    #endregion video players

    #region gif players
    public class GIFPlayerUWP : IWallpaper
    {
        public GIFPlayerUWP(GIFViewUWP player, IntPtr hwnd, LibraryModel model = null)
        {
            this.HWND = hwnd;
            this.Player = player;
            this.Model = model;
        }
        IntPtr HWND { get; set; }
        GIFViewUWP Player { get; set; }
        LibraryModel Model { get; set; }
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
        public void Close()
        {
            throw new NotImplementedException();
        }

        public IntPtr GetHWND()
        {
            throw new NotImplementedException();
        }

        public Process GetProcess()
        {
            throw new NotImplementedException();
        }

        public LibraryModel GetWallpaperData()
        {
            throw new NotImplementedException();
        }

        public WallpaperType GetWallpaperType()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }

    #endregion web browsers
}
