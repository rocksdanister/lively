using Lively.UI.Shared.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lively.UI.WinUI.Views.Pages.ControlPanel
{
    public sealed partial class ScreensaverLayoutView : Page
    {
        private ScreensaverLayoutViewModel viewModel;

        public ScreensaverLayoutView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.viewModel ??= (DataContext as ControlPanelViewModel)?.ScreensaverVm;
            this.DataContext = this.viewModel;
        }
    }
}
