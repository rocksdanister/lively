using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf.Core
{
    public class GIFPlayerUWP : IWallpaper
    {
        IntPtr HWND { get; set; }
        GIFViewUWP Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public GIFPlayerUWP(string filePath, LibraryModel model, LivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            Player = new GIFViewUWP(filePath, scaler);
            this.Model = model;
            this.Display = display;
        }

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
            Player.Stop();
        }

        public void Play()
        {
            Player.Play();
        }

        public void SendMessage(string msg)
        {
            //throw new NotImplementedException();
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

        public void SetVolume(int volume)
        {
            //gif has no sound.
        }

        public void SetPlaybackPos(int pos)
        {
            
        }
    }
}
