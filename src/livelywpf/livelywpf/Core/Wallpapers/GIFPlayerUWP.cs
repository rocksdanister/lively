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

namespace livelywpf.Core.Wallpapers
{
    public class GIFPlayerUWP : IWallpaper
    {
        private IntPtr hwnd;
        private readonly GIFViewUWP player;
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

        public GIFPlayerUWP(string filePath, ILibraryModel model, ILivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            player = new GIFViewUWP(filePath, scaler == WallpaperScaler.auto ? WallpaperScaler.uniform : scaler);
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
