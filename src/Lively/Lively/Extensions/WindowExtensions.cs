using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WindowsSize = System.Windows.Size;

namespace Lively.Extensions
{
    internal static class WindowExtensions
    {
        public static void CenterToWindow(this Window window, IntPtr targetHwnd)
        {
            var sourceHwnd = new WindowInteropHelper(window).Handle;
            NativeMethods.GetWindowRect(targetHwnd, out NativeMethods.RECT crt);
            NativeMethods.GetWindowRect(sourceHwnd, out NativeMethods.RECT prt);
            //Assigning left, top to window directly not working correctly with display scaling..
            NativeMethods.SetWindowPos(sourceHwnd,
                0,
                crt.Left + (crt.Right - crt.Left) / 2 - (prt.Right - prt.Left) / 2,
                crt.Top - (crt.Top - crt.Bottom) / 2 - (prt.Bottom - prt.Top) / 2,
                0,
                0,
                0x0001 | 0x0004);
        }

        public static void CenterToWindow(this Window window, Window target)
        {
            CenterToWindow(window, new WindowInteropHelper(target).Handle);
        }

        public static void NativeResize(this Window window, Rectangle rect)
        {
            NativeMethods.SetWindowPos(new WindowInteropHelper(window).Handle, 0, rect.Left, rect.Top, rect.Width, rect.Height, 0x0010 | 0x0004);
        }

        public static void NativeMove(this Window window, Rectangle rect)
        {
            NativeMethods.SetWindowPos(new WindowInteropHelper(window).Handle, 0, rect.Left, rect.Top, 0, 0, 0x0010 | 0x0001);
        }

        /// <summary>
        /// makes program window handle child of window ui framework element.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="pgmHandle"></param>
        /// <param name="element"></param>
        public static void SetProgramToFramework(this Window window, IntPtr pgmHandle, FrameworkElement element)
        {
            if (pgmHandle == IntPtr.Zero || !NativeMethods.IsWindow(pgmHandle))
            {
                return;
            }

            IntPtr previewHwnd = new WindowInteropHelper(window).Handle;
            if (previewHwnd == IntPtr.Zero || !NativeMethods.IsWindow(previewHwnd))
            {
                return;
            }

            NativeMethods.RECT prct = new NativeMethods.RECT();
            var reviewPanel = GetAbsolutePlacement(element, true);

            if (!NativeMethods.SetWindowPos(pgmHandle, 1, (int)reviewPanel.Left, (int)reviewPanel.Top, (int)reviewPanel.Width, (int)reviewPanel.Height, 0 | 0x0010))
            {
                return;
            }

            //ScreentoClient is no longer used, this supports windows mirrored mode also, calculate new relative position of window w.r.t parent.
            NativeMethods.MapWindowPoints(pgmHandle, previewHwnd, ref prct, 2);
            WindowUtil.TrySetParent(pgmHandle, previewHwnd);

            //Position the wp window relative to the new parent window(workerw).
            _ = NativeMethods.SetWindowPos(pgmHandle, 1, prct.Left, prct.Top, (int)reviewPanel.Width, (int)reviewPanel.Height, 0 | 0x0010);
        }

        //https://stackoverflow.com/questions/386731/get-absolute-position-of-element-within-the-window-in-wpf
        /// <summary>
        /// Get UI Framework element position.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="relativeToScreen">false: w.r.t application</param>
        /// <returns></returns>
        public static Rect GetAbsolutePlacement(this FrameworkElement element, bool relativeToScreen = false)
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
        public static WindowsSize GetElementPixelSize(this UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (var source1 = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = source1.CompositionTarget.TransformToDevice;

            if (element.DesiredSize == new WindowsSize())
                element.Measure(new WindowsSize(double.PositiveInfinity, double.PositiveInfinity));

            return (WindowsSize)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        //private const int LWA_ALPHA = 0x2;
        //private const int LWA_COLORKEY = 0x1;

        ///// <summary>
        ///// Set window alpha.
        ///// </summary>
        ///// <param name="Handle"></param>
        //public static void SetWindowTransparency(IntPtr Handle)
        //{
        //    var styleCurrentWindowExtended = NativeMethods.GetWindowLongPtr(Handle, (-20));
        //    var styleNewWindowExtended =
        //        styleCurrentWindowExtended.ToInt64() ^
        //        NativeMethods.WindowStyles.WS_EX_LAYERED;

        //    NativeMethods.SetWindowLongPtr(new HandleRef(null, Handle), (int)NativeMethods.GWL.GWL_EXSTYLE, (IntPtr)styleNewWindowExtended);
        //    NativeMethods.SetLayeredWindowAttributes(Handle, 0, 128, LWA_ALPHA);
        //}
    }
}
