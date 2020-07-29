using System.Windows;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for ApplicationRulesView.xaml
    /// </summary>
    public partial class ApplicationRulesView : Window
    {
        public ApplicationRulesView()
        {
            InitializeComponent();
            this.DataContext = Program.AppRulesVM;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Program.AppRulesVM.UpdateDiskFile();
        }

    }
}
