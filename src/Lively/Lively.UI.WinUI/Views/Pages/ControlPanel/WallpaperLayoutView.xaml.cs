using Lively.UI.Shared.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lively.UI.WinUI.Views.Pages.ControlPanel
{
    public sealed partial class WallpaperLayoutView : Page
    {
        private WallpaperLayoutViewModel viewModel;

        public WallpaperLayoutView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.viewModel ??= (DataContext as ControlPanelViewModel)?.WallpaperVm;
            this.DataContext = this.viewModel;
        }
    }
}
