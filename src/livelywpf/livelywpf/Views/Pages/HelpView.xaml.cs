using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace livelywpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for HelpView.xaml
    /// </summary>
    public partial class HelpView : Page
    {
        public HelpView()
        {
            InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<HelpViewModel>();
            //storePanel.Visibility = Program.IsMSIX ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HelpPageHost_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            global::livelyPages.HelpPage userControl =
                windowsXamlHost.GetUwpInternalObject() as global::livelyPages.HelpPage;

            if (userControl != null)
            {
                userControl.UIText = new livelyPages.HelpPage.LocalizeText()
                {
                    TitleWebsite = Properties.Resources.TextWebsite,
                    TitleCommunity = Properties.Resources.TitleCommunity,
                    TitleDocumentation = Properties.Resources.TitleDocumentation,
                    TitleReportBug = Properties.Resources.TitleReportBug,
                    TitleSourceCode = Properties.Resources.TitleSourceCode,
                    TitleStoreReview = Properties.Resources.TitleStore,
                    DescStoreReview = Properties.Resources.TextStoreReview,
                    DescWebsite = Properties.Resources.DescOfficialWebpage,
                    DescReportBug = Properties.Resources.DescReportBug,
                    DescSourceCode = Properties.Resources.TextGitHubStar,
                    TitleSupport = Properties.Resources.TextSupport,
                    DescSupport = Properties.Resources.DescSupperDev,
                    DescCommunity = Properties.Resources.DescCommunity,
                    DescDocumentation = Properties.Resources.DescDocumentation,
                };
            }
        }
    }
}
