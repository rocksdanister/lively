using Lively.Common;
using Lively.Models;
using Lively.Services;

namespace Lively.Factories
{
    public interface ILivelyPropertyFactory
    {
        string CreateLivelyPropertyFolder(ILibraryModel model, IDisplayMonitor display, WallpaperArrangement arrangement, IUserSettingsService userSettings);
    }
}