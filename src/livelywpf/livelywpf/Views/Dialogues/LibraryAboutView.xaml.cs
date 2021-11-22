using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace livelywpf.Views.Dialogues
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
            Helpers.LinkHandler.OpenBrowser(e.Uri);
        }
    }
}
