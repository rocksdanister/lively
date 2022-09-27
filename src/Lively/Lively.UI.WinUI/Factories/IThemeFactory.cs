using Lively.Models;

namespace Lively.UI.WinUI.Factories
{
    public interface IThemeFactory
    {
        ThemeModel CreateTheme(string themeDir);
        ThemeModel CreateTheme(string filePath, string name, string description);
    }
}