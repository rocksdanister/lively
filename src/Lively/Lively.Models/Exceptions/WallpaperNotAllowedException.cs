using System;

namespace Lively.Models.Exceptions;

public class WallpaperNotAllowedException : Exception
{
    public WallpaperNotAllowedException()
    {
    }

    public WallpaperNotAllowedException(string message)
        : base(message)
    {
    }

    public WallpaperNotAllowedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
