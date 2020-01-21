using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
    /// Interaction logic for DisplayLayoutWindow.xaml
    /// </summary>
    public partial class DisplayLayoutWindow : MetroWindow
    {
        private ObservableCollection<DisplayListBox> displayLBItems = new ObservableCollection<DisplayListBox>();

        List<DisplayID> displayIDWindows = new List<DisplayID>();
        private MainWindow mainWindow;
        public DisplayLayoutWindow(MainWindow obj)
        {
            InitializeComponent();

            mainWindow = obj;
            if ((int)SaveData.config.WallpaperArrangement >= 0 && (int)SaveData.config.WallpaperArrangement <= 2)
            {
                displayLayoutSelect.SelectedIndex = (int)SaveData.config.WallpaperArrangement;
            }
            else
            {
                SaveData.config.WallpaperArrangement = 0;
                SaveData.SaveConfig();
                displayLayoutSelect.SelectedIndex = 0;
            }
            displayLayoutSelect.SelectionChanged += ComboBox_SelectionChanged;

            DisplayLB.ItemsSource = displayLBItems;
            UpdateDisplayListBox();

            //screen identification
            foreach (var item in System.Windows.Forms.Screen.AllScreens)
            {
                DisplayID id = new DisplayID(item.DeviceName, item.Bounds.X, item.Bounds.Y)
                {
                    //does not work properly in different dpi situations, using setwindowpos instead.
                    //Left = item.Bounds.X,
                    //Top = item.Bounds.Y,
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
                    filePath = SetupDesktop.wallpapers[i].FilePath;// = System.IO.Path.GetFileName(SetupDesktop.wallpapers[i].filePath);
                }
                displayLBItems.Add(new DisplayListBox(scr.DeviceName, filePath));
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (displayLayoutSelect.SelectedIndex == -1)
                return;

            SaveData.config.WallpaperArrangement = (SaveData.WallpaperArrangement) displayLayoutSelect.SelectedIndex;
            SaveData.SaveConfig();

            //close all currently running wp's & reset ui
            SetupDesktop.CloseAllWallpapers();
            foreach (var item in displayLBItems)
            {
                item.FileName = null;
                item.FilePath = null;
            }
        }


        private void ButtonCloseAll_Click(object sender, RoutedEventArgs e)
        {
            SetupDesktop.CloseAllWallpapers();

            foreach (var item in displayLBItems)
            {
                item.FileName = null;
                item.FilePath = null;
            }
            //displayLBItems.Clear();
            //UpdateDisplayListBox();
        }
        private void MenuItem_Close_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayLB.SelectedIndex == -1)
                return;

            SetupDesktop.CloseWallpaper(displayLBItems[DisplayLB.SelectedIndex].DisplayDevice);

            foreach (var item in displayLBItems)
            {
                if(item.DisplayDevice.Equals(displayLBItems[DisplayLB.SelectedIndex].DisplayDevice))
                {
                    item.FileName = null;
                    item.FilePath = null;
                    break;
                }
            }
        }

        private void MenuItem_ShowLocation_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayLB.SelectedIndex == -1)
                return;


            foreach (var item in displayLBItems)
            {
                if (item.DisplayDevice.Equals(displayLBItems[DisplayLB.SelectedIndex].DisplayDevice, StringComparison.Ordinal))
                {
                    if (item.FilePath != null)
                    {
                        if (File.Exists(item.FilePath))
                        {
                            try
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo
                                {
                                    Arguments = "\"" + System.IO.Path.GetDirectoryName(item.FilePath) + "\"",
                                    FileName = "explorer.exe"
                                };
                                Process.Start(startInfo);
                            }
                            catch { }
                        }
                    }
                    break;
                }
            }
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
