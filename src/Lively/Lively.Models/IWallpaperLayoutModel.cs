namespace Lively.Models
{
    public interface IWallpaperLayoutModel
    {
        DisplayMonitor Display { get; set; }
        string LivelyInfoPath { get; set; }
    }
}