using System;

namespace Lively.Models.Exceptions;

public class WallpaperPluginNotFoundException : Exception
{
    public WallpaperPluginNotFoundException()
    {
    }

    public WallpaperPluginNotFoundException(string message)
        : base(message)
    {
    }

    public WallpaperPluginNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
