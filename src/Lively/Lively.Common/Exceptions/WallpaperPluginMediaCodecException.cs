using System;

namespace Lively.Common.Exceptions;

/// <summary>
/// Windows N/KN codec missing.
/// </summary>
public class WallpaperPluginMediaCodecException : Exception
{
    public WallpaperPluginMediaCodecException()
    {
    }

    public WallpaperPluginMediaCodecException(string message)
        : base(message)
    {
    }

    public WallpaperPluginMediaCodecException(string message, Exception inner)
        : base(message, inner)
    {
    }
}