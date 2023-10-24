using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Extensions
{
    public static class WallpaperExtensions
    {
        public static bool IsOnlineWallpaper(this WallpaperType type) => 
            type == WallpaperType.url || type == WallpaperType.videostream;

        public static bool IsWebWallpaper(this WallpaperType type) =>
            type == WallpaperType.web || type == WallpaperType.webaudio || type == WallpaperType.url;

        public static bool IsVideoWallpaper(this WallpaperType type) =>
            type == WallpaperType.video || type == WallpaperType.videostream;
    }
}
