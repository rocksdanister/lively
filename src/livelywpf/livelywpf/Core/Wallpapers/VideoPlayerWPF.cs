using livelywpf.Core.API;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using livelywpf.Models;
using livelywpf.Views.Wallpapers;
using livelywpf.Helpers.Shell;

namespace livelywpf.Core.Wallpapers
{
    /// <summary>
    /// Built in Windws media foundation player.
    /// </summary>
    public class VideoPlayerWpf : IWallpaper
    {
        private readonly MediaElementView player;

        public bool IsLoaded => player?.IsActive == true;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public ILibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc => null;

        public ILivelyScreen Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public VideoPlayerWpf(string filePath, ILibraryModel model, ILivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            player = new MediaElementView(filePath, scaler == WallpaperScaler.auto ? WallpaperScaler.uniform : scaler);
            this.Model = model;
            this.Screen = display;
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
                Handle = new WindowInteropHelper(player).Handle;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            DesktopUtil.RefreshDesktop();
        }

        public void Terminate()
        {
            Close();
        }

        public void SetVolume(int volume)
        {
            player.SetVolume(volume);
        }

        public void SetMute(bool mute)
        {
            //todo
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
