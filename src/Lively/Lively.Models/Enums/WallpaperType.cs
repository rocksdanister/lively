namespace Lively.Models.Enums;

/// <summary>
/// Represents the various types of wallpapers supported.
/// </summary>
public enum WallpaperType
{
    /// <summary>
    /// Application wallpaper.
    /// </summary>
    app,
    /// <summary>
    /// Webpage wallpaper.
    /// </summary>
    web,
    /// <summary>
    /// Webpage with an audio visualizer.
    /// </summary>
    webaudio,
    /// <summary>
    /// Webpage link wallpaper (used in the "Type" tab only, not for the "Library").
    /// </summary>
    url,
    /// <summary>
    /// Bizhawk emulator-based wallpaper.
    /// </summary>
    bizhawk,
    /// <summary>
    /// Unity game wallpaper.
    /// </summary>
    unity,
    /// <summary>
    /// Godot game wallpaper.
    /// </summary>
    godot,
    /// <summary>
    /// Video wallpaper.
    /// </summary>
    video,
    /// <summary>
    /// Animated GIF wallpaper.
    /// </summary>
    gif,
    /// <summary>
    /// Unity-based audio visualizer wallpaper for backward compatibility with Lively pre v1.0.
    /// </summary>
    unityaudio,
    /// <summary>
    /// Video stream wallpaper.
    /// </summary>
    videostream,
    /// <summary>
    /// Static picture wallpaper.
    /// </summary>
    picture,
    /*
    /// <summary>
    /// Animated sequence in HEIC file format (currently commented out).
    /// </summary>
    heic
    */
}
