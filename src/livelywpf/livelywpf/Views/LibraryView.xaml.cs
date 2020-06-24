using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : System.Windows.Controls.Page
    {
        public LibraryViewModel LibraryVM { get; set; }
        livelygrid.LivelyGridView LivelyGridControl { get; set; }

        public LibraryView()
        {
            InitializeComponent();
            LibraryVM = new LibraryViewModel();
            this.DataContext = LibraryVM; //uwp control also gets binded..
        }

        private void LivelyGridView_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            LivelyGridControl = windowsXamlHost.GetUwpInternalObject() as global::livelygrid.LivelyGridView;

            if (LivelyGridControl != null)
            {
                //LivelyGridControl.LivelyGrid.SelectionChanged += LivelyGrid_SelectionChanged;
                //LivelyGridControl.GridElementSize(livelygrid.GridSize.Small);
            }
        }

        private async void LivelyGrid_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            var gridView = sender as Windows.UI.Xaml.Controls.GridView;

            ContentDialog noWifiDialog = new ContentDialog
            {
                //Title = LibraryVM[gridView.SelectedIndex].Title,
                //Content = LibraryVM[gridView.SelectedIndex].Desc,
                PrimaryButtonText = "Set as Wallpaper",
                CloseButtonText = "Cancel"
            };

            // Use this code to associate the dialog to the appropriate AppWindow by setting
            // the dialog's XamlRoot to the same XamlRoot as an element that is already present in the AppWindow.
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                //note: can still select the suggestbox, how to add multiple roots?
                noWifiDialog.XamlRoot = gridView.XamlRoot;
            }

            ContentDialogResult result = await noWifiDialog.ShowAsync();
        }
    }
}
