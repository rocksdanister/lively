using Lively.Common;
using Lively.Common.Helpers.Storage;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lively.Helpers
{
    public class WallpaperLibraryFactory : IWallpaperLibraryFactory
    {
        public LibraryModel CreateFromDirectory(string folderPath)
        {
            if (File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
            {
                LivelyInfoModel info = JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(folderPath, "LivelyInfo.json"));
                return info != null ? new LibraryModel(info, folderPath, LibraryItemType.ready, false) : throw new Exception("Corrupted wallpaper metadata");
            }
            throw new FileNotFoundException("Wallpaper not found.");
        }
    }
}