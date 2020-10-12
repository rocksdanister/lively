using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf.Core
{
    class WebEdge : IWallpaper
    {
        public WebEdge(string path, LibraryModel model, LivelyScreen display)
        {
            Player = new WebView2Element(path);
            this.Model = model;
            this.Display = display;
        }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        IntPtr HWND { get; set; }
        WebView2Element Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }

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

        }

        public void Play()
        {

        }

        public void Resume()
        {

        }

        public void SendMessage(string msg)
        {

        }

        public void SetHWND(IntPtr hwnd)
        {

        }

        public void SetScreen(LivelyScreen display)
        {
 
        }

        public void SetVolume(int volume)
        {

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

        public void Stop()
        {

        }

        public void Terminate()
        {
            Close();
        }
    }
}
