using Lively.Common;
using Lively.Common.Services;
using Lively.Grpc.Client;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutView : Page
    {
        private readonly IAppUpdaterClient appUpdater;
        public AboutView()
        {
            this.InitializeComponent();
            var vm = App.Services.GetRequiredService<AboutViewModel>();
            this.DataContext = vm;
            appUpdater = App.Services.GetRequiredService<IAppUpdaterClient>();
            appUpdater.UpdateChecked += AboutView_UpdateChecked;
            infoBar.Severity = GetSeverity(appUpdater.Status);
            //this.Unloaded += vm.OnWindowClosing;
        }

        private void AboutView_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = this.DispatcherQueue.TryEnqueue(() =>
            {
                infoBar.Severity = GetSeverity(e.UpdateStatus);
            });
        }

        private InfoBarSeverity GetSeverity(AppUpdateStatus status)
        {
            return status switch
            {
                AppUpdateStatus.uptodate => InfoBarSeverity.Informational,
                AppUpdateStatus.available => InfoBarSeverity.Success,
                AppUpdateStatus.invalid => InfoBarSeverity.Error,
                AppUpdateStatus.notchecked => InfoBarSeverity.Warning,
                AppUpdateStatus.error => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Error,
            };
        }

        //Issue: WinUI firing unloaded randomly..
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //appUpdater.UpdateChecked -= AboutView_UpdateChecked;
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e) => LinkHandler.OpenBrowser("https://github.com/rocksdanister");
        private void TwitterButton_Click(object sender, RoutedEventArgs e) => LinkHandler.OpenBrowser("https://twitter.com/rocksdanister");
        private void RedditButton_Click(object sender, RoutedEventArgs e) => LinkHandler.OpenBrowser("https://reddit.com/u/rocksdanister");
        private void YoutubeButton_Click(object sender, RoutedEventArgs e) => LinkHandler.OpenBrowser("https://www.youtube.com/channel/UClep84ofxC41H8-R9UfNPSQ");
        private void EmailButton_Click(object sender, RoutedEventArgs e) => LinkHandler.OpenBrowser("mailto:awoo.git@gmail.com");
    }
}
