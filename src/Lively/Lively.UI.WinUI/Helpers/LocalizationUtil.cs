using Lively.Common;
using Lively.Common.Helpers.Files;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.Helpers
{
    //Ref: https://docs.microsoft.com/en-us/windows/uwp/app-resources/localize-strings-ui-manifest
    public static class LocalizationUtil
    {
        private static readonly ResourceLoader resourceLoader;

        static LocalizationUtil()
        {
            //Use GetForViewIndependentUse instead of GetForCurrentView when resolving resources from code as there is no current view in non-packaged scenarios.
            //The following exception occurs if you call GetForCurrentView in non-packaged scenarios: Resource Contexts may not be created on threads that do not have a CoreWindow.
            resourceLoader = ResourceLoader.GetForViewIndependentUse();
        }

        public static string LocalizeWallpaperCategory(WallpaperType type)
        {
            return type switch
            {
                WallpaperType.app => resourceLoader.GetString("TextApplication"),
                WallpaperType.unity => "Unity",
                WallpaperType.godot => "Godot",
                WallpaperType.unityaudio => "Unity",
                WallpaperType.bizhawk => "Bizhawk",
                WallpaperType.web => resourceLoader.GetString("Website/Header"),
                WallpaperType.webaudio => resourceLoader.GetString("AudioGroup/Header"),
                WallpaperType.url => resourceLoader.GetString("Website/Header"),
                WallpaperType.video => resourceLoader.GetString("TextVideo"),
                WallpaperType.gif => "Gif",
                WallpaperType.videostream => resourceLoader.GetString("TextWebStream"),
                WallpaperType.picture => resourceLoader.GetString("TextPicture"),
                //WallpaperType.heic => "HEIC",
                (WallpaperType)(100) => "Lively Wallpaper",
                _ => resourceLoader.GetString("TextError"),
            };
        }

        public static List<string> FileDialogFilterAll(bool anyFile = false)
        {
            var filterCollection = new List<string>();
            if (anyFile)
            {
                filterCollection.Add("*");
            }
            foreach (var item in FileFilter.LivelySupportedFormats)
            {
                foreach (var extension in item.Extentions)
                {
                    filterCollection.Add(extension);
                }
            }
            return filterCollection.Distinct().ToList();
        }

        public static string[] FileDialogFilter(WallpaperType wallpaperType) => 
            FileFilter.LivelySupportedFormats.First(x => x.Type == wallpaperType).Extentions;

        public static string FileDialogFilterNative(WallpaperType wallpaperType)
        {
            var filterString = new StringBuilder();
            var selection = FileFilter.LivelySupportedFormats.First(x => x.Type == wallpaperType);
            filterString.Append(LocalizeWallpaperCategory(selection.Type)).Append('\0');
            foreach (var extension in selection.Extentions)
            {
                filterString.Append('*').Append(extension).Append(';');
            }
            filterString.Remove(filterString.Length - 1, 1).Append('\0');
            filterString.Remove(filterString.Length - 1, 1).Append('\0');
            return filterString.ToString();
        }

        public static string FileDialogFilterAllNative(bool anyFile = false)
        {
            var filterString = new StringBuilder();
            if (anyFile)
            {
                filterString.Append(resourceLoader.GetString("TextAllFiles")).Append("\0*.*\0");
            }
            foreach (var item in FileFilter.LivelySupportedFormats)
            {
                filterString.Append(LocalizeWallpaperCategory(item.Type)).Append('\0');
                foreach (var extension in item.Extentions)
                {
                    filterString.Append('*').Append(extension).Append(';');
                }
                filterString.Remove(filterString.Length - 1, 1).Append('\0');
            }
            filterString.Remove(filterString.Length - 1, 1).Append('\0');
            return filterString.ToString();
        }
    }
}
