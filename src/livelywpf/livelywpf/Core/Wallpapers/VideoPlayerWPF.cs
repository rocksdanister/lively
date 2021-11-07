using livelywpf.Core.API;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using livelywpf.Models;
using livelywpf.Views.Wallpapers;

namespace livelywpf.Core.Wallpapers
{
    /// <summary>
    /// Built in Windws media foundation player.
    /// </summary>
    public class VideoPlayerWPF : IWallpaper
    {
        private IntPtr hwnd;
        private readonly MediaElementWPF player;
        private readonly ILibraryModel model;
        private ILivelyScreen display;

        public bool IsLoaded => player?.IsActive == true;

        public WallpaperType Category => model.LivelyInfo.Type;

        public ILibraryModel Model => model;

        public IntPtr Handle => hwnd;

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc => null;

        public ILivelyScreen Screen { get => display; set => display = value; }

        public string LivelyPropertyCopyPath => null;

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public VideoPlayerWPF(string filePath, ILibraryModel model, ILivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            player = new MediaElementWPF(filePath, scaler == WallpaperScaler.auto ? WallpaperScaler.uniform : scaler);
            this.model = model;
            this.display = display;
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
    }
}
