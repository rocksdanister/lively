using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
namespace livelywpf.Views.SetupWizard
{
    /// <summary>
    /// Interaction logic for PagePleaseWait.xaml
    /// </summary>
    public partial class PagePleaseWait : Page
    {
        public event EventHandler ProcessCompleted;

        public PagePleaseWait()
        {
            InitializeComponent();
            DoStuff();
        }

        private async void DoStuff()
        {
            Program.SettingsVM.Settings.WallpaperBundleVersion = await Task.Run(() => App.ExtractWallpaperBundle());
            Program.SettingsVM.UpdateConfigFile();
            Program.LibraryVM.WallpaperDirectoryUpdate();

            ProcessCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
