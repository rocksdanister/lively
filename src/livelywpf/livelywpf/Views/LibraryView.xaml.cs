using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : System.Windows.Controls.Page
    {
        //public LibraryViewModel LibraryVM { get; set; }
        livelygrid.LivelyGridView LivelyGridControl { get; set; }
        //LibraryViewModel LibraryVM = new LibraryViewModel();
        public LibraryView()
        {
            InitializeComponent();
            //uwp control also gets binded..
            this.DataContext = Program.LibraryVM; 
        }

        private void LivelyGridView_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            LivelyGridControl = windowsXamlHost.GetUwpInternalObject() as global::livelygrid.LivelyGridView;

            if (LivelyGridControl != null)
            {
                LivelyGridControl.GridElementSize((livelygrid.GridSize)Program.SettingsVM.SelectedTileSizeIndex);
                LivelyGridControl.ContextMenuClick += LivelyGridControl_ContextMenuClick;
            }
        }

        /// <summary>
        /// Not possible to do direct mvvm currently, putting the contextmenu inside datatemplate works but.. 
        /// the menu is opening only when right clicking on the DataTemplate content which is not covering completely the GridViewItem.
        /// So the workaround I did is set it outside of template and the datacontext is calculated in code behind.
        /// ref: https://github.com/microsoft/microsoft-ui-xaml/issues/911
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LivelyGridControl_ContextMenuClick(object sender, object e)
        {
            var s = sender as MenuFlyoutItem;
            var obj = (LibraryModel)e;
            Debug.WriteLine(obj.Title);
            switch (s.Name)
            {
                case "showOnDisk":
                    Program.LibraryVM.WallpaperShowOnDisk(e);
                    break;
                case "setWallpaper":
                    Program.LibraryVM.WallpaperSet(e);
                    break;
                case "exportWallpaper":
                    string savePath = "";
                    var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = "Select location to save the file",
                        Filter = "Lively/zip file|*.zip",
                        FileName = ((LibraryModel)e).Title,
                    };
                    if (saveFileDialog1.ShowDialog() == true)
                    {
                        savePath = saveFileDialog1.FileName;
                    }
                    if (String.IsNullOrEmpty(savePath))
                    {
                        break;
                    }
                    Program.LibraryVM.WallpaperExport(e, savePath);
                    break;
                case "deleteWallpaper":
                    Program.LibraryVM.WallpaperDelete(e);
                    break;
                case "customiseWallpaper":
                    //todo: send display info.
                    Program.LibraryVM.WallpaperSendMsg(e, "lively-customise ");
                    break;
            }
        }

        /*
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
        */

        private void Page_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //stop rendering previews..
            LivelyGridControl.GridElementSize(livelygrid.GridSize.NoPreview);
        }
    }
}
