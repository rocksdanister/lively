using livelywpf.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for MultiWallpaperImport.xaml
    /// </summary>
    public partial class MultiWallpaperImport : Window
    {
        public MultiWallpaperImport(List<string> paths)
        {
            InitializeComponent();
            if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
            {
                //multi import is not supported for duplicate layout..
                SetupDesktop.TerminateAllWallpapers();
                Program.SettingsVM.Settings.WallpaperArrangement = WallpaperArrangement.per;
                Program.SettingsVM.UpdateConfigFile();
            }
            var vm = new MultiWallpaperImportViewModel(paths);
            this.DataContext = vm;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SetupDesktop.TerminateAllWallpapers();
        }
    }
}
