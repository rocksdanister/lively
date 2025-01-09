using Lively.Models.Enums;
using System.Collections.Generic;

namespace Lively.Models
{
    public class ScreenSaverLayoutModel
    {
        public WallpaperArrangement Layout { get; set; }
        public List<WallpaperLayoutModel> Wallpapers { get; set; }
    }
}
