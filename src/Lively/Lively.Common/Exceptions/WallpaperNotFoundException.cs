using System;

namespace Lively.Common.Exceptions;

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
