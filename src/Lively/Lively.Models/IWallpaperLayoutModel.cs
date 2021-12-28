namespace Lively.Models
{
    public interface IWallpaperLayoutModel
    {
        IDisplayMonitor Display { get; set; }
        string LivelyInfoPath { get; set; }
    }
}