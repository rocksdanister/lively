using System;

namespace Lively.Models.Exceptions;

public class WallpaperPluginException : Exception
{
    public WallpaperPluginException()
    {
    }

    public WallpaperPluginException(string message)
        : base(message)
    {
    }

    public WallpaperPluginException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
