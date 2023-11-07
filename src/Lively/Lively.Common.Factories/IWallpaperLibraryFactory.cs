using Lively.Models;

namespace Lively.Helpers
{
    public interface IWallpaperLibraryFactory
    {
        LibraryModel CreateFromDirectory(string folderPath);
        LibraryModel CreateFromMetadata(LivelyInfoModel metadata);
    }
}