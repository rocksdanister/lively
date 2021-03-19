using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace livelywpf.Core
{
    //Ref: 
    //https://github.com/rocksdanister/lively/discussions/342
    //https://wiki.videolan.org/documentation:modules/rc/
    public class VideoVlcPlayer : IWallpaper
    {
        public VideoVlcPlayer(string path, LibraryModel model, LivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            string cmdArgs =
                //hide menus and controls.
                "--qt-minimal-view " +
                //prevent player window resizing to video size.
                "--no-qt-video-autoresize " +
                //do not create system-tray icon.
                "--no-qt-system-tray " +
                //allow screensaver
                "--no-disable-screensaver " +
                //file path
                "\"" + path + "\"";

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vlc", "vlc.exe"),
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vlc"),
                Arguments = cmdArgs,
            };

            Process proc = new Process()
            {
                StartInfo = start,
            };
        }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public IntPtr GetHWND()
        {
            throw new NotImplementedException();
        }

        public string GetLivelyPropertyCopyPath()
        {
            throw new NotImplementedException();
        }

        public Process GetProcess()
        {
            throw new NotImplementedException();
        }

        public LivelyScreen GetScreen()
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

        public void SendMessage(string msg)
        {
            throw new NotImplementedException();
        }

        public void SetHWND(IntPtr hwnd)
        {
            throw new NotImplementedException();
        }

        public void SetPlaybackPos(int pos)
        {
            throw new NotImplementedException();
        }

        public void SetScreen(LivelyScreen display)
        {
            throw new NotImplementedException();
        }

        public void SetVolume(int volume)
        {
            throw new NotImplementedException();
        }

        public void Show()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
