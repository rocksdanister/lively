using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace livelywpf.Helpers.Files
{

    /// <summary>
    /// OpenFileDialog helper.
    /// </summary>
    public class FileFilter
    {
        public static readonly FileData[] LivelySupportedFormats = new FileData[] {
            new FileData(WallpaperType.video, new string[]{".wmv", ".avi", ".flv", ".m4v",
                    ".mkv", ".mov", ".mp4", ".mp4v", ".mpeg4",
                    ".mpg", ".webm", ".ogm", ".ogv", ".ogx" }),
            new FileData(WallpaperType.picture, new string[] {".jpg", ".jpeg", ".png", 
                    ".bmp", ".tif", ".tiff", ".webp" }),
            new FileData(WallpaperType.gif, new string[]{".gif" }),
            //new FileData(WallpaperType.heic, new string[] {".heic" }),//, ".heics", ".heif", ".heifs" }),
            new FileData(WallpaperType.web, new string[]{".html" }),
            new FileData(WallpaperType.webaudio, new string[]{".html" }),
            new FileData(WallpaperType.app, new string[]{".exe" }),
            //new FileFilter(WallpaperType.unity,"*.exe"),
            //new FileFilter(WallpaperType.unityaudio,"Unity Audio Visualiser |*.exe"),
            new FileData(WallpaperType.godot, new string[]{".exe" }),
            //note: lively .zip is not a wallpapertype, its a filetype.
            new FileData((WallpaperType)(100),  new string[]{".zip" })
        };

        public static string GetLocalisedWallpaperTypeString(WallpaperType type)
        {
            string localisedText = type switch
            {
                WallpaperType.app => Properties.Resources.TextApplication,
                WallpaperType.unity => "Unity",
                WallpaperType.godot => "Godot",
                WallpaperType.unityaudio => "Unity",
                WallpaperType.bizhawk => "Bizhawk",
                WallpaperType.web => Properties.Resources.TextWebsite,
                WallpaperType.webaudio => Properties.Resources.TitleAudio,
                WallpaperType.url => Properties.Resources.TextWebsite,
                WallpaperType.video => Properties.Resources.TextVideo,
                WallpaperType.gif => "Gif",
                WallpaperType.videostream => Properties.Resources.TextWebStream,
                WallpaperType.picture => Properties.Resources.TextPicture,
                //WallpaperType.heic => "HEIC",
                (WallpaperType)(100) => Properties.Resources.TitleAppName,
                _ => Properties.Resources.TextError,
            };
            return localisedText;
        }

        /// <summary>
        /// Identify Lively wallpaper type from file information.
        /// <br>If more than one wallpapertype has same extension, first result is selected.</br>
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>-1 if not supported, 100 if Lively .zip</returns>
        public static WallpaperType GetLivelyFileType(string filePath)
        {
            //todo: Use file header(?) to verify filetype instead of extension.
            var item = LivelySupportedFormats.FirstOrDefault(
                x => x.Extentions.Any(y => y.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase)));

            return item != null ? item.Type : (WallpaperType)(-1);
        }

        /// <summary>
        /// Generating filter text for file dialog (culture localised.)
        /// </summary>
        /// <param name="anyFile">Show any filetype.</param>
        /// <returns></returns>
        public static string GetLivelySupportedFileDialogFilter(bool anyFile = false)
        {
            StringBuilder filterString = new StringBuilder();
            if(anyFile)
            {
                filterString.Append(Properties.Resources.TextAllFiles + "|*.*|");
            }
            foreach (var item in LivelySupportedFormats)
            {
                filterString.Append(GetLocalisedWallpaperTypeString(item.Type));
                filterString.Append("|");
                foreach (var extension in item.Extentions)
                {
                    filterString.Append("*").Append(extension).Append(";");
                }
                filterString.Remove(filterString.Length - 1, 1);
                filterString.Append("|");
            }
            filterString.Remove(filterString.Length - 1, 1);

            return filterString.ToString();
        }
    }
}
