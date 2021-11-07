using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using livelywpf.Core;
using livelywpf.Core.Wallpapers;
using livelywpf.Models;
using livelywpf.Services;

namespace livelywpf.Factories
{
    public class WallpaperFactory : IWallpaperFactory
    {
        public class MsixNotAllowedException : Exception
        {
            public MsixNotAllowedException()
            {
            }

            public MsixNotAllowedException(string message)
                : base(message)
            {
            }

            public MsixNotAllowedException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public class DepreciatedException : Exception
        {
            public DepreciatedException()
            {
            }

            public DepreciatedException(string message)
                : base(message)
            {
            }

            public DepreciatedException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public IWallpaper CreateWallpaper(ILibraryModel obj, ILivelyScreen display, IUserSettingsService userSettings)
        {
            IWallpaper instanceWallpaper = null;
            switch (obj.LivelyInfo.Type)
            {
                case WallpaperType.web:
                case WallpaperType.webaudio:
                case WallpaperType.url:
                    switch (Program.SettingsVM.Settings.WebBrowser)
                    {
                        case LivelyWebBrowser.cef:
                            instanceWallpaper = new WebProcess(obj.FilePath, obj, display);
                            break;
                        case LivelyWebBrowser.webview2:
                            instanceWallpaper = new WebEdge(obj.FilePath, obj, display);
                            break;
                    }
                    break;
                case WallpaperType.video:
                    //How many videoplayers you need? Yes.
                    switch (Program.SettingsVM.Settings.VideoPlayer)
                    {
                        case LivelyMediaPlayer.wmf:
                            instanceWallpaper = new VideoPlayerWPF(obj.FilePath, obj,
                                display, Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyMediaPlayer.libvlc:
                            //depreciated
                            throw new DepreciatedException("libvlc depreciated player selected.");
                        case LivelyMediaPlayer.libmpv:
                            //depreciated
                            throw new DepreciatedException("libmpv depreciated player selected.");
                        case LivelyMediaPlayer.libvlcExt:
                            instanceWallpaper = new VideoPlayerVLCExt(obj.FilePath, obj, display);
                            break;
                        case LivelyMediaPlayer.libmpvExt:
                            instanceWallpaper = new VideoPlayerMPVExt(obj.FilePath, obj, display,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyMediaPlayer.mpv:
                            instanceWallpaper = new VideoMpvPlayer(obj.FilePath, obj, display,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyMediaPlayer.vlc:
                            instanceWallpaper = new VideoVlcPlayer(obj.FilePath, obj, display,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                    }
                    break;
                case WallpaperType.gif:
                case WallpaperType.picture:
                    switch (Program.SettingsVM.Settings.GifPlayer)
                    {
                        case LivelyGifPlayer.win10Img:
                            instanceWallpaper = new GIFPlayerUWP(obj.FilePath, obj,
                                display, Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyGifPlayer.libmpvExt:
                            instanceWallpaper = new VideoPlayerMPVExt(obj.FilePath, obj, display,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyGifPlayer.mpv:
                            instanceWallpaper = new VideoMpvPlayer(obj.FilePath, obj, display,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                    }
                    break;
                case WallpaperType.app:
                case WallpaperType.bizhawk:
                case WallpaperType.unity:
                case WallpaperType.unityaudio:
                case WallpaperType.godot:
                    if (Constants.ApplicationType.IsMSIX)
                    {
                        throw new MsixNotAllowedException("Program wallpaper on MSIX package not allowed.");
                    }
                    else
                    {
                        instanceWallpaper = new ExtPrograms(obj.FilePath, obj, display,
                            Program.SettingsVM.Settings.WallpaperWaitTime);
                    }
                    break;
                case WallpaperType.videostream:
                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "youtube-dl.exe")))
                    {
                        instanceWallpaper = new VideoMpvPlayer(obj.FilePath, obj, display,
                               Program.SettingsVM.Settings.WallpaperScaling, Program.SettingsVM.Settings.StreamQuality);
                    }
                    else
                    {
                        //note: wallpaper type will be videostream, don't forget..
                        instanceWallpaper = new WebProcess(obj.FilePath, obj, display);
                    }
                    break;
            }
            return instanceWallpaper;
        }
    }
}
