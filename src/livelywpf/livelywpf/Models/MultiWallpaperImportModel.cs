using livelywpf.Helpers.Files;

namespace livelywpf.Models
{
    class MultiWallpaperImportModel : IMultiWallpaperImportModel
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public WallpaperType Type { get; set; }
        public string LocalizedType { get; set; }
        public MultiWallpaperImportModel(string path, WallpaperType type, int id)
        {
            this.Path = path;
            this.FileName = System.IO.Path.GetFileName(path);
            this.Type = type;
            this.LocalizedType = FileFilter.GetLocalisedWallpaperTypeString(type);
            this.Id = id;
        }
    }
}
