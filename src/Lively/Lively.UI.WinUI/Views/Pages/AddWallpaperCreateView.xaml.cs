using Lively.UI.Shared.ViewModels;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddWallpaperCreateView : Page
    {
        public AddWallpaperCreateView(AddWallpaperCreateViewModel vm)
        {
            this.InitializeComponent();
            this.DataContext = vm;
        }
    }
}
