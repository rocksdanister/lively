using livelywpf.Core;

namespace livelywpf.Models
{
    public interface IScreenLayoutModel
    {
        string LivelyPropertyPath { get; set; }
        ILivelyScreen Screen { get; set; }
        string ScreenImagePath { get; set; }
        string ScreenTitle { get; set; }
    }
}