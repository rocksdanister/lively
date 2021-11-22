using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

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
            //storePanel.Visibility = Constants.ApplicationType.IsMSIX ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
