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
            this.Closing += Program.AppRulesVM.OnWindowClosing;
        }
    }
}
