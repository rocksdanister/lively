using Lively.Models;
using Lively.Models.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
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

            switch (Displays.Count > 1 ? Layout : WallpaperArrangement.per)
            {
                case WallpaperArrangement.per:
                    {
                        var totalBounds = new Rectangle();
                        foreach (var item in Displays)
                        {
                            totalBounds = Rectangle.Union(totalBounds, item.Screen.Bounds);
                        }
                        int totalWidth = totalBounds.Width;
                        int totalHeight = totalBounds.Height;
                        // Worst case factor + margin
                        var factor = Math.Max(totalHeight / this.ActualHeight, totalWidth / this.ActualWidth) + 2;

                        // Normalize values, alternatively implement auto-scaling Canvas control.
                        foreach (var item in Displays)
                        {
                            item.NormalizedBounds = new Rectangle((int)(item.Screen.Bounds.Left / factor),
                                (int)(item.Screen.Bounds.Top / factor),
                                (int)(item.Screen.Bounds.Width / factor),
                                (int)(item.Screen.Bounds.Height / factor));
                        }
                    }
                    break;
                case WallpaperArrangement.duplicate:
                    {
                        int sampleWidth = 1920;
                        int sampleHeight = 1080;
                        int offsetX = 150;
                        int offsetY = 150;
                        var totalBounds = new Rectangle();
                        // Creating fake display for presentation (overlapped.)
                        for (int i = 0; i < Displays.Count; i++)
                        {
                            var bounds = new Rectangle(offsetX * i, offsetY * i, sampleWidth, sampleHeight);
                            totalBounds = Rectangle.Union(totalBounds, bounds);
                        }
                        int totalWidth = totalBounds.Width;
                        int totalHeight = totalBounds.Height;
                        var factor = Math.Max(totalHeight / this.ActualHeight, totalWidth / this.ActualWidth) + 2;

                        for (int i = 0; i < Displays.Count; i++)
                        {
                            Displays[i].NormalizedBounds = new Rectangle((int)(offsetX * i / factor),
                                (int)(offsetY * i / factor),
                                (int)(sampleWidth / factor),
                                (int)(sampleHeight / factor));
                        }
                    }
                    break;
                case WallpaperArrangement.span:
                    {
                        int sampleWidth = 1920;
                        int sampleHeight = 1080;
                        int offsetX = sampleWidth / 2;
                        int offsetY = 0;
                        var totalBounds = new Rectangle();
                        // Creating fake display for presentation (side by side.)
                        for (int i = 0; i < Displays.Count; i++)
                        {
                            var bounds = new Rectangle(offsetX * i, offsetY * i, sampleWidth, sampleHeight);
                            totalBounds = Rectangle.Union(totalBounds, bounds);
                        }
                        int totalWidth = totalBounds.Width;
                        int totalHeight = totalBounds.Height;
                        var factor = Math.Max(totalHeight / this.ActualHeight, totalWidth / this.ActualWidth) + 2;

                        for (int i = 0; i < Displays.Count; i++)
                        {
                            Displays[i].NormalizedBounds = new Rectangle((int)(offsetX * i / factor),
                                (int)(offsetY * i / factor),
                                (int)(sampleWidth / factor),
                                (int)(sampleHeight / factor));
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
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

        private void Displays_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Call the update methods whenever the collection changes
            UpdateCanvas();
            UpdateDisplaySelection();
        }

        private void UpdateDisplaySelection()
        {
            // Only visual change
            foreach (var item in Displays)
                item.IsSelected = Layout != WallpaperArrangement.per || item == SelectedItem;
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
    }
}
