﻿using Lively.Core;
using Lively.Models;
using Lively.Services;
using System;
using System.Drawing;

namespace Lively.Factories
{
    public interface IWallpaperPluginFactory
    {
        IWallpaper CreateWallpaper(LibraryModel model, DisplayMonitor display, IUserSettingsService userSettings, bool isPreview = false);
        IWallpaper CreateDwmThumbnailWallpaper(LibraryModel model, IntPtr thumbnailSrc, Rectangle targetRect, DisplayMonitor display);
    }
}