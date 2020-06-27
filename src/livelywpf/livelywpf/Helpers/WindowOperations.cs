using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace livelywpf
{
    public static class WindowOperations
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
        /// makes program window handle child of window ui framework element.
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
                NLogger.LogWin32Error("setwindowpos(1) fail MapWallpaperToWindow(),");
            }

            //ScreentoClient is no longer used, this supports windows mirrored mode also, calculate new relative position of window w.r.t parent.
            NativeMethods.MapWindowPoints(pgmHandle, previewHwnd, ref prct, 2);


            SetParentSafe(pgmHandle, previewHwnd);
            //Position the wp window relative to the new parent window(workerw).
            if (!NativeMethods.SetWindowPos(pgmHandle, 1, prct.Left, prct.Top, (int)reviewPanel.Width, (int)reviewPanel.Height, 0 | 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos(2) fail MapWallpaperToWindow(),");
            }

        }

        public static void SetParentSafe(IntPtr child, IntPtr parent)
        {
            IntPtr ret = NativeMethods.SetParent(child, parent);
            if (ret.Equals(IntPtr.Zero))
            {
                NLogger.LogWin32Error("failed to set custom parent,");
            }
        }

        /// <summary>
        /// Removes window border & some menuitems. Won't remove everything in apps with custom UI system.
        /// Flags Credit: https://github.com/Codeusa/Borderless-Gaming
        /// If there is an issue with me using the flags just let me know I will remove it.
        /// </summary>
        /// <param name="handle">window handle</param>
        public static void BorderlessWinStyle(IntPtr handle)
        {
            // Get window styles
            var styleCurrentWindowStandard = NativeMethods.GetWindowLongPtr(handle, (-16));
            var styleCurrentWindowExtended = NativeMethods.GetWindowLongPtr(handle, (-20));

            // Compute new styles (XOR of the inverse of all the bits to filter)
            var styleNewWindowStandard =
                              styleCurrentWindowStandard.ToInt64()
                              & ~(
                                    (Int64)NativeMethods.WindowStyles.WS_CAPTION // composite of Border and DialogFrame          
                                  | (Int64)NativeMethods.WindowStyles.WS_THICKFRAME
                                  | (Int64)NativeMethods.WindowStyles.WS_SYSMENU
                                  | (Int64)NativeMethods.WindowStyles.WS_MAXIMIZEBOX // same as TabStop
                                  | (Int64)NativeMethods.WindowStyles.WS_MINIMIZEBOX // same as Group
                              );


            var styleNewWindowExtended =
                styleCurrentWindowExtended.ToInt64()
                & ~(
                      (Int64)NativeMethods.WindowStyles.WS_EX_DLGMODALFRAME
                    | (Int64)NativeMethods.WindowStyles.WS_EX_COMPOSITED
                    | (Int64)NativeMethods.WindowStyles.WS_EX_WINDOWEDGE
                    | (Int64)NativeMethods.WindowStyles.WS_EX_CLIENTEDGE
                    | (Int64)NativeMethods.WindowStyles.WS_EX_LAYERED
                    | (Int64)NativeMethods.WindowStyles.WS_EX_STATICEDGE
                    | (Int64)NativeMethods.WindowStyles.WS_EX_TOOLWINDOW
                    | (Int64)NativeMethods.WindowStyles.WS_EX_APPWINDOW
                );

            // update window styles
            NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (-16), (IntPtr)styleNewWindowStandard);
            NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (-20), (IntPtr)styleNewWindowExtended);

            // remove the menu and menuitems and force a redraw
            var menuHandle = NativeMethods.GetMenu(handle);
            if (menuHandle != IntPtr.Zero)
            {
                var menuItemCount = NativeMethods.GetMenuItemCount(menuHandle);

                for (var i = 0; i < menuItemCount; i++)
                {
                    NativeMethods.RemoveMenu(menuHandle, 0, NativeMethods.MF_BYPOSITION | NativeMethods.MF_REMOVE);
                }
                NativeMethods.DrawMenuBar(handle);
            }
        }

        /// <summary>
        /// Makes window toolwindow and force remove from taskbar.
        /// </summary>
        /// <param name="handle">window handle</param>
        public static void RemoveWindowFromTaskbar(IntPtr handle)
        {
            var styleNewWindowExtended =
                   (Int64)NativeMethods.WindowStyles.WS_EX_NOACTIVATE
                   | (Int64)NativeMethods.WindowStyles.WS_EX_TOOLWINDOW;

            // update window styles
            NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (-20), (IntPtr)styleNewWindowExtended);
        }

    }
}
