using Lively.Core;
using Lively.Models;
using Lively.Services;

namespace Lively.Factories
{
    public interface IWallpaperFactory
    {
        IWallpaper CreateWallpaper(ILibraryModel model, IDisplayMonitor display, IUserSettingsService userSettings, bool isPreview = false);
    }
}