using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Security;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
        private Object selectedTile;
        /// <summary>
        /// Fires when flyoutmenu is clicked, object is datacontext.
        /// </summary>
        public event EventHandler<object> ContextMenuClick;
        public event EventHandler<DragEventArgs> FileDroppedEvent;
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

        private void GridControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            GridView gridView = (GridView)sender;
            contextMenu.ShowAt(gridView, e.GetPosition(gridView));
            var a = ((FrameworkElement)e.OriginalSource).DataContext;
            selectedTile = a;
        }

        private void contextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTile != null)
            {
                ContextMenuClick?.Invoke(sender, selectedTile);
            }
        }

        #region drag and drop

        private void OnFileDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

            if (e.DragUIOverride != null)
            {
                //e.DragUIOverride.Caption = "New Wallpaper";
                e.DragUIOverride.IsCaptionVisible = false;
                e.DragUIOverride.IsContentVisible = true;
            }

            this.AddFilePanel.Visibility = Visibility.Visible;
        }

        private void OnFileDragLeave(object sender, DragEventArgs e)
        {
            this.AddFilePanel.Visibility = Visibility.Collapsed;
        }

        private async void OnFileDrop(object sender, DragEventArgs e)
        {
            FileDroppedEvent?.Invoke(sender, e);
            this.AddFilePanel.Visibility = Visibility.Collapsed;
        }

        #endregion

    }
}
