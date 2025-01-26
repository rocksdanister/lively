using Lively.Common.Services;
using Lively.Models.Enums;
using System;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.ApplicationModel.Resources;

namespace Lively.Services
{
    public class ResourceService : IResourceService
    {
        public event EventHandler<string> CultureChanged;

        private readonly ResourceManager resourceManager;

        public ResourceService()
        {
            resourceManager = Properties.Resources.ResourceManager;
        }

        public void SetCulture(string name)
        {
            if (CultureInfo.DefaultThreadCurrentCulture?.Name == name)
                return;

            var culture = string.IsNullOrEmpty(name) ? null : new CultureInfo(name);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            CultureChanged?.Invoke(this, name);
        }

        public void SetSystemDefaultCulture()
        {
            SetCulture(null);
        }

        public string GetString(string resource)
        {
            // Compatibility with UWP .resw shared classes.
            var formattedResource = resource.Replace("/", ".");
            var culture = CultureInfo.DefaultThreadCurrentCulture;
            return culture != null ? 
                resourceManager.GetString(formattedResource, culture) : resourceManager.GetString(formattedResource);
        }

        public string GetString(WallpaperType type)
        {
            return type switch
            {
                WallpaperType.app => resourceManager.GetString("TextApplication"),
                WallpaperType.unity => "Unity",
                WallpaperType.godot => "Godot",
                WallpaperType.unityaudio => "Unity",
                WallpaperType.bizhawk => "Bizhawk",
                WallpaperType.web => resourceManager.GetString("Website/Header"),
                WallpaperType.webaudio => resourceManager.GetString("AudioGroup/Header"),
                WallpaperType.url => resourceManager.GetString("Website/Header"),
                WallpaperType.video => resourceManager.GetString("TextVideo"),
                WallpaperType.gif => "Gif",
                WallpaperType.videostream => resourceManager.GetString("TextWebStream"),
                WallpaperType.picture => resourceManager.GetString("TextPicture"),
                //WallpaperType.heic => "HEIC",
                (WallpaperType)(100) => "Lively Wallpaper",
                _ => resourceManager.GetString("TextError"),
            };
        }
    }
}
