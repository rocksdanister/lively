using ModernWpf.Media.Animation;
using System;
using System.Windows;

namespace livelywpf.Cef 
{
    /// <summary>
    /// Interaction logic for LivelyPropertiesTrayWidget.xaml
    /// </summary>
    public partial class LivelyPropertiesTrayWidget : Window
    {
        public LivelyPropertiesTrayWidget(LibraryModel data, string livelyPropertyPath)
        {
            InitializeComponent();
            //top-right location.
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Height = (int)(SystemParameters.WorkArea.Height / 1.1f);
            this.Top = SystemParameters.WorkArea.Bottom - this.Height - 10;
            this.Left = SystemParameters.WorkArea.Right - this.Width - 5;
            //this.Opacity = opacity;

            ContentFrame.Navigate(new Cef.LivelyPropertiesView(data, livelyPropertyPath), new SuppressNavigationTransitionInfo());
        }
    }
}
