using Lively.Common.Services;
using Lively.Models.Enums;
using System;
using Windows.ApplicationModel.Resources;

namespace Lively.UI.WinUI.Services
{
    //Ref: https://docs.microsoft.com/en-us/windows/uwp/app-resources/localize-strings-ui-manifest
    public class ResourceService : IResourceService
    {
        private readonly ResourceLoader resourceLoader;

        public ResourceService()
        {
            //Use GetForViewIndependentUse instead of GetForCurrentView when resolving resources from code as there is no current view in non-packaged scenarios.
            //The following exception occurs if you call GetForCurrentView in non-packaged scenarios: Resource Contexts may not be created on threads that do not have a CoreWindow.
            resourceLoader = ResourceLoader.GetForViewIndependentUse();
        }

        public string GetString(string resource)
        {
            return resourceLoader?.GetString(resource);
        }

        public string GetString(WallpaperType type)
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

        public void SetCulture(string name)
        {
            throw new NotImplementedException();
        }

        public void SetSystemDefaultCulture()
        {
            throw new NotImplementedException();
        }
    }
}
