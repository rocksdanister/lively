using Lively.Models;

namespace Lively.Models
{
    public interface IScreenLayoutModel
    {
        string LivelyPropertyPath { get; set; }
        DisplayMonitor Screen { get; set; }
        string ScreenImagePath { get; set; }
        string ScreenTitle { get; set; }
    }
}