using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        Large,
        NoPreview
    }

    public sealed partial class LivelyGridView : UserControl
    {
        public LivelyGridView()
        {
            this.InitializeComponent();
        }

        public void GridElementSize(GridSize gridSize)
        {
            switch (gridSize)
            {
                case GridSize.Small:
                    GridControl.ItemTemplate = (DataTemplate)this.Resources["Small"];
                    break;
                case GridSize.Normal:
                    GridControl.ItemTemplate = (DataTemplate)this.Resources["Normal"];
                    break;
                case GridSize.Large:
                    GridControl.ItemTemplate = (DataTemplate)this.Resources["Large"];
                    break;
                case GridSize.NoPreview:
                    GridControl.ItemTemplate = (DataTemplate)this.Resources["NoPreview"];
                    break;
                default:
                    GridControl.ItemTemplate = (DataTemplate)this.Resources["Normal"];
                    break;
            }
        }
    }
}
