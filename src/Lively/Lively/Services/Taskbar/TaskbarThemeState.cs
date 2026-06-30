using Lively.Models.Enums;
using System.Drawing;

namespace Lively.Services.Taskbar
{
    internal readonly struct TaskbarThemeState
    {
        public TaskbarThemeState(TaskbarTheme theme, Color accentColor)
        {
            Theme = theme;
            AccentColor = accentColor;
        }

        public TaskbarTheme Theme { get; }
        public Color AccentColor { get; }
    }
}
