using Lively.Common.Services;
using Lively.Models.Enums;
using Microsoft.Windows.Globalization;
using System;
using System.Globalization;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

namespace Lively.UI.WinUI.Services
{
    //Ref: https://docs.microsoft.com/en-us/windows/uwp/app-resources/localize-strings-ui-manifest
    public class ResourceService : IResourceService
    {
        public event EventHandler<string> CultureChanged;
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
            // Setting is persisted between sessions (?.)
            // Ref: https://learn.microsoft.com/en-us/uwp/api/windows.globalization.applicationlanguages.primarylanguageoverride?view=winrt-26100
            if (string.Equals(name, ApplicationLanguages.PrimaryLanguageOverride, StringComparison.OrdinalIgnoreCase))
                return;

            // Issues:
            // Setting String.Empty (default) is giving error.
            // ApplicationLanguages.Languages list is not ordered, ref: https://github.com/microsoft/microsoft-ui-xaml/issues/10075
            name = string.IsNullOrEmpty(name) ? CultureInfo.CurrentUICulture.Name : name;
            ApplicationLanguages.PrimaryLanguageOverride = name;
            // Issue:
            // To update GetString().
            // https://github.com/microsoft/WindowsAppSDK/issues/3052
            // https://github.com/microsoft/WindowsAppSDK/issues/2806
            ResourceContext.SetGlobalQualifierValue("Language", name);

            CultureChanged?.Invoke(this, name);
        }

        public void SetSystemDefaultCulture()
        {
            SetCulture(string.Empty);
        }
    }
}
