using Lively.Common;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lively.UI.Wpf.Views.SetupWizard
{
    /// <summary>
    /// Interaction logic for PageWindowsN.xaml
    /// </summary>
    public partial class PageWindowsN : Page
    {
        public PageWindowsN()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            LinkHandler.OpenBrowser(e.Uri);
        }
    }
}
