using System;

namespace Lively.Models.Exceptions;

public class WallpaperNotFoundException : Exception
{
    public WallpaperNotFoundException()
    {
    }

    public WallpaperNotFoundException(string message)
        : base(message)
    {
    }

    public WallpaperNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
