using livelywpf.Helpers;
using livelywpf.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace livelywpf.Views.Dialogues
{
    /// <summary>
    /// Interaction logic for MultiWallpaperImport.xaml
    /// </summary>
    public partial class MultiWallpaperImport : Window
    {
        public MultiWallpaperImport(List<string> paths)
        {
            InitializeComponent();
            var vm = new MultiWallpaperImportViewModel(paths);
            this.DataContext = vm;
            this.Closing += vm.OnWindowClosing;
            if (vm.CloseAction == null)
                vm.CloseAction = new Action(this.Close);
        }
    }
}
