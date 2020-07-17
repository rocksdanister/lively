using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace livelywpf.Views.ScreenRecordlibVLC
{
    /// <summary>
    /// Interaction logic for ScreenRecordFloatingControl.xaml
    /// </summary>
    public partial class ScreenRecordFloatingControl : Window
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private bool _recordStarted = false;
        public event EventHandler<bool> RecordEvent;
        int elapsedTime = 0;

        public ScreenRecordFloatingControl(double left, double top)
        {
            InitializeComponent();
            this.Left = left;
            this.Top = top;
            this.Topmost = true;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            elapsedTime++;
            var span = TimeSpan.FromSeconds(elapsedTime); 
            var time = string.Format("{0}:{1:00}",
                                        (int)span.TotalMinutes,
                                        span.Seconds);
            timerText.Text = time;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_recordStarted == false)
            {
                timerText.Text = "0:00";
                recordBtn.Content = "Stop";
                recordBtn.Background = Brushes.Red;
                _recordStarted = true;
                RecordEvent?.Invoke(this, true);
                dispatcherTimer.Start();
            }
            else
            {
                dispatcherTimer.Stop();
                RecordEvent?.Invoke(this, false);
                this.Close();
            }
        }
    }
}
