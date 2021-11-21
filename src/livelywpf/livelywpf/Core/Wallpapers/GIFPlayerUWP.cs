using ImageMagick;
using livelywpf.Core.API;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using livelywpf.Models;
using livelywpf.Views.Wallpapers;
using livelywpf.Helpers.Shell;

namespace livelywpf.Core.Wallpapers
{
    public class GIFPlayerUwp : IWallpaper
    {
        private readonly GifUwpView player;

        public bool IsLoaded => player?.IsActive == true;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public ILibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc => null;

        public ILivelyScreen Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public GIFPlayerUwp(string filePath, ILibraryModel model, ILivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            player = new GifUwpView(filePath, scaler == WallpaperScaler.auto ? WallpaperScaler.uniform : scaler);
            this.Model = model;
            this.Screen = display;
        }

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                player.Close();
            }));
        }

        public void Pause()
        {
            player.Stop();
        }

        public void Play()
        {
            player.Play();
        }

        public async Task ScreenCapture(string filePath)
        {
            await Task.Run(() =>
            {
                //read first frame of gif image
                using var image = new MagickImage(Model.FilePath);
                if (image.Width < 1920)
                {
                    //if the image is too small then resize to min: 1080p using integer scaling for sharpness.
                    image.FilterType = FilterType.Point;
                    image.Thumbnail(new Percentage(100 * 1920 / image.Width));
                }
                image.Write(Path.GetExtension(filePath) != ".jpg" ? filePath + ".jpg" : filePath);
            });
        }

        public void Show()
        {
            if (player != null)
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

        public void SetMute(bool mute)
        {
            //nothing
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //todo
        }

        public void SendMessage(IpcMessage obj)
        {
            //todo
        }
    }
}
