using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf.Core
{
    /// <summary>
    /// Built in libvlc videoplayer.
    /// </summary>
    public class VideoPlayerVLC : IWallpaper
    {
        public VideoPlayerVLC(string filePath, LibraryModel model, LivelyScreen screen)
        {          
            Player = new VLCElement(filePath, model.LivelyInfo.Type == WallpaperType.videostream);
            this.Model = model;
            this.Display = screen;
        }

        IntPtr HWND { get; set; }
        VLCElement Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {
            if(Player != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    Player.Close();
                }));
            }
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
            if (Player != null)
            {
                Player.PausePlayer();
            }
        }

        public void Play()
        {
            if (Player != null)
            {
                Player.PlayMedia();
            }
        }

        public void SendMessage(string msg)
        {
            //throw new NotImplementedException();
        }

        public void SetHWND(IntPtr hwnd)
        {
            HWND = hwnd;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public async void Show()
        {
            if (Player != null)
            {
                Player.Closed += Player_Closed;
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    Player.Show();
                }));
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
            if (Player != null)
            {
                Player.StopPlayer();
            }
        }

        public void Terminate()
        {
            Close();
        }

        public void Resume()
        {
            //throw new NotImplementedException();
        }

        public void SetVolume(int volume)
        {
            if(Player != null)
            {
                Player.SetVolume(volume);
            }
        }
    }
}
