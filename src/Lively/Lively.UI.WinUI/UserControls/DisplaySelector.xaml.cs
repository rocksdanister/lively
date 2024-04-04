using Google.Protobuf.WellKnownTypes;
using Lively.Common;
using Lively.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Lively.UI.WinUI.UserControls
{
    public sealed partial class DisplaySelector : UserControl
    {
        public ObservableCollection<ScreenLayoutModel> Displays
        {
            get { return (ObservableCollection<ScreenLayoutModel>)GetValue(DisplaysProperty); }
            set
            {
                // Remove event handler from old collection
                if (Displays is not null)
                    Displays.CollectionChanged -= Displays_CollectionChanged;
                // Subscribe to event handler for new collection
                if (value is not null)
                    value.CollectionChanged += Displays_CollectionChanged;

                SetValue(DisplaysProperty, value);

                UpdateCanvas();
                UpdateDisplaySelection();
            }
        }

        public static readonly DependencyProperty DisplaysProperty =
            DependencyProperty.Register("Displays", typeof(ObservableCollection<ScreenLayoutModel>), typeof(DisplaySelector), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public ScreenLayoutModel SelectedItem
        {
            get { return (ScreenLayoutModel)GetValue(SelectedItemProperty); }
            set 
            {
                SetValue(SelectedItemProperty, value);

                UpdateDisplaySelection();
            }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(ScreenLayoutModel), typeof(DisplaySelector), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public bool IsSelection
        {
            get { return (bool)GetValue(IsSelectionProperty); }
            set { SetValue(IsSelectionProperty, value); }
        }

        public static readonly DependencyProperty IsSelectionProperty =
            DependencyProperty.Register("IsSelection", typeof(bool), typeof(DisplaySelector), new PropertyMetadata(true, OnDependencyPropertyChanged));

        public WallpaperArrangement Layout
        {
            get { return (WallpaperArrangement)GetValue(LayoutProperty); }
            set 
            {
                if (value != Layout && (int)value != -1)
                   SetValue(LayoutProperty, value);

                UpdateCanvas();
                UpdateDisplaySelection();
            }
        }

        public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register("Layout", typeof(WallpaperArrangement), typeof(DisplaySelector), new PropertyMetadata(WallpaperArrangement.per, OnDependencyPropertyChanged));

        private static void OnDependencyPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var obj = s as DisplaySelector;
            if (e.Property == DisplaysProperty)
                obj.Displays = (ObservableCollection<ScreenLayoutModel>)e.NewValue;
            else if (e.Property == SelectedItemProperty)
                obj.SelectedItem = (ScreenLayoutModel)e.NewValue;
            else if (e.Property == LayoutProperty)
                obj.Layout = (WallpaperArrangement)e.NewValue;
            else if (e.Property == IsSelectionProperty)
                obj.IsSelection = (bool)e.NewValue;
        }

        public DisplaySelector()
        {
            this.InitializeComponent();
        }

        private void UpdateCanvas()
        {
            // Control(s) (ActualWidth, ActualHeight) only available once loaded
            if (Displays is null || !Displays.Any() || !this.IsLoaded)
                return;

            switch (Layout)
            {
                case WallpaperArrangement.per:
                    {
                        // Normalize values
                        // Note: It is better to implement auto scaling canvas control instead in the future
                        int totalWidth = Displays.Sum(item => item.Screen.Bounds.Width);
                        int totalHeight = Displays.Sum(item => item.Screen.Bounds.Height);

                        foreach (var item in Displays)
                        {
                            var normalizedBounds = Normalize(item.Screen.Bounds, totalWidth, totalHeight);
                            item.NormalizedBounds = new Rectangle(normalizedBounds.Left, normalizedBounds.Top, normalizedBounds.Width, normalizedBounds.Height);
                        }

                        // Bounds.Left and Right can be negative
                        int minLeft = Displays.Min(item => item.NormalizedBounds.Left);
                        int maxRight = Displays.Max(item => item.NormalizedBounds.Left + item.NormalizedBounds.Width);
                        int minTop = Displays.Min(item => item.NormalizedBounds.Top);
                        int maxBottom = Displays.Max(item => item.NormalizedBounds.Top + item.NormalizedBounds.Height);

                        // Center to canvas
                        double horizontalOffset = (maxRight + minLeft) / 2 - this.ActualWidth / 2;
                        double verticalOffset = (maxBottom + minTop) / 2 - this.ActualHeight / 2;

                        foreach (var item in Displays)
                        {
                            item.NormalizedBounds = new Rectangle(
                                (int)(item.NormalizedBounds.Left - horizontalOffset),
                                (int)(item.NormalizedBounds.Top - verticalOffset),
                                item.NormalizedBounds.Width,
                                item.NormalizedBounds.Height);
                        }
                    }
                    break;
                case WallpaperArrangement.duplicate:
                    {
                        // Normalize values
                        int sampleWidth = 1920;
                        int sampleHeight = 1080;
                        int totalWidth = sampleWidth * Displays.Count;
                        int totalHeight = sampleHeight * Displays.Count;

                        foreach (var item in Displays)
                        {
                            var normalizedBounds = Normalize(new Rectangle(0, 0, sampleWidth, sampleHeight), totalWidth, totalHeight);
                            item.NormalizedBounds = new Rectangle(normalizedBounds.Left, normalizedBounds.Top, normalizedBounds.Width, normalizedBounds.Height);
                        }

                        // Center to canvas
                        int normalizedTotalWidth = Displays[0].NormalizedBounds.Width + (int)(sampleWidth * Displays.Count * 0.01f);
                        int normalizedTotalHeight = Displays[0].NormalizedBounds.Height;
                        double horizontalOffset = this.ActualWidth / 2 - normalizedTotalWidth / 2;
                        double verticalOffset = this.ActualHeight / 2 - normalizedTotalHeight / 2;

                        for (int i = 0; i < Displays.Count; i++)
                        {
                            var item = Displays[i];
                            var allMargin = sampleWidth * i * 0.01f;
                            item.NormalizedBounds = new Rectangle(
                                (int)(item.NormalizedBounds.Left + horizontalOffset + allMargin),
                                (int)(item.NormalizedBounds.Top + verticalOffset + allMargin),
                                item.NormalizedBounds.Width,
                                item.NormalizedBounds.Height);
                        }
                    }
                    break;
                case WallpaperArrangement.span:
                    {
                        // Normalize values
                        int sampleWidth = 1920;
                        int sampleHeight = 1080;
                        int totalWidth = sampleWidth * Displays.Count;
                        int totalHeight = sampleHeight * Displays.Count;

                        for (int i = 0; i < Displays.Count; i++)
                        {
                            var normalizedBounds = Normalize(new Rectangle(sampleWidth * i, 0, sampleWidth, sampleHeight), totalWidth, totalHeight);
                            Displays[i].NormalizedBounds = new Rectangle(normalizedBounds.Left, normalizedBounds.Top, normalizedBounds.Width, normalizedBounds.Height);
                        }

                        // Center to canvas
                        int normalizedTotalWidth = Displays.Sum(item => item.NormalizedBounds.Width) - (int)(Displays.Count * sampleWidth * 0.01f);
                        int normalizedTotalHeight = Displays.Max(item => item.NormalizedBounds.Height);
                        double horizontalOffset = this.ActualWidth / 2 - normalizedTotalWidth / 2;
                        double verticalOffset = this.ActualHeight / 2 - normalizedTotalHeight / 2;

                        for (int i = 0; i < Displays.Count; i++)
                        {
                            var item = Displays[i];
                            var leftMargin = -i * sampleWidth * 0.01f;
                            item.NormalizedBounds = new Rectangle(
                            (int)(item.NormalizedBounds.Left + horizontalOffset + leftMargin),
                            (int)(item.NormalizedBounds.Top + verticalOffset),
                            item.NormalizedBounds.Width,
                            item.NormalizedBounds.Height);
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void Displays_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Call the update methods whenever the collection changes
            UpdateCanvas();
            UpdateDisplaySelection();
        }

        private void UpdateDisplaySelection()
        {
            var isSelectionVisible = Layout switch
            {
                WallpaperArrangement.per => true,
                WallpaperArrangement.span => false,
                WallpaperArrangement.duplicate => false,
                _ => throw new NotImplementedException(),
            };

            foreach (var item in Displays)
                item.IsSelected = isSelectionVisible && item == SelectedItem;
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsSelection || Layout != WallpaperArrangement.per)
                return;

            if (sender is FrameworkElement element && element.DataContext is ScreenLayoutModel screenLayoutModel)
                SelectedItem = screenLayoutModel;
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!IsSelection || Layout != WallpaperArrangement.per)
                return;

            if (sender is Grid grid)
                grid.Background = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemChromeMediumLowColor"]);
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!IsSelection || Layout != WallpaperArrangement.per)
                return;

            if (sender is Grid grid)
                grid.Background = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemChromeLowColor"]);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvas();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCanvas();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            //TODO: Unsubcribe Grid_PointerPressed().. ?
            //TODO: Unsub CollectionChanged event ViewModel ?
        }

        private static Rectangle Normalize(Rectangle rect, int maxWidth, int maxHeight, int newMaxValue = 300)
        {
            int largerDimension = Math.Max(maxWidth, maxHeight);

            double normalizedLeft = (double)rect.Left / largerDimension;
            double normalizedTop = (double)rect.Top / largerDimension;
            double normalizedWidth = (double)rect.Width / largerDimension;
            double normalizedHeight = (double)rect.Height / largerDimension;

            int left = (int)(normalizedLeft * newMaxValue);
            int top = (int)(normalizedTop * newMaxValue);
            int width = (int)(normalizedWidth * newMaxValue);
            int height = (int)(normalizedHeight * newMaxValue);

            return new Rectangle(left, top, width, height);
        }
    }
}
