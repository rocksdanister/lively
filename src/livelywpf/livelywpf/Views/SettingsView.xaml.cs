using Microsoft.Toolkit.Wpf.UI.XamlHost;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Xaml.Controls;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : System.Windows.Controls.Page
    {
        public SettingsView()
        {
            InitializeComponent();
            //SettingsViewModel vm = new SettingsViewModel();
            this.DataContext = Program.SettingsVM;
            SetupDesktop.SetWallpaper();
        }

        private void SettingsPageHost_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            global::livelysettings.SettingsPage userControl =
                windowsXamlHost.GetUwpInternalObject() as global::livelysettings.SettingsPage;

            if (userControl != null)
            {

            }
        }
    }
}
