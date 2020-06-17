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
        ObservableCollection<livelygrid.ViewModel> LibraryItems { get; set; }
        livelygrid.LivelyGridView LivelyGridControl { get; set; }

        public LibraryView()
        {
            InitializeComponent();
        }

        private void LivelyGridView_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            LivelyGridControl = windowsXamlHost.GetUwpInternalObject() as global::livelygrid.LivelyGridView;

            if (LivelyGridControl != null)
            {
                LibraryItems = new ObservableCollection<livelygrid.ViewModel>();
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title1", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title2", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title3", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title4", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title5", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title6", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title7", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title1", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LivelyGridControl.Items = LibraryItems;
                LivelyGridControl.LivelyGrid.SelectionChanged += LivelyGrid_SelectionChanged;
                //LivelyGridControl.GridElementSize(livelygrid.GridSize.Small);
            }
        }

        private async void LivelyGrid_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            var gridView = sender as Windows.UI.Xaml.Controls.GridView;

            ContentDialog noWifiDialog = new ContentDialog
            {
                Title = LibraryItems[gridView.SelectedIndex].Title,
                Content = LibraryItems[gridView.SelectedIndex].Desc,
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
