using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace livelywpf.Helpers
{

    /// <summary>
    /// OpenFileDialog helper.
    /// </summary>
    public class FileFilter
    {
        public static readonly FileData[] LivelySupportedFormats = new FileData[] {
            new FileData(WallpaperType.video,
                "*.wmv; *.avi; *.flv; *.m4v;" +
                "*.mkv; *.mov; *.mp4; *.mp4v; *.mpeg4;" +
                "*.mpg; *.webm; *.ogm; *.ogv; *.ogx"),
            new FileData(WallpaperType.picture,
                "*.jpg; *.jpeg; *.png; *.bmp; *.tif; *.tiff"),
            new FileData(WallpaperType.gif, "*.gif"),
            new FileData(WallpaperType.web, "*.html"),
            new FileData(WallpaperType.webaudio, "*.html"),
            new FileData(WallpaperType.app,"*.exe"),
            //new FileFilter(WallpaperType.unity,"*.exe"),
            //new FileFilter(WallpaperType.unityaudio,"Unity Audio Visualiser |*.exe"),
            new FileData(WallpaperType.godot,"*.exe"),
            //note: lively .zip is not a wallpapertype, its a filetype.
            new FileData((WallpaperType)(100), "*.zip")};

        public static string GetLocalisedWallpaperTypeText(WallpaperType type)
        {
            string localisedText = type switch
            {
                WallpaperType.app => Properties.Resources.TextApplication,
                WallpaperType.unity => Properties.Resources.TextApplication + " Unity",
                WallpaperType.godot => Properties.Resources.TextApplication + " Godot",
                WallpaperType.unityaudio => Properties.Resources.TextApplication + " Unity " + Properties.Resources.TitleAudio,
                WallpaperType.bizhawk => Properties.Resources.TextApplication + " Bizhawk",
                WallpaperType.web => Properties.Resources.TextWebsite,
                WallpaperType.webaudio => Properties.Resources.TextWebsite + " " + Properties.Resources.TitleAudio,
                WallpaperType.url => Properties.Resources.TextOnline,
                WallpaperType.video => Properties.Resources.TextVideo,
                WallpaperType.gif => Properties.Resources.TextGIF,
                WallpaperType.videostream => Properties.Resources.TextWebStream,
                WallpaperType.picture => Properties.Resources.TextPicture,
                (WallpaperType)(100) => Properties.Resources.TitleAppName,
                _ => Properties.Resources.TextError,
            };
            return localisedText;
        }

        /// <summary>
        /// Identify Lively wallpaper type from file information.
        ///  If more than one wallpapertype has same extension, first result is selected.
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>-1 if not supported, 100 if Lively .zip</returns>
        public static WallpaperType GetFileType(string filePath)
        {
            //todo: Use file header(?) to verify filetype instead of extension.
            var item = LivelySupportedFormats.FirstOrDefault(x => x.Extentions.Contains(
                Path.GetExtension(filePath),
                StringComparison.OrdinalIgnoreCase));

            return item != null ? item.Type : (WallpaperType)(-1);
        }
    }
}
