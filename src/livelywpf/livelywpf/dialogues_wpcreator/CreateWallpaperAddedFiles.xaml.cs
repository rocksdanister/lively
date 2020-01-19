using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for CreateWallpaperAddedFiles.xaml
    /// </summary>
    public partial class CreateWallpaperAddedFiles : MetroWindow
    {
        public CreateWallpaperAddedFiles(List<string> files)
        {
            InitializeComponent();
            filesListVIew.ItemsSource = files;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            //this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
