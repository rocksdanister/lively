using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for WebView2Element.xaml
    /// </summary>
    public partial class WebView2Element : Window
    {
        //ref: https://docs.microsoft.com/en-us/microsoft-edge/webview2/gettingstarted/wpf
        private readonly string path;
        public WebView2Element(string url)
        {
            path = url;
            InitializeComponent();
            webView.EnsureCoreWebView2Async();
            this.Loaded += WebView2Element_Loaded;
        }

        private void WebView2Element_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }

        private void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            //todo: lively properties.
            //ref: https://docs.microsoft.com/en-us/microsoft-edge/webview2/reference/wpf/0-9-515/microsoft-web-webview2-wpf-webview2#executescriptasync
        }

        private void webView_CoreWebView2Ready(object sender, EventArgs e)
        {
            //todo: 
            //link checking
            //cross-origin request fix for disk files.
            webView.CoreWebView2.Navigate(path);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(webView != null)
            {
                try
                {
                    webView.Dispose();
                }
                catch { }
            }
        }
    }
}
