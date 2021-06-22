using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using livelywpf.Core.API;
using livelywpf.Helpers;

namespace livelywpf.Core
{
    //incomplete
    class PictureWallpaper : IWallpaper
    {
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        private DesktopWallpaperPosition desktopScaler;
        private IDesktopWallpaper desktop;
        private readonly LibraryModel model;
        private string systemWallpaperPath;
        private LivelyScreen display;

        public PictureWallpaper(string filePath, LibraryModel model, LivelyScreen display, WallpaperScaler scaler = WallpaperScaler.fill)
        {
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
            this.display = display;
            this.model = model;
        }

        public void Close()
        {
            Terminate();
        }

        public IntPtr GetHWND()
        {
            return IntPtr.Zero;
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
            return WallpaperType.picture;
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

        public void SendMessage(string msg)
        {
            //nothing
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //nothing
        }

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public void SetVolume(int volume)
        {
            //nothing
        }

        public void Show()
        {
            //desktop.Enable();
            desktop.SetPosition(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span ? Helpers.DesktopWallpaperPosition.Span : desktopScaler);
            desktop.SetWallpaper(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span ? null : display.DeviceId, model.FilePath);
        }

        public void Stop()
        {
            //nothing
        }

        public void Terminate()
        {
            //restoring original wallpaper.
            desktop.SetWallpaper(display.DeviceId ,systemWallpaperPath);
        }

        public void SendMessage(IpcMessage obj)
        {
            //todo
        }

        public bool IsLoaded()
        {
            //nothing to load..
            return true;
        }
    }
}
