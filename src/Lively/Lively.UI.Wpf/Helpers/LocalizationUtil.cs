using Lively.Common;
using Lively.Common.Helpers.Files;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.UI.Wpf.Helpers
{
    public class LocalizationUtil
    {
        public static string GetLocalizedWallpaperCategory(WallpaperType type)
        {
            return type switch
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
        }

        /// <summary>
        /// Generating filter text for file dialog (culture localised.)
        /// </summary>
        /// <param name="anyFile">Show any filetype.</param>
        /// <returns></returns>
        public static string GetLocalizedSupportedFileDialogFilter(bool anyFile = false)
        {
            StringBuilder filterString = new StringBuilder();
            if(anyFile)
            {
                filterString.Append(Properties.Resources.TextAllFiles + "|*.*|");
            }
            foreach (var item in FileFilter.LivelySupportedFormats)
            {
                filterString.Append(GetLocalizedWallpaperCategory(item.Type));
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
