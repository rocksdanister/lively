using Lively.Common;
using Lively.Grpc.Client;
using Lively.Models.Enums;
using Lively.UI.Shared.ViewModels;
using Lively.UI.WinUI.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Views.Pages
{
    public sealed partial class AppUpdateView : Page
    {
        private readonly AppUpdateViewModel viewModel;

        public AppUpdateView()
        {
            this.InitializeComponent();
            this.viewModel = App.Services.GetRequiredService<AppUpdateViewModel>();
            this.DataContext = this.viewModel;

            // Error when setting in xaml
            WebView.DefaultBackgroundColor = ((SolidColorBrush)App.Current.Resources["ApplicationPageBackgroundThemeBrush"]).Color;
            _ = InitializeWebView2Async();
        }

        private async Task InitializeWebView2Async()
        {
            if (!viewModel.IsWebView2Available)
                return;

            try
            {
                var options = new CoreWebView2EnvironmentOptions();
                var userDataPath = Path.Combine(Constants.CommonPaths.TempWebView2Dir, Assembly.GetExecutingAssembly().GetName().Name);
                var webView2Environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataPath, options);
                await WebView.EnsureCoreWebView2Async(webView2Environment);
            }
            catch (Exception ex)
            {
                viewModel.UpdateChangelogError = $"Exception: {ex.GetType().Name}\nMessage: {ex.Message}";
            }
        }

        // ref: https://github.com/MicrosoftEdge/WebView2Samples
        private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (args.Exception != null)
            {
                viewModel.UpdateChangelogError = $"Exception: {args.Exception.GetType().Name}\nMessage: {args.Exception.Message}";
            }
            else
            {
                // Set website to reflect app interface
                var pageTheme = App.Services.GetRequiredService<IUserSettingsClient>().Settings.ApplicationTheme switch
                {
                    AppTheme.Auto => "auto", // Website handles theme change based on WebView change.
                    AppTheme.Light => "light",
                    AppTheme.Dark => "dark",
                    _ => "auto",
                };
                var accentColorDark1 = ((Windows.UI.Color)App.Current.Resources["SystemAccentColorDark1"]).ToHex().Substring(1);
                var accentColorLight1 = ((Windows.UI.Color)App.Current.Resources["SystemAccentColorLight1"]).ToHex().Substring(1);
                var param = $"?source=app&theme={pageTheme}&colorLight={accentColorLight1}&colorDark={accentColorDark1}";

                var url = viewModel.IsBetaBuild ?
                    $"https://www.rocksdanister.com/lively-webpage/changelog/{param}" :
                    $"https://www.rocksdanister.com/lively/changelog/{param}";
                WebView.Source = LinkUtil.SanitizeUrl(url);

                WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                // Theme need to set css, ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4426
                WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
                // Don't allow contextmenu and devtools
                WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            }
        }

        private void CoreWebView2_NewWindowRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs args)
        {
            // Prevent popups
            if (!args.IsUserInitiated)
                return;

            // Open hyperlinks in default browser
            args.Handled = true;
            LinkUtil.OpenBrowser(args.Uri);
        }

        private void WebView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            // Stay in page
            if (args.IsRedirected)
                args.Cancel = true;
            else
                WebViewProgress.Visibility = Visibility.Visible;
        }

        private void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            WebViewProgress.Visibility = Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundGridShadow.Receivers.Add(BackgroundGrid);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            WebView.Close();
        }
    }
}
