using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using livelywpf.Core.API;
using livelywpf.Helpers;
using livelywpf.Models;

namespace livelywpf.Core.Wallpapers
{
    //incomplete
    class PictureWallpaper : IWallpaper
    {
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        private readonly DesktopWallpaperPosition desktopScaler;
        private readonly IDesktopWallpaper desktop;
        private readonly string systemWallpaperPath;

        public bool IsLoaded => true;

        public WallpaperType Category => WallpaperType.picture;

        public ILibraryModel Model { get; }

        public IntPtr Handle => IntPtr.Zero;

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc => null;

        public ILivelyScreen Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        private readonly WallpaperArrangement arrangement;

        public PictureWallpaper(string filePath, ILibraryModel model, ILivelyScreen display, WallpaperArrangement arrangement, WallpaperScaler scaler = WallpaperScaler.fill)
        {
            this.arrangement = arrangement;

            //Has transition animation..
            desktop = (IDesktopWallpaper)new Helpers.DesktopWallpaperClass();
            systemWallpaperPath = desktop.GetWallpaper(display.DeviceId);
            desktopScaler = Helpers.DesktopWallpaperPosition.Fill;
            switch (scaler)
            {
                case WallpaperScaler.none:
                    desktopScaler = Helpers.DesktopWallpaperPosition.Center;
                    break;
                case WallpaperScaler.fill:
                    desktopScaler = Helpers.DesktopWallpaperPosition.Stretch;
                    break;
                case WallpaperScaler.uniform:
                    desktopScaler = Helpers.DesktopWallpaperPosition.Fit;
                    break;
                case WallpaperScaler.uniformFill:
                    //not exaclty the same, lively's uniform fill pivot is topleft whereas for windows its center.
                    desktopScaler = Helpers.DesktopWallpaperPosition.Fill;
                    break;
            }
            this.Screen = display;
            this.Model = model;
        }

        public void Close()
        {
            Terminate();
        }

        public void Pause()
        {
            //nothing
        }

        public void Play()
        {
            //nothing
        }

        public Task ScreenCapture(string filePath)
        {
            throw new NotImplementedException();
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //nothing
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Screen = display;
        }

        public void SetVolume(int volume)
        {
            //nothing
        }

        public void SetMute(bool mute)
        {
            //nothing
        }

        public void Show()
        {
            //desktop.Enable();
            desktop.SetPosition(arrangement == WallpaperArrangement.span ? Helpers.DesktopWallpaperPosition.Span : desktopScaler);
            desktop.SetWallpaper(arrangement == WallpaperArrangement.span ? null : Screen.DeviceId, Model.FilePath);
        }

        public void Stop()
        {
            //nothing
        }

        public void Terminate()
        {
            //restoring original wallpaper.
            desktop.SetWallpaper(Screen.DeviceId ,systemWallpaperPath);
        }

        public void SendMessage(IpcMessage obj)
        {
            //todo
        }
    }
}
