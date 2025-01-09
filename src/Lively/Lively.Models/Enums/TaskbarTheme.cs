namespace Lively.Models.Enums;

public enum TaskbarTheme
{
    /// <summary>
    /// System default theme.
    /// </summary>
    none,
    /// <summary>
    /// Fully transparent taskbar.
    /// </summary>
    clear,
    /// <summary>
    /// Blurred taskbar effect.
    /// </summary>
    blur,
    /// <summary>
    /// Fluent design theme for the taskbar.
    /// </summary>
    fluent,
    /// <summary>
    /// User-defined color for the taskbar.
    /// </summary>
    color,
    /// <summary>
    /// Average color of the live wallpaper applied to the taskbar.
    /// </summary>
    wallpaper,
    /// <summary>
    /// Fluent style using the average color of the live wallpaper.
    /// </summary>
    wallpaperFluent,
}
