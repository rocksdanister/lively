namespace Lively.Models.Enums;

/// <summary>
/// Represents suggested stream quality levels.
/// </summary>
public enum StreamQualitySuggestion
{
    /// <summary>
    /// Lowest stream quality (144p).
    /// </summary>
    Lowest,
    /// <summary>
    /// Low stream quality (240p).
    /// </summary>
    Low,
    /// <summary>
    /// Low-medium stream quality (360p).
    /// </summary>
    LowMedium,
    /// <summary>
    /// Medium stream quality (480p).
    /// </summary>
    Medium,
    /// <summary>
    /// Medium-high stream quality (720p).
    /// </summary>
    MediumHigh,
    /// <summary>
    /// High stream quality (1080p).
    /// </summary>
    High,
    /// <summary>
    /// Highest stream quality (1081p).
    /// </summary>
    Highest
}