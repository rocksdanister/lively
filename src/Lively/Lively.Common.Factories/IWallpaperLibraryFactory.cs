using Lively.Models;

namespace Lively.Helpers
{
    public interface IWallpaperLibraryFactory
    {
        LivelyInfoModel GetMetadata(string folderPath);
        LibraryModel CreateFromDirectory(string folderPath);
        LibraryModel CreateFromMetadata(LivelyInfoModel metadata);
    }
}