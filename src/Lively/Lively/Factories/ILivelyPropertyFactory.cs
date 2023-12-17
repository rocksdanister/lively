﻿using Lively.Common;
using Lively.Models;
using Lively.Services;

namespace Lively.Factories
{
    public interface ILivelyPropertyFactory
    {
        string CreateLivelyPropertyFolder(LibraryModel model, DisplayMonitor display, WallpaperArrangement arrangement, IUserSettingsService userSettings);
    }
}