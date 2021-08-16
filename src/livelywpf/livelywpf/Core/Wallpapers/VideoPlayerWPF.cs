using livelywpf.Core.API;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf.Core
{
    /// <summary>
    /// Built in Windws media foundation player.
    /// </summary>
    public class VideoPlayerWPF : IWallpaper
    {
        private IntPtr hwnd;
        private readonly MediaElementWPF player;
        private readonly LibraryModel model;
        private LivelyScreen display;
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public VideoPlayerWPF(string filePath, LibraryModel model, LivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            player = new MediaElementWPF(filePath, scaler == WallpaperScaler.auto ? WallpaperScaler.uniform : scaler);
            this.model = model;
            this.display = display;
        }

        public WallpaperType GetWallpaperType()
        {
            return WallpaperType.video;
        }

        public LibraryModel GetWallpaperData()
        {
            return model;
        }

        public IntPtr GetHWND()
        {
            return hwnd;
        }

        public IntPtr GetHWNDInput()
        {
            return IntPtr.Zero;
        }

        public Process GetProcess()
        {
            return null;
        }

        public void Play()
        {
            player.PlayMedia();
        }

        public void Pause()
        {
            player.PausePlayer();
        }

        public void Stop()
        {
            player.StopPlayer();
        }

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                player.Close();
            }));
        }

        public LivelyScreen GetScreen()
        {
            return display;
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

        public void SendMessage(string msg)
        {
            //todo
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public void Terminate()
        {
            Close();
        }

        public void SetVolume(int volume)
        {
            player.SetVolume(volume);
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //todo
        }

        public Task ScreenCapture(string filePath)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(IpcMessage obj)
        {
            //todo
        }

        public bool IsLoaded()
        {
            return player?.IsActive == true;
        }
    }
}
