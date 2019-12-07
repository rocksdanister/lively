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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for CreateWallpaper.xaml
    /// </summary>
    public partial class CreateWallpaper : Window
    {
        public CreateWallpaper()
        {
            InitializeComponent();
     
        }

        private void Tile_Ext_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Hidden;
            Create_Frame.NavigationService.Navigate(new PageZipCreate());
        }

        private void Tile_Creator_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
