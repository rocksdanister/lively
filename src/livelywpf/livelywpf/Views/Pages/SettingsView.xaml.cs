using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace livelywpf.Views.Pages
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : System.Windows.Controls.Page
    {
        public SettingsView()
        {
            InitializeComponent();
            //SettingsViewModel vm = new SettingsViewModel();
            this.DataContext = App.Services.GetRequiredService<SettingsViewModel>();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Helpers.LinkHandler.OpenBrowser(e.Uri);
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }
    }
}
