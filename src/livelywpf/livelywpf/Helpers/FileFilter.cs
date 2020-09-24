using System;
using System.Collections.Generic;
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
                _ => "Nil",
            };
            return localisedText;
        }
    }
}
