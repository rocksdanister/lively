using livelywpf.Core;

namespace livelywpf.Models
{
    public interface IWallpaperLayoutModel
    {
        string LivelyInfoPath { get; set; }
        LivelyScreen LivelyScreen { get; set; }
    }
}