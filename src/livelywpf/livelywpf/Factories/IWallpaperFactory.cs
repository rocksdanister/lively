using livelywpf.Core;
using livelywpf.Models;
using livelywpf.Services;

namespace livelywpf.Factories
{
    public interface IWallpaperFactory
    {
        IWallpaper CreateWallpaper(ILibraryModel model, ILivelyScreen display, IUserSettingsService userSettings, bool isPreview = false);
    }
}