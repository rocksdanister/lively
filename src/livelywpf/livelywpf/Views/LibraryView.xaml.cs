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

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : Page
    {
        ObservableCollection<livelygrid.ViewModel> LibraryItems { get; set; }
        public LibraryView()
        {
            InitializeComponent();
        }

        private void LivelyGridView_ChildChanged(object sender, EventArgs e)
        {
            // Hook up x:Bind source.
            global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost windowsXamlHost =
                sender as global::Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost;
            global::livelygrid.LivelyGridView userControl =
                windowsXamlHost.GetUwpInternalObject() as global::livelygrid.LivelyGridView;

            if (userControl != null)
            {
                LibraryItems = new ObservableCollection<livelygrid.ViewModel>();
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title1", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title2", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title3", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title4", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title5", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title6", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                LibraryItems.Add(new livelygrid.ViewModel() { Title = "title7", Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\patrick.gif" });
                userControl.Items = LibraryItems;
            }
        }


    }
}
