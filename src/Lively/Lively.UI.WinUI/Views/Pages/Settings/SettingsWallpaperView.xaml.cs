using Lively.UI.WinUI.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Lively.UI.WinUI.Views.Pages.Settings
{
    public sealed partial class SettingsWallpaperView : Page
    {
        public SettingsWallpaperView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<SettingsWallpaperViewModel>();

        }
    }
}
