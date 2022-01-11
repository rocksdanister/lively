using Lively.Common;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Lively.UI.Wpf.Views.Dialogues
{
    /// <summary>
    /// Interaction logic for LibraryAboutView.xaml
    /// </summary>
    public partial class LibraryAboutView : Page
    {
        public LibraryAboutView()
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
