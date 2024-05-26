using Lively.UI.WinUI.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Lively.UI.WinUI.Views.Pages.Settings
{
    public sealed partial class SettingsPerformanceView : Page
    {
        public SettingsPerformanceView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<SettingsPerformanceViewModel>();
        }
    }
}
