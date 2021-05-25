using ModernWpf.Controls;
using System;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Page = System.Windows.Controls.Page;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : Page
    {
        public AboutView()
        {
            InitializeComponent();
            appVersionText.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + 
                (Program.IsTestBuild ? "b": (Program.IsMSIX ? Properties.Resources.TitleStore : string.Empty));
        }

        private void btnLicense_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowDocDialog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "license.rtf"));
        }

        private void btnAttribution_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowDocDialog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "attribution.rtf"));
        }

        private void ShowDocDialog(string docPath)
        {
            var item = new DocView(docPath)
            {
                Title = Properties.Resources.TitleDocumentation,
            };
            if (App.AppWindow.IsVisible)
            {
                item.Owner = App.AppWindow;
                item.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                item.Width = App.AppWindow.Width / 1.2;
                item.Height = App.AppWindow.Height / 1.2;
            }
            else
            {
                item.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            }
            item.ShowDialog();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Helpers.LinkHandler.OpenBrowser(e.Uri);
        }
    }
}
