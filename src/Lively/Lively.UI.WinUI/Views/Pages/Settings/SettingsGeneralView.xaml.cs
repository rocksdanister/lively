using Lively.UI.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Lively.UI.WinUI.Views.Pages.Settings
{
    public sealed partial class SettingsGeneralView : Page
    {
        public SettingsGeneralView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<SettingsGeneralViewModel>();
        }
    }
}
