using Lively.Models;
using Lively.Models.Enums;
using System;
using System.IO;
using System.Linq;

namespace Lively.Common
{
    public static class FileTypes
    {
        public static readonly FileTypeModel[] SupportedFormats = new FileTypeModel[] {
            new FileTypeModel(WallpaperType.video, new string[]{".wmv", ".avi", ".flv", ".m4v",
                    ".mkv", ".mov", ".mp4", ".mp4v", ".mpeg4",
                    ".mpg", ".webm", ".ogm", ".ogv", ".ogx" }),
            new FileTypeModel(WallpaperType.picture, new string[] {".jpg", ".jpeg", ".png",
                    ".bmp", ".tif", ".tiff", ".webp", ".jfif" }),
            new FileTypeModel(WallpaperType.gif, new string[]{".gif" }),
            //new FileData(WallpaperType.heic, new string[] {".heic" }),//, ".heics", ".heif", ".heifs" }),
            new FileTypeModel(WallpaperType.web, new string[]{".html" }),
            new FileTypeModel(WallpaperType.webaudio, new string[]{".html" }),
            new FileTypeModel(WallpaperType.app, new string[]{".exe" }),
            //new FileFilter(WallpaperType.unity,"*.exe"),
            //new FileFilter(WallpaperType.unityaudio,"Unity Audio Visualiser |*.exe"),
            new FileTypeModel(WallpaperType.godot, new string[]{".exe" }),
            //note: lively .zip is not a wallpapertype, its a filetype.
            new FileTypeModel((WallpaperType)100,  new string[]{".zip" })
        };

        /// <summary>
        /// Identify Lively wallpaper type from file information.
        /// <br>If more than one wallpapertype has same extension, first result is selected.</br>
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>-1 if not supported, 100 if Lively .zip</returns>
        public static WallpaperType GetFileType(string filePath)
        {
            //todo: Use file header(?) to verify filetype instead of extension.
            var item = SupportedFormats.FirstOrDefault(
                x => x.Extentions.Any(y => y.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase)));

            return item != null ? item.Type : (WallpaperType)(-1);
        }
    }
}
