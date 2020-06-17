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
            //ShowContentDialog();
        }

        private void SettingsNavView_ChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;

            Windows.UI.Xaml.Controls.NavigationView navView =
                (Windows.UI.Xaml.Controls.NavigationView)windowsXamlHost.Child;

            if (navView != null)
            {
                //navView.OpenPaneLength = 50;
                navView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                navView.SelectionFollowsFocus = NavigationViewSelectionFollowsFocus.Enabled;
                navView.IsPaneToggleButtonVisible = false;
                navView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
                navView.IsSettingsVisible = false;
                navView.MenuItems.Add(CreateMenu("General", "general", Symbol.Go));
                navView.MenuItems.Add(CreateMenu("Performance", "perf", Symbol.Clock));
                navView.MenuItems.Add(CreateMenu("Audio", "audio", Symbol.Audio));
                navView.MenuItems.Add(CreateMenu("Wallpaper", "wallpaper", Symbol.ImportAll));
                //navView.ItemInvoked += NavView_ItemInvoked;
            }
        }

        private Windows.UI.Xaml.Controls.NavigationViewItem CreateMenu(string menuName, string tag, Symbol icon)
        {
            Windows.UI.Xaml.Controls.NavigationViewItem item = new NavigationViewItem
            {
                Name = menuName,
                Content = menuName,
                Tag = tag,
                Icon = new SymbolIcon(icon)
            };
            return item;
        }
    }
}
