namespace livelywpf.Models
{
    interface IMultiWallpaperImportModel
    {
        string FileName { get; set; }
        int Id { get; set; }
        string LocalizedType { get; set; }
        string Path { get; set; }
        WallpaperType Type { get; set; }
    }
}