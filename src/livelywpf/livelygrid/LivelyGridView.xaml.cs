using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace livelygrid
{
    public enum GridSize
    {
        Small,
        Normal,
        Large
    }

    public sealed partial class LivelyGridView : UserControl
    {
        public ObservableCollection<ViewModel> Items = new ObservableCollection<ViewModel>();
        public GridView LivelyGrid = null;
        public LivelyGridView()
        {
            this.InitializeComponent();
            LivelyGrid = GridControl;
        }

        public void GridElementSize(GridSize gridSize)
        {
            switch (gridSize)
            {
                case GridSize.Small:
                    LivelyGrid.ItemTemplate = (DataTemplate)this.Resources["Small"];
                    break;
                case GridSize.Normal:
                    LivelyGrid.ItemTemplate = (DataTemplate)this.Resources["Normal"];
                    break;
                case GridSize.Large:
                    LivelyGrid.ItemTemplate = (DataTemplate)this.Resources["Large"];
                    break;
                default:
                    LivelyGrid.ItemTemplate = (DataTemplate)this.Resources["Normal"];
                    break;
            }
        }

    }
}
