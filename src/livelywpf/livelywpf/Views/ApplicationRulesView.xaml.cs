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
using System.Windows.Shapes;

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
