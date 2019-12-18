using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static livelywpf.SaveData;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for DisplaySelectWindow.xaml
    /// </summary>
    public partial class DisplaySelectWindow : Window
    { 
        private ObservableCollection<DisplayListBox> displayLBItems = new ObservableCollection<DisplayListBox>();

        List<DisplayID> displayIDWindows = new List<DisplayID>();
        public static string selectedDisplay = null;
        public DisplaySelectWindow()
        {
            InitializeComponent();
            selectedDisplay = null;

            DisplayLB.ItemsSource = displayLBItems;
            UpdateDisplayListBox();

            //screen identification.
            foreach (var item in System.Windows.Forms.Screen.AllScreens)
            {
                DisplayID id = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                {
                    //does not work properly in different dpi situations, using setwindowpos instead.
                    //Left = item.Bounds.X,
                    //Top = item.Bounds.Y
                };
                id.Show();
                displayIDWindows.Add(id);
            }

        }

        private void UpdateDisplayListBox()
        {
            string filePath;
            int i;
            foreach (var scr in System.Windows.Forms.Screen.AllScreens)
            {
                filePath = null;
                if ((i = SetupDesktop.wallpapers.FindIndex(x => x.DeviceName == scr.DeviceName)) != -1)
                {
                    filePath = SetupDesktop.wallpapers[i].FilePath;
                }
                displayLBItems.Add(new DisplayListBox(scr.DeviceName, filePath));
            }
        }

        private void DisplayLB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DisplayLB.SelectedIndex == -1)
                return;

            selectedDisplay = displayLBItems[DisplayLB.SelectedIndex].DisplayDevice;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var item in displayIDWindows)
            {
                item.Close();
            }
        }

    }
}
