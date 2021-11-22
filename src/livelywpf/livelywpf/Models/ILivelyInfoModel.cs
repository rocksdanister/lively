namespace livelywpf.Models
{
    public interface ILivelyInfoModel
    {
        string AppVersion { get; set; }
        string Arguments { get; set; }
        string Author { get; set; }
        string Contact { get; set; }
        string Desc { get; set; }
        string FileName { get; set; }
        bool IsAbsolutePath { get; set; }
        string License { get; set; }
        string Preview { get; set; }
        string Thumbnail { get; set; }
        string Title { get; set; }
        WallpaperType Type { get; set; }
    }
}