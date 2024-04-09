using Lively.Common;
using Lively.Models;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Services
{
    public interface IDialogService
    {
        Task ShowHelpDialogAsync();
        Task ShowControlPanelDialogAsync();
        Task ShowAboutDialogAsync();
        Task<DisplayMonitor> ShowDisplayChooseDialogAsync();
        Task<ApplicationModel> ShowApplicationPickerDialogAsync();
        Task ShowDialogAsync(string message, string title, string primaryBtnText);
        Task<DialogResult> ShowDialogAsync(object content,
            string title,
            string primaryBtnText,
            string secondaryBtnText,
            bool isDefaultPrimary = true);
        Task<string> ShowTextInputDialogAsync(string title);
        Task ShowThemeDialogAsync();
        Task ShowPatreonSupportersDialogAsync();
        Task<LibraryModel> ShowDepthWallpaperDialogAsync(string imagePath);
        Task<WallpaperCreateType?> ShowWallpaperCreateDialogAsync(string filePath);
        Task<WallpaperCreateType?> ShowWallpaperCreateDialogAsync();
        public enum DialogResult
        {
            none,
            primary,
            seconday
        }
    }
}