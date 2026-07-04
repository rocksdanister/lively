using Lively.UI.Shared.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace Lively.UI.WinUI.Views.LivelyProperty
{
    public sealed partial class LivelyPropertiesView : Page
    {
        private CustomiseWallpaperViewModel viewModel;

        // Default constructor for Frame.
        public LivelyPropertiesView()
        {
            this.InitializeComponent();
        }

        // Dialog constructor.
        public LivelyPropertiesView(CustomiseWallpaperViewModel viewModel) : this()
        {
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
        }

        // Frame constructor.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.viewModel = e.Parameter as CustomiseWallpaperViewModel;
            this.DataContext = this.viewModel;
        }

        private void SuppressControlWheel(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        //protected override void OnNavigatedFrom(NavigationEventArgs e)
        //{
        //    viewModel.OnClose();
        //}
    }
}
