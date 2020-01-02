using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace livelywpf.Lively.Helpers
{
    public static class WindowOperations
    {

        //https://stackoverflow.com/questions/386731/get-absolute-position-of-element-within-the-window-in-wpf
        /// <summary>
        /// Get UI Framework element position.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="relativeToScreen">false: w.r.t application</param>
        /// <returns></returns>
        public static Rect GetAbsolutePlacement(FrameworkElement element, bool relativeToScreen = false)
        {
            var absolutePos = element.PointToScreen(new System.Windows.Point(0, 0));
            if (relativeToScreen)
            {
                //taking display dpi into account..
                var pixelSize = GetElementPixelSize(element);
                return new Rect(absolutePos.X, absolutePos.Y, pixelSize.Width, pixelSize.Height);
            }
            var posMW = Application.Current.MainWindow.PointToScreen(new System.Windows.Point(0, 0));
            absolutePos = new System.Windows.Point(absolutePos.X - posMW.X, absolutePos.Y - posMW.Y);
            return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
        }

        //https://stackoverflow.com/questions/3286175/how-do-i-convert-a-wpf-size-to-physical-pixels
        /// <summary>
        /// Retrieves pixel size of UI element, taking display scaling into account.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (var source1 = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = source1.CompositionTarget.TransformToDevice;

            if (element.DesiredSize == new Size())
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return (Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        /// <summary>
        /// makes program child of window ui framework element.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="pgmHandle"></param>
        /// <param name="element"></param>
        public static void SetProgramToFramework(Window window, IntPtr pgmHandle, FrameworkElement element)
        {
            IntPtr previewHwnd = new WindowInteropHelper(window).Handle;

            NativeMethods.RECT prct = new NativeMethods.RECT();

            var reviewPanel = WindowOperations.GetAbsolutePlacement(element, true);

            if (!NativeMethods.SetWindowPos(pgmHandle, 1, (int)reviewPanel.Left, (int)reviewPanel.Top, (int)reviewPanel.Width, (int)reviewPanel.Height, 0 | 0x0010))
            {
                SetupDesktop.LogWin32Error("setwindowpos(1) fail MapWallpaperToWindow(),");
            }

            //ScreentoClient is no longer used, this supports windows mirrored mode also, calculate new relative position of window w.r.t parent.
            NativeMethods.MapWindowPoints(pgmHandle, previewHwnd, ref prct, 2);


            SetupDesktop.SetParentSafe(pgmHandle, previewHwnd);
            //Position the wp window relative to the new parent window(workerw).
            if (!NativeMethods.SetWindowPos(pgmHandle, 1, prct.Left, prct.Top, (int)reviewPanel.Width, (int)reviewPanel.Height, 0 | 0x0010))
            {
                SetupDesktop.LogWin32Error("setwindowpos(2) fail MapWallpaperToWindow(),");
            }

        }

    }
}
