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

namespace livelywpf.Views.Dialogues
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        public string Result
        {
            get { return txtBox.Text; }
        }

        public TextInputDialog(string msg, string title, string primaryBtnText = "Ok", string secondaryBtnText = "Cancel")
        {
            InitializeComponent();
            lblQtn.Content = msg;
            this.Title = title;
            this.primaryBtn.Content = primaryBtnText;
            this.secondaryBtn.Content = secondaryBtnText;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtBox.Focus();
        }
    }
}
