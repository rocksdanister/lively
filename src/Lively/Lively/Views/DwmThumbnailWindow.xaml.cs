using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;

namespace Lively.Views
{
    public partial class DwmThumbnailWindow : Window
    {
        public bool AutoSizeDwmWindow { get; set; }
        public event EventHandler InputReceived;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly Rectangle rectSrc, rectDest;
        private readonly IntPtr thumbnailSrc;
        private DwmThumbnailWrapper dwmThumbnail;

        public DwmThumbnailWindow(IntPtr thumbnailSrc, Rectangle rectSrc, Rectangle rectDest)
        {
            InitializeComponent();
            this.rectSrc = rectSrc;
            this.rectDest = rectDest;
            this.thumbnailSrc = thumbnailSrc;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            dwmThumbnail = new DwmThumbnailWrapper(thumbnailSrc, windowHandle);
            // Resize taking display scaling into account
            if (AutoSizeDwmWindow && !NativeMethods.SetWindowPos(
                       windowHandle,
                       -1, //topmost
                       rectSrc.Left,
                       rectSrc.Top,
                       rectSrc.Width,
                       rectSrc.Height,
                       0x0040))
            {
                Logger.Error(LogUtil.GetWin32Error("Window resize fail"));
            }

            if (dwmThumbnail.TryShow())
            {
                dwmThumbnail.Update(rectSrc, rectDest);
            }

            // To prevent PreviewMouseMove firing immediately
            await Task.Delay(500);
            this.PreviewMouseMove += Window_PreviewMouseMove;
            this.PreviewMouseDown += Window_PreviewMouseDown;
            this.PreviewKeyDown += Window_PreviewKeyDown;
            this.PreviewTouchDown += Window_PreviewTouchDown;
            this.PreviewMouseWheel += Window_PreviewMouseWheel;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            dwmThumbnail?.Dispose();
            dwmThumbnail = null;
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            InputReceived?.Invoke(this, e);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            InputReceived?.Invoke(this, e);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            InputReceived?.Invoke(this, e);
        }

        private void Window_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            InputReceived?.Invoke(this, e);
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            InputReceived?.Invoke(this, e);
        }
    }
}
