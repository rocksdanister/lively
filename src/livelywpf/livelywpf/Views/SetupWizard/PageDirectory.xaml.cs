using System;
using System.Collections.Generic;
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

namespace livelywpf.Views.SetupWizard
{
    /// <summary>
    /// Interaction logic for PageDirectory.xaml
    /// </summary>
    public partial class PageDirectory : Page
    {
        public PageDirectory()
        {
            InitializeComponent();
            this.DataContext = Program.SettingsVM;
        }
    }
}
