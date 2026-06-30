using Lively.Models.Enums;
using System.Drawing;

namespace Lively.Services.Taskbar
{
    internal static class TaskbarThemeColor
    {
        public static uint ToAbgr(byte alpha, Color color)
        {
            return ((uint)alpha << 24) | ((uint)color.B << 16) | ((uint)color.G << 8) | color.R;
        }

        public static uint ToUtilityColor(Color color)
        {
            return ToAbgr(0, color) & 0x00FFFFFFu;
        }

        public static uint ToLegacyGradientColor(TaskbarThemeState state)
        {
            return state.Theme switch
            {
                TaskbarTheme.clear => 16777215, // 00FFFFFF
                TaskbarTheme.blur => 0,
                TaskbarTheme.fluent => 167772160, // 0A000000
                TaskbarTheme.color => ToAbgr(200, state.AccentColor),
                TaskbarTheme.wallpaper => ToAbgr(200, state.AccentColor),
                TaskbarTheme.wallpaperFluent => ToAbgr(125, state.AccentColor),
                _ => 0,
            };
        }
    }
}
