using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for DisplayID.xaml
    /// </summary>
    public partial class DisplayID : Window
    {
        private int xPos = 0 , yPos = 0;
        public DisplayID(string id,int xPos, int yPos, bool destroyAfterElapsedTime = false, int elapsedTime = 5)
        {
            InitializeComponent();
            this.xPos = xPos;
            this.yPos = yPos;

            label.Text = id;
            if (destroyAfterElapsedTime)
            {
                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(elapsedTime)
                };
                timer.Tick += TimerTick;
                timer.Start();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            NativeMethods.SetWindowPos(windowHandle, 1, xPos, yPos, 0, 0, 0x0010 | 0x0001); //ignores dpi, this.Left, this.Right is unreliable due to dpi differences.changes ? (SWP_NOACTIVATE,SWP_NOSIZE & Bottommost)
            //NativeMethods.SetWindowPos(windowHandle, 1, 0, 0, 0, 0, 0x0002 | 0x0010 | 0x0001); // SWP_NOMOVE ,SWP_NOACTIVATE,SWP_NOSIZE & Bottom most.
        }

        private void TimerTick(object sender, EventArgs e)
        {       
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            Close();
        }

    }
}
