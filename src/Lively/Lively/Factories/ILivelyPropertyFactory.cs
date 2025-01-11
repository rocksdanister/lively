using Lively.Common.Services;
using Lively.Models;
using Lively.Models.Enums;

namespace Lively.Factories
{
    public interface ILivelyPropertyFactory
    {
        string CreateLivelyPropertyFolder(LibraryModel model, DisplayMonitor display, WallpaperArrangement arrangement, IUserSettingsService userSettings);
    }
}