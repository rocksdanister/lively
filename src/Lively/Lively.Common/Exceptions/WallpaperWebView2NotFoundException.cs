using System;

namespace Lively.Common.Exceptions;

public class WallpaperWebView2NotFoundException : Exception
{
    public WallpaperWebView2NotFoundException()
    {
    }

    public WallpaperWebView2NotFoundException(string message)
        : base(message)
    {
    }

    public WallpaperWebView2NotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
