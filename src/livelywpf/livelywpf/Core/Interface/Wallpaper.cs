using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using livelywpf.Model;

namespace livelywpf.Core
{
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
        void SetVolume(int volume);
        event EventHandler<WindowInitializedArgs> WindowInitialized;
    }

    public class WindowInitializedArgs
    {
        public bool Success { get; set; }
        public Exception Error { get; set; }
        public string Msg { get; set; }
    }
}
