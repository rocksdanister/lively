using Lively.Common.Com;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    //Problems:
    //Screensaver won't work since hwnd is zero.
    //Preview wallpaper won't work since hwnd is zero.
    //Restore wallpaper dont work properly since show() sets it even before core!
    public class PictureWinApi : IWallpaper
    {
        private class WinWallpaper
        {
            public WinWallpaper(string deviceId, string filePath)
            {
                DeviceId = deviceId;
                FilePath = filePath;
            }

            public string DeviceId { get; set; }
            public string FilePath { get; set; }
        }

        private readonly DesktopWallpaperPosition desktopScaler;
        private readonly IDesktopWallpaper desktop;
        private readonly List<WinWallpaper> wallpapersToRestore;

        public bool IsExited { get; private set; }

        public bool IsLoaded => true;

        public WallpaperType Category => WallpaperType.picture;

        public LibraryModel Model { get; }

        public IntPtr Handle => IntPtr.Zero;

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc => null;

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        private readonly WallpaperArrangement arrangement;
        private readonly string filePath;

        public PictureWinApi(string filePath,
            LibraryModel model,
            DisplayMonitor display,
            WallpaperArrangement arrangement,
            WallpaperScaler scaler = WallpaperScaler.fill)
        {
            //Has transition animation if enabled in os settings..
            wallpapersToRestore = new List<WinWallpaper>();
            desktop = (IDesktopWallpaper)new DesktopWallpaperClass();
            //Keeping track of system wallpapers..
            if (arrangement == WallpaperArrangement.span)
            {
                for (uint i = 0; i < desktop.GetMonitorDevicePathCount(); i++)
                {
                    var id = desktop.GetMonitorDevicePathAt(i);
                    wallpapersToRestore.Add(new WinWallpaper(id, desktop.GetWallpaper(id)));
                }
            }
            else
            {
                wallpapersToRestore.Add(new WinWallpaper(display.DeviceId, desktop.GetWallpaper(display.DeviceId)));
            }
            //lively scaler != win scaler
            desktopScaler = scaler switch
            {
                WallpaperScaler.none => DesktopWallpaperPosition.Center,
                WallpaperScaler.fill => DesktopWallpaperPosition.Stretch,
                WallpaperScaler.uniform => DesktopWallpaperPosition.Fit,
                WallpaperScaler.uniformFill => DesktopWallpaperPosition.Fill, //not the same, here uniform fill pivot is topleft whereas for windows its center.
                WallpaperScaler.auto => DesktopWallpaperPosition.Fill, //todo
                _ => DesktopWallpaperPosition.Fill,
            };

            this.Model = model;
            this.Screen = display;
            this.filePath = filePath;
            this.arrangement = arrangement;
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

        public void SetScreen(DisplayMonitor display)
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

        public async Task ShowAsync()
        {
            //desktop.Enable();
            desktop.SetPosition(arrangement == WallpaperArrangement.span ? DesktopWallpaperPosition.Span : desktopScaler);
            desktop.SetWallpaper(arrangement == WallpaperArrangement.span ? null : Screen.DeviceId, filePath);

            //Nothing to setup..
        }

        public void Stop()
        {
            //nothing
        }
        
        public void Close()
        {
            RestoreWallpaper();
            IsExited = true;
        }

        public void Terminate()
        {
            RestoreWallpaper();
            IsExited = true;
        }

        //restore original wallpaper (if possible.)
        private void RestoreWallpaper()
        {
            /*
            foreach (var item in wallpapersToRestore)
            {
                desktop.SetWallpaper(item.DeviceId, item.FilePath);
            }
            */
        }

        public void SendMessage(IpcMessage obj)
        {
            //nothing
        }
    }
}
