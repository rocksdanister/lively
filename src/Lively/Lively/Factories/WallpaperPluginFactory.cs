using Lively.Common.Helpers;
using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Core.Wallpapers;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Drawing;
using System.IO;

namespace Lively.Factories
{
    public class WallpaperPluginFactory : IWallpaperPluginFactory
    {
        private readonly IWebView2UserDataFactory webView2UserDataFactory;
        private readonly IDisplayManager displayManager;
        private readonly ILivelyPropertyFactory lpFactory;
        private readonly IUserSettingsService userSettings;

        public WallpaperPluginFactory(ILivelyPropertyFactory lpFactory,
            IWebView2UserDataFactory webViewUserDataFactory, 
            IDisplayManager displayManager,
            IUserSettingsService userSettings)
        {
            this.lpFactory = lpFactory;
            this.userSettings = userSettings;
            this.displayManager = displayManager;
            this.webView2UserDataFactory = webViewUserDataFactory;
        }

        public IWallpaper CreateDwmThumbnailWallpaper(LibraryModel model,
                                                      IntPtr thumbnailSrc,
                                                      Rectangle targetRect,
                                                      DisplayMonitor display)
        {
            return new DwmThumbnailPlayer(thumbnailSrc, model, display, targetRect);
        }

        public IWallpaper CreateWallpaper(LibraryModel model,
            DisplayMonitor display,
            WallpaperArrangement arrangement,
            bool isWindowed = false)
        {
            switch (model.LivelyInfo.Type)
            {
                case WallpaperType.web:
                case WallpaperType.webaudio:
                case WallpaperType.url:
                    switch (userSettings.Settings.WebBrowser)
                    {
                        case LivelyWebBrowser.cef:
                            return new WebCefSharpProcess(model.FilePath,
                                model,
                                display,
                                lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                userSettings.Settings.WebDebugPort,
                                userSettings.Settings.CefDiskCache,
                                userSettings.Settings.ApplicationTheme,
                                userSettings.Settings.AudioVolumeGlobal,
                                userSettings.Settings.VisualizerAudioDeviceId);
                        case LivelyWebBrowser.webview2:
                            return new WebWebView2(model.FilePath,
                                model,
                                display,
                                userSettings.Settings.WebDebugPort,
                                lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                GetWebView2UserDataDir(arrangement, display, isWindowed),
                                userSettings.Settings.ApplicationTheme,
                                userSettings.Settings.AudioVolumeGlobal,
                                GetWebView2Scale(display, isWindowed),
                                userSettings.Settings.VisualizerAudioDeviceId);
                    }
                    break;
                case WallpaperType.video:
                    //How many videoplayers you need? Yes.
                    switch (userSettings.Settings.VideoPlayer)
                    {
                        case LivelyMediaPlayer.wmf:
                            return new VideoWmfProcess(model.FilePath, model,
                                display, 0, userSettings.Settings.WallpaperScaling);
                        case LivelyMediaPlayer.libmpv:
                            throw new DepreciatedException("libmpv depreciated player selected.");
                        case LivelyMediaPlayer.libmpvExt:
                            throw new DepreciatedException("libmpvExt depreciated player selected.");
                        case LivelyMediaPlayer.libvlc:
                            throw new DepreciatedException("libvlc depreciated player selected.");
                        case LivelyMediaPlayer.libvlcExt:
                            return new VideoLibVlcPlayer(model.FilePath,
                                model,
                                display,
                                lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                userSettings.Settings.ApplicationTheme,
                                userSettings.Settings.AudioVolumeGlobal,
                                userSettings.Settings.VideoPlayerHwAccel);
                        case LivelyMediaPlayer.mpv:
                            return new VideoMpvPlayer(model.FilePath,
                                model,
                                display,
                                lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                userSettings.Settings.VideoPlayerHwAccel,
                                isWindowed: isWindowed,
                                userSettings.Settings.VideoTargetColorSpaceMode);
                        case LivelyMediaPlayer.vlc:
                            return new VideoVlcPlayer(model.FilePath, 
                                model, 
                                display,
                                userSettings.Settings.WallpaperScaling, 
                                userSettings.Settings.VideoPlayerHwAccel);
                    }
                    break;
                case WallpaperType.gif:
                    switch (userSettings.Settings.GifPlayer)
                    {
                        case LivelyGifPlayer.win10Img:
                        throw new PluginNotFoundException("xaml island gif player not available.");
                        case LivelyGifPlayer.libmpvExt:
                            throw new DepreciatedException("libmpvExt depreciated player selected.");
                        case LivelyGifPlayer.mpv:
                            return new VideoMpvPlayer(model.FilePath,
                                model,
                                display,
                                lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                userSettings.Settings.VideoPlayerHwAccel,
                                isWindowed: isWindowed, 
                                userSettings.Settings.VideoTargetColorSpaceMode);
                        case LivelyGifPlayer.libvlcExt:
                            return new VideoLibVlcPlayer(model.FilePath,
                                model,
                                display,
                                lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                userSettings.Settings.ApplicationTheme,
                                userSettings.Settings.AudioVolumeGlobal,
                                userSettings.Settings.VideoPlayerHwAccel);
                    }
                    break;
                case WallpaperType.picture:
                    switch (userSettings.Settings.PicturePlayer)
                    {
                        case LivelyPicturePlayer.picture:
                            throw new PluginNotFoundException("xaml island gif player not available.");
                        case LivelyPicturePlayer.winApi:
                        return new PictureWinApi(model.FilePath, model, display, arrangement, userSettings.Settings.WallpaperScaling);
                        case LivelyPicturePlayer.mpv:
                            return new VideoMpvPlayer(model.FilePath,
                                model,
                                display,
                                lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                userSettings.Settings.VideoPlayerHwAccel,
                                isWindowed: isWindowed,
                                userSettings.Settings.VideoTargetColorSpaceMode);
                        case LivelyPicturePlayer.wmf:
                            return new VideoWmfProcess(model.FilePath, model, display, 0, userSettings.Settings.WallpaperScaling);
                    }
                    break;
                case WallpaperType.app:
                case WallpaperType.bizhawk:
                case WallpaperType.unity:
                case WallpaperType.unityaudio:
                case WallpaperType.godot:
                    if (PackageUtil.IsRunningAsPackaged)
                    {
                        throw new MsixNotAllowedException("Program wallpaper on MSIX package not allowed.");
                    }
                    else
                    {
                        return new ExtPrograms(model.FilePath, model, display,
                          userSettings.Settings.WallpaperWaitTime);
                    }
                case WallpaperType.videostream:
                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "youtube-dl.exe")))
                    {
                        return new VideoMpvPlayer(model.FilePath,
                            model,
                            display,
                            lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                            userSettings.Settings.VideoPlayerHwAccel,
                            isWindowed: isWindowed,
                            userSettings.Settings.VideoTargetColorSpaceMode,
                            userSettings.Settings.StreamQuality);
                    }
                    else
                    {
                        return userSettings.Settings.WebBrowser switch
                        {
                            LivelyWebBrowser.cef => new WebCefSharpProcess(model.FilePath,
                                                        model,
                                                        display,
                                                        lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                                        userSettings.Settings.WebDebugPort,
                                                        userSettings.Settings.CefDiskCache,
                                                        userSettings.Settings.ApplicationTheme,
                                                        userSettings.Settings.AudioVolumeGlobal),
                            _ => new WebWebView2(model.FilePath,
                                                    model,
                                                    display,
                                                    userSettings.Settings.WebDebugPort,
                                                    lpFactory.CreateLivelyPropertyFolder(model, display, arrangement, userSettings),
                                                    GetWebView2UserDataDir(arrangement, display, isWindowed),
                                                    userSettings.Settings.ApplicationTheme,
                                                    userSettings.Settings.AudioVolumeGlobal,
                                                    GetWebView2Scale(display, isWindowed)),
                        };
                    }
            }
            throw new PluginNotFoundException("Wallpaper player not found.");
        }

        private string GetWebView2UserDataDir(WallpaperArrangement arrangement, DisplayMonitor display, bool isWindowed)
        {
            return userSettings.Settings.CefDiskCache && !isWindowed ? 
                webView2UserDataFactory.GetUserDataFolder(arrangement, display) : webView2UserDataFactory.GetTempUserDataFolder();
        }

        private double? GetWebView2Scale(DisplayMonitor display, bool isWindowed)
        {
            if (isWindowed || !displayManager.IsMultiScreen())
                return null;

            // When running as child of WorkerW/Progman, the WebView2 surface does not pick up the correct DPI. 
            return DpiUtil.TryGetDisplayScale(display.HMonitor, out double targetScale) ? targetScale : null;
        }

        #region exceptions

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

        public class PluginNotFoundException : Exception
        {
            public PluginNotFoundException()
            {
            }

            public PluginNotFoundException(string message)
                : base(message)
            {
            }

            public PluginNotFoundException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        #endregion //exceptions
    }
}
