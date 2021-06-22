using ImageMagick;
using livelywpf.Core.API;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

        public IntPtr GetHWND()
        {
            return hwnd;
        }

        public IntPtr GetHWNDInput()
        {
            return IntPtr.Zero;
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

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public async Task ScreenCapture(string filePath)
        {
            await Task.Run(() =>
            {
                //read first frame of gif image
                using var image = new MagickImage(GetWallpaperData().FilePath);
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

        public bool IsLoaded()
        {
            return player?.IsActive == true;
        }
    }
}
