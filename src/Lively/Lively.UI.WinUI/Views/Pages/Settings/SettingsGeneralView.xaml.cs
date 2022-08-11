using Lively.Common;
using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsGeneralView : Page
    {
        private readonly FontIcon[] audioIcons =
{
            new FontIcon(){ Glyph = "\uE74F" },
            new FontIcon(){ Glyph = "\uE992" },
            new FontIcon(){ Glyph = "\uE993" },
            new FontIcon(){ Glyph = "\uE994" },
            new FontIcon(){ Glyph = "\uE995" },
        };

        public SettingsGeneralView()
        {
            this.InitializeComponent();
            var settings = App.Services.GetRequiredService<SettingsViewModel>();
            this.DataContext = settings;
            UpdateAudioSliderIcon(settings.GlobalWallpaperVolume);
        }

        private void UpdateAudioSliderIcon(double volume) => 
            audioBtn.Icon = audioIcons[(int)Math.Ceiling((audioIcons.Length - 1) * volume / 100)];

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) => UpdateAudioSliderIcon(e.NewValue);

        //NavigateUri not working, Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/5630
        private void Color_HyperlinkButton_Click(object sender, RoutedEventArgs e) => LinkHandler.OpenBrowser("ms-settings:colors");

        private void Language_HyperlinkButton_Click(object sender, RoutedEventArgs e) => LinkHandler.OpenBrowser("https://rocksdanister.github.io/lively/translation/");
    }
}
