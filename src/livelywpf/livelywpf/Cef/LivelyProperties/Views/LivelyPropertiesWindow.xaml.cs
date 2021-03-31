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
        public LivelyPropertiesWindow(LibraryModel model)
        {
            InitializeComponent();
            ContentFrame.Navigate(new Cef.LivelyPropertiesView(model), new SuppressNavigationTransitionInfo());
        }
    }
}
