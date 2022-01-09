using Lively.Models;

namespace Lively.UI.Wpf.Models
{
    public interface IScreenLayoutModel
    {
        string LivelyPropertyPath { get; set; }
        IDisplayMonitor Screen { get; set; }
        string ScreenImagePath { get; set; }
        string ScreenTitle { get; set; }
    }
}