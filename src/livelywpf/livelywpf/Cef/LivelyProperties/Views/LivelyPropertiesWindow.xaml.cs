using System;
using System.Windows;
using livelywpf.Core;
using ModernWpf.Media.Animation;

namespace livelywpf.Cef
{
    /// <summary>
    /// Interaction logic for LivelyPropertiesWindow.xaml
    /// </summary>
    public partial class LivelyPropertiesWindow : Window
    {
        public LivelyPropertiesWindow(LibraryModel data, string livelyPropertyPath, LivelyScreen screen)
        {
            InitializeComponent();
            ContentFrame.Navigate(new Cef.LivelyPropertiesView(data, livelyPropertyPath, screen), new SuppressNavigationTransitionInfo());
        }
    }
}
