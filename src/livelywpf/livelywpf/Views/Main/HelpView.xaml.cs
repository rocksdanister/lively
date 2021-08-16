using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for HelpView.xaml
    /// </summary>
    public partial class HelpView : Page
    {
        public HelpView()
        {
            InitializeComponent();
            this.DataContext = new HelpViewModel();
            //storePanel.Visibility = Program.IsMSIX ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
