using ModernWpf.Media.Animation;
using System;
using System.Windows;
using System.Windows.Interop;
using livelywpf.Core;
using System.Windows.Input;
using livelywpf.Models;

namespace livelywpf.Views.LivelyProperty.Dialogues
{
    /// <summary>
    /// Interaction logic for LivelyPropertiesTrayWidget.xaml
    /// </summary>
    public partial class LivelyPropertiesTrayWidget : Window
    {
        //readonly int xPos = 0, yPos = 0, width = 250, height = 250;
        public LivelyPropertiesTrayWidget(ILibraryModel model)
        {
            InitializeComponent();
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) this.Close(); };

            //top-right location.
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Height = (int)(SystemParameters.WorkArea.Height / 1.1f);
            this.Top = SystemParameters.WorkArea.Bottom - this.Height - 10;
            this.Left = SystemParameters.WorkArea.Right - this.Width - 5;
            this.Title = model.Title;

            //top-right location
            //this.width = (int)this.Width;
            //this.height = (int)(screen.Bounds.Height / 1.1f);
            //xPos = screen.Bounds.Right - this.width - 100;
            //yPos = screen.Bounds.Bottom - this.height - 10;

            ContentFrame.Navigate(new LivelyPropertiesView(model), new SuppressNavigationTransitionInfo());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ignores dpi, this.Left, this.Right is affected by system dpi.
            //NativeMethods.SetWindowPos(new WindowInteropHelper(this).Handle, 1, xPos, yPos, width, height, 0 | 0x0010);
        }
    }
}
