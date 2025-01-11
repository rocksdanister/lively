using Lively.UI.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Lively.UI.WinUI.Views.Pages.Settings
{
    public sealed partial class SettingsWallpaperView : Page
    {
        private readonly SettingsWallpaperViewModel viewModel;

        public SettingsWallpaperView()
        {
            this.InitializeComponent();
            viewModel = App.Services.GetRequiredService<SettingsWallpaperViewModel>();
            this.DataContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsWallpaperViewModel.IsAppMusicExclusionRuleChanged) && viewModel.IsAppMusicExclusionRuleChanged)
            {
                // Delay for the infobar show animation
                await Task.Delay(500);
                ScrollElementIntoView(musicWallpaperRestartNotify);
            }
        }

        public void ScrollElementIntoView(UIElement element)
        {
            var transform = element.TransformToVisual(scrollViewer.Content as UIElement);
            var elementBounds = transform.TransformBounds(new (new Point(0, 0), element.RenderSize));

            scrollViewer.ChangeView(0, elementBounds.Top, null);
        }
    }
}
