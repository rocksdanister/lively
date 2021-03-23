using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf.Core
{
    public class GIFPlayerUWP : IWallpaper
    {
        private IntPtr hwnd;
        private readonly GIFViewUWP player;
        private readonly LibraryModel model;
        private LivelyScreen display;
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public GIFPlayerUWP(string filePath, LibraryModel model, LivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            player = new GIFViewUWP(filePath, scaler);
            this.model = model;
            this.display = display;
        }

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                player.Close();
            }));
        }

        public IntPtr GetHWND()
        {
            return hwnd;
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
            return display;
        }

        public LibraryModel GetWallpaperData()
        {
            return model;
        }

        public WallpaperType GetWallpaperType()
        {
            return model.LivelyInfo.Type;
        }

        public void Pause()
        {
            player.Stop();
        }

        public void Play()
        {
            player.Play();
        }

        public void SendMessage(string msg)
        {
            //throw new NotImplementedException();
        }

        public void SetHWND(IntPtr hwnd)
        {
            this.hwnd = hwnd;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public void Show()
        {
            if(player != null)
            {
                player.Closed += Player_Closed;
                player.Show();
                hwnd = new WindowInteropHelper(player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            SetupDesktop.RefreshDesktop();
        }

        public void Stop()
        {
            player.Stop();
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
