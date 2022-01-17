using Lively.UI.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Lively.UI.Wpf.Views.Pages
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
        }
    }
}
