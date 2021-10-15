using livelywpf.Helpers.Pinvoke;
using System;
using System.Windows;
using System.Windows.Interop;

namespace livelywpf.Views.Screen
{
    /// <summary>
    /// Interaction logic for ScreenLabelView.xaml
    /// </summary>
    public partial class ScreenLabelView : Window
    {
        readonly int xPos = 0, yPos = 0;
        public ScreenLabelView(string screenLabel, int posLeft, int posTop)
        {
            InitializeComponent();
            ScreenLabel.Text = screenLabel;
            xPos = posLeft;
            yPos = posTop;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ignores dpi, this.Left, this.Right is affected by system dpi.
            NativeMethods.SetWindowPos(new WindowInteropHelper(this).Handle, 1, xPos, yPos, 0, 0,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE | (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE);
        }
    }
}
