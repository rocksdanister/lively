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
        public WallpaperType Type { get; set; }
        public string Extentions { get; set; }
        public string LocalisedTypeText { get; set; }
        public FileFilter(WallpaperType type, string filterText, string localisedText = null)
        {
            this.Type = type;
            this.Extentions = filterText;
            if (localisedText == null)
            {
                LocalisedTypeText = LocaliseWallpaperTypeEnum(Type);
            }
            else
            {
                this.LocalisedTypeText = localisedText;
            }
        }

        public static string LocaliseWallpaperTypeEnum(WallpaperType type)
        {
            string localisedText = type switch
            {
                livelywpf.WallpaperType.app => Properties.Resources.TextApplication,
                livelywpf.WallpaperType.unity => Properties.Resources.TextApplication + " Unity",
                livelywpf.WallpaperType.godot => Properties.Resources.TextApplication + " Godot",
                livelywpf.WallpaperType.unityaudio => Properties.Resources.TextApplication + " Unity " + Properties.Resources.TitleAudio,
                livelywpf.WallpaperType.bizhawk => Properties.Resources.TextApplication + " Bizhawk",
                livelywpf.WallpaperType.web => Properties.Resources.TextWebsite,
                livelywpf.WallpaperType.webaudio => Properties.Resources.TextWebsite + " " + Properties.Resources.TitleAudio,
                livelywpf.WallpaperType.url => Properties.Resources.TextOnline,
                livelywpf.WallpaperType.video => Properties.Resources.TextVideo,
                livelywpf.WallpaperType.gif => Properties.Resources.TextGIF,
                livelywpf.WallpaperType.videostream => Properties.Resources.TextWebStream,
                livelywpf.WallpaperType.picture => "Picture",
                _ => "Nil",
            };
            return localisedText;
        }

        /// <summary>
        /// Get filetype.
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>-1 if not supported, 100 if Lively .zip</returns>
        public static WallpaperType GetFileType(string filePath)
        {
            //todo: Use header(?) to verify filetype instead of extension.
            FileFilter[] wallpaperFilter = new FileFilter[] {
            new FileFilter(WallpaperType.video,
                "*.wmv; *.avi; *.flv; *.m4v;" +
                "*.mkv; *.mov; *.mp4; *.mp4v; *.mpeg4;" +
                "*.mpg; *.webm; *.ogm; *.ogv; *.ogx"),
            new FileFilter(WallpaperType.picture,
                "*.jpg; *.jpeg; *.png; *.bmp; *.tif; *.tiff"),
            new FileFilter(WallpaperType.web, "*.html"),
            new FileFilter(WallpaperType.webaudio, "*.html"),
            new FileFilter(WallpaperType.gif, "*.gif"),
            new FileFilter(WallpaperType.app,"*.exe"),
            //new FileFilter(WallpaperType.unity,"*.exe"),
            //new FileFilter(WallpaperType.unityaudio,"Unity Audio Visualiser |*.exe"),
            new FileFilter(WallpaperType.godot,"*.exe"),
            //note: lively .zip is not a wallpapertype, its a filetype.
            new FileFilter((WallpaperType)(100), "*.zip", Properties.Resources.TitleAppName)};

            var item = wallpaperFilter.FirstOrDefault(x => x.Extentions.Contains(
                Path.GetExtension(filePath),
                StringComparison.OrdinalIgnoreCase));

            if (item != null)
            {
                return item.Type;
            }
            else
            {
                return (WallpaperType)(-1);
            }
        }
    }
}
