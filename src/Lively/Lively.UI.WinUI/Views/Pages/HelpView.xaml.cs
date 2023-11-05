using Lively.Common;
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

namespace Lively.UI.WinUI.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HelpView : Page
    {
        public HelpView()
        {
            this.InitializeComponent();
        }

        private void WebsiteCard_Click(object sender, RoutedEventArgs e) => LinkUtil.OpenBrowser("https://www.rocksdanister.com/lively/");

        private void DocumentationCard_Click(object sender, RoutedEventArgs e) => LinkUtil.OpenBrowser("https://github.com/rocksdanister/lively/wiki");

        private void CommunityCard_Click(object sender, RoutedEventArgs e) => LinkUtil.OpenBrowser("https://www.reddit.com/r/LivelyWallpaper/");

        private void SoureCodeCard_Click(object sender, RoutedEventArgs e) => LinkUtil.OpenBrowser("https://github.com/rocksdanister/lively");

        private void ReportBugCard_Click(object sender, RoutedEventArgs e) => LinkUtil.OpenBrowser("https://github.com/rocksdanister/lively/wiki/Common-Problems");
    }
}
