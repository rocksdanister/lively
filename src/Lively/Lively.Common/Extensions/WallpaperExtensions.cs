using Lively.Models.Enums;

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

        public static bool IsApplicationWallpaper(this WallpaperType type) =>
            type == WallpaperType.unity || type == WallpaperType.unityaudio || type == WallpaperType.app || type == WallpaperType.godot || type == WallpaperType.bizhawk;

        /// <summary>
        /// Picture, gif and other non dynamic format
        /// </summary>
        public static bool IsMediaWallpaper(this WallpaperType type) => 
            IsVideoWallpaper(type) || type == WallpaperType.gif || type == WallpaperType.picture;
    }
}
