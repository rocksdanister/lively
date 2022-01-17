using Lively.Common;
using Lively.UI.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Page = System.Windows.Controls.Page;

namespace Lively.UI.Wpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : Page
    {
        public AboutView()
        {
            InitializeComponent();
            var vm = App.Services.GetRequiredService<AboutViewModel>();
            this.DataContext = vm;
            this.Unloaded += vm.OnViewClosing;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            LinkHandler.OpenBrowser(e.Uri);
        }
    }
}
