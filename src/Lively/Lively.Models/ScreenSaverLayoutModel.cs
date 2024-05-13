using Lively.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Models
{
    public class ScreenSaverLayoutModel
    {
        public WallpaperArrangement Layout { get; set; }
        public List<WallpaperLayoutModel> Wallpapers { get; set; }
    }
}
