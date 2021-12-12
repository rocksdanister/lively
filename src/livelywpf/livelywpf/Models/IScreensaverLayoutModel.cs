using livelywpf.Core;

namespace livelywpf.Models
{
    public interface IScreensaverLayoutModel
    {
        WallpaperArrangement Arrangement { get; set; }
        string LivelyInfoPath { get; set; }
        LivelyScreen LivelyScreen { get; set; }
        ScreensaverMode Mode { get; set; }
    }
}