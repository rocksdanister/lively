using System;
using System.Collections.Generic;
using livelywpf.Helpers;

namespace livelywpf
{
    class MultiWallpaperImportModel
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public WallpaperType Type { get; set; }
        public string LocalizedType { get; set; }
        public MultiWallpaperImportModel(string path, WallpaperType type)
        {
            this.Path = path;
            this.FileName = System.IO.Path.GetFileName(path);
            this.Type = type;
            this.LocalizedType = FileFilter.GetLocalisedWallpaperTypeText(type);
        }
    }
}
