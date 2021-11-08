using livelywpf.Core;
using livelywpf.Models;

namespace livelywpf.Factories
{
    public interface ILivelyPropertyFactory
    {
        string CreateLivelyPropertyFolder(ILibraryModel model, ILivelyScreen display, WallpaperArrangement arrangement);
    }
}