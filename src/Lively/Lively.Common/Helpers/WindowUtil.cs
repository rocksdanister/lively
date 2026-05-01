using Lively.Common.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;


namespace Lively.Common.Helpers
{
    public static class WindowUtil
    {
        public static double GetDisplayCoverageRatioByWindowGrid(
            List<IntPtr> topLevelWindows,
            Rectangle screenBounds,
            int tileSize = 50,
            double threshold = 0.05)
        {
            if (topLevelWindows is null || topLevelWindows.Count == 0)
                return 0d;

            if (topLevelWindows.Exists(NativeMethods.IsZoomed))
                return 1d;

            int width = screenBounds.Width;
            int height = screenBounds.Height;
            int cols = (int)Math.Ceiling(width / (double)tileSize);
            int rows = (int)Math.Ceiling(height / (double)tileSize);
            int totalTiles = rows * cols;
            int coveredCount = 0;
            var covered = new bool[rows, cols];

            foreach (var hwnd in topLevelWindows)
            {
                if (NativeMethods.GetWindowRect(hwnd, out var rect) == 0 || IsEmpty(rect))
                    continue;

                if (IsWindowCoveringTarget(rect, screenBounds, 0.95))
                    return 1d;

                // Find overlapping tile indices
                int xStart = Math.Max(0, (rect.Left - screenBounds.Left) / tileSize);
                int xEnd = Math.Min(cols - 1, (rect.Right - screenBounds.Left - 1) / tileSize);
                int yStart = Math.Max(0, (rect.Top - screenBounds.Top) / tileSize);
                int yEnd = Math.Min(rows - 1, (rect.Bottom - screenBounds.Top - 1) / tileSize);

                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        if (!covered[y, x])
                        {
                            covered[y, x] = true;
                            coveredCount++;
                        }
                    }
                }
            }

            if (totalTiles == 0)
                return 0d;

            return coveredCount / (double)totalTiles;
        }

        public static bool IsDisplayCoveredByWindowGrid(
            List<IntPtr> topLevelWindows,
            Rectangle screenBounds,
            int tileSize = 50,
            double threshold = 0.05)
        {
            return GetDisplayCoverageRatioByWindowGrid(topLevelWindows, screenBounds, tileSize, threshold) >= 1d - threshold;
        }

        public static double GetDisplayCoverageRatioByAnyWindow(List<IntPtr> topLevelWindows, Rectangle screenBounds)
        {
            if (topLevelWindows is null || topLevelWindows.Count == 0)
                return 0d;

            double maxCoverage = 0d;
            var targetAreaSize = (long)(screenBounds.Width * screenBounds.Height);
            if (targetAreaSize <= 0)
                return 0d;

            foreach (var hwnd in topLevelWindows)
            {
                if (NativeMethods.IsZoomed(hwnd))
                    return 1d;

                if (NativeMethods.GetWindowRect(hwnd, out var rect) == 0 || IsEmpty(rect))
                    continue;

                var intersection = Rectangle.Intersect(ToRectangle(rect), screenBounds);
                if (intersection.IsEmpty)
                    continue;

                var intersectionArea = (long)intersection.Width * intersection.Height;
                var coverage = intersectionArea / (double)targetAreaSize;
                if (coverage > maxCoverage)
                {
                    maxCoverage = coverage;
                    if (maxCoverage >= 1d)
                        return 1d;
                }
            }

            return maxCoverage;
        }

        public static bool IsDisplayCoveredByAnyWindow(List<IntPtr> topLevelWindows, Rectangle screenBounds, double threshold = 0.95)
        {
            return GetDisplayCoverageRatioByAnyWindow(topLevelWindows, screenBounds) >= threshold;
        }

        public static bool IsDisplayCoveredByWindow(IntPtr hwnd, Rectangle screenBounds, double threshold = 0.95)
        {
            return NativeMethods.IsZoomed(hwnd) || IsWindowCoveringTarget(hwnd, screenBounds, threshold);
        }

        public static List<IntPtr> GetVisibleTopLevelWindows()
        {
            var windows = new List<IntPtr>();

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (IsVisibleTopLevelWindows(hWnd))
                    windows.Add(hWnd);

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public static bool IsVisibleTopLevelWindows(IntPtr hwnd)
        {
            if (NativeMethods.IsWindowVisible(hwnd) && 
                !IsCloakedWindow(hwnd) && !IsTransparentWindow(hwnd) &&
                !NativeMethods.IsIconic(hwnd) && 
                !IsToolWindow(hwnd) &&
                // Check the window does not have WS_EX_NOACTIVATE (or if it does, it has WS_EX_APPWINDOW)
                (!IsNoActivateWindow(hwnd) || IsAppWindow(hwnd)) &&
                NativeMethods.GetWindowRect(hwnd, out _) != 0 &&
                NativeMethods.GetWindowTextLength(hwnd) != 0 &&
                IsTopLevelWindow(hwnd))
                return true;

            return false;
        }

        public static bool IsExcludedDesktopWindowClass(IntPtr hwnd)
        {
            const int maxChars = 256;
            StringBuilder className = new(maxChars);
            return NativeMethods.GetClassName((int)hwnd, className, maxChars) > 0 && 
                WindowClassExclusions.DesktopClasses.Contains(className.ToString());
        }

        private static bool IsWindowCoveringTarget(NativeMethods.RECT windowRect, Rectangle targetArea, double threshold)
        {
            int left = Math.Max(windowRect.Left, targetArea.Left);
            int top = Math.Max(windowRect.Top, targetArea.Top);
            int right = Math.Min(windowRect.Right, targetArea.Right);
            int bottom = Math.Min(windowRect.Bottom, targetArea.Bottom);

            if (!(right >= left && bottom >= top))
                return false;

            int intersectionWidth = Math.Max(0, right - left);
            int intersectionHeight = Math.Max(0, bottom - top);
            long intersectionArea = (long)intersectionWidth * intersectionHeight;

            var targetAreaSize = (long)(targetArea.Width * targetArea.Height);
            if (targetAreaSize <= 0)
                return false;

            double coverageRatio = intersectionArea / (double)targetAreaSize;
            return coverageRatio >= threshold;
        }

        public static bool IsWindowCoveringTarget(Rectangle windowRect, Rectangle targetArea, double threshold)
        {
            var intersection = Rectangle.Intersect(windowRect, targetArea);
            var ratio = (intersection.Width * intersection.Height) / (double)(targetArea.Width * targetArea.Height);
            return ratio >= threshold;
        }

        public static bool IsWindowCoveringTarget(IntPtr windowHwnd, Rectangle targetArea, double threshold)
        {
            if (NativeMethods.GetWindowRect(windowHwnd, out var windowRect) != 0)
                return IsWindowCoveringTarget(ToRectangle(windowRect), targetArea, threshold);
            return false;

        }

        public static bool IsTopLevelWindow(IntPtr hWnd)
        {
            return NativeMethods.GetAncestor(hWnd, NativeMethods.GetAncestorFlags.GetRoot) == hWnd;
        }

        public static bool IsUWPApp(IntPtr hwnd)
        {
            return HasClass(hwnd, "ApplicationFrameWindow");
        }

        public static bool IsTransparentWindow(IntPtr hwnd)
        {
            int exStyle = NativeMethods.GetWindowLong(hwnd, (int)NativeMethods.GWL.GWL_EXSTYLE);
            bool isLayered = (exStyle & NativeMethods.WindowStyles.WS_EX_LAYERED) != 0;
            bool isTransparent = (exStyle & NativeMethods.WindowStyles.WS_EX_TRANSPARENT) != 0;
            return isLayered || isTransparent;
        }

        public static bool IsToolWindow(IntPtr hwnd)
        {
            int exStyle = GetExtendedWindowStyle(hwnd);
            return HasFlag(exStyle, NativeMethods.WindowStyles.WS_EX_TOOLWINDOW);
        }

        public static bool IsAppWindow(IntPtr hwnd)
        {
            int exStyle = GetExtendedWindowStyle(hwnd);
            return HasFlag(exStyle, NativeMethods.WindowStyles.WS_EX_APPWINDOW);
        }

        public static bool IsNoActivateWindow(IntPtr hwnd)
        {
            int exStyle = GetExtendedWindowStyle(hwnd);
            return HasFlag(exStyle, NativeMethods.WindowStyles.WS_EX_NOACTIVATE);
        }

        public static bool IsCloakedWindow(IntPtr hwnd)
        {
            NativeMethods.DwmGetWindowAttribute(hwnd, (int)NativeMethods.DWMWINDOWATTRIBUTE.Cloaked, out int cloakedVal, sizeof(int));
            return cloakedVal != 0;
        }

        public static bool TrySetParent(IntPtr child, IntPtr parent)
        {
            return NativeMethods.SetParent(child, parent) != IntPtr.Zero;
        }

        public static IntPtr GetLastChildWindow(IntPtr parent)
        {
            IntPtr lastChild = IntPtr.Zero;

            NativeMethods.EnumChildWindows(parent, (hWnd, lParam) =>
            {
                lastChild = hWnd;
                return true;
            }, IntPtr.Zero);

            return lastChild;
        }

        /// <summary>
        /// Makes window toolwindow and force remove from taskbar.
        /// </summary>
        /// <param name="handle">window handle</param>
        public static void RemoveWindowFromTaskbar(IntPtr handle)
        {
            var styleCurrentWindowExtended = NativeMethods.GetWindowLongPtr(handle, (int)NativeMethods.GWL.GWL_EXSTYLE);

            var styleNewWindowExtended = styleCurrentWindowExtended.ToInt64() |
                   (Int64)NativeMethods.WindowStyles.WS_EX_NOACTIVATE |
                   (Int64)NativeMethods.WindowStyles.WS_EX_TOOLWINDOW;

            //update window styles
            //https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowlongptra
            //Certain window data is cached, so changes you make using SetWindowLongPtr will not take effect until you call the SetWindowPos function?
            NativeMethods.ShowWindow(handle, (int)NativeMethods.SHOWWINDOW.SW_HIDE);
            if (NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (int)NativeMethods.GWL.GWL_EXSTYLE, (IntPtr)styleNewWindowExtended) == IntPtr.Zero)
            {
                //LogUtil.LogWin32Error("Failed to modify window style");
            }
            NativeMethods.ShowWindow(handle, (int)NativeMethods.SHOWWINDOW.SW_SHOW);
        }

        /// <summary>
        /// Removes window border and some menuitems. Won't remove everything in apps with custom UI system.<para>
        /// Ref: https://github.com/Codeusa/Borderless-Gaming
        /// </para>
        /// </summary>
        /// <param name="handle">Window handle</param>
        public static void BorderlessWinStyle(IntPtr handle)
        {
            // Get window styles
            var styleCurrentWindowStandard = NativeMethods.GetWindowLongPtr(handle, (int)NativeMethods.GWL.GWL_STYLE);
            var styleCurrentWindowExtended = NativeMethods.GetWindowLongPtr(handle, (int)NativeMethods.GWL.GWL_EXSTYLE);

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
            if (NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (int)NativeMethods.GWL.GWL_STYLE, (IntPtr)styleNewWindowStandard) == IntPtr.Zero)
            {
                //LogUtil.LogWin32Error("Failed to modify window style(1)");
            }

            if (NativeMethods.SetWindowLongPtr(new HandleRef(null, handle), (int)NativeMethods.GWL.GWL_EXSTYLE, (IntPtr)styleNewWindowExtended) == IntPtr.Zero)
            {
                //LogUtil.LogWin32Error("Failed to modify window style(2)");
            }

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

        private const int LWA_ALPHA = 0x2;
        private const int LWA_COLORKEY = 0x1;

        public static void SetWindowTransparency(IntPtr hwnd, byte transparency = 255)
        {
            var exStyle = GetExtendedWindowStyle(hwnd);
            if ((exStyle & NativeMethods.WindowStyles.WS_EX_LAYERED) == 0)
            {
                var styleNewWindowExtended = exStyle | NativeMethods.WindowStyles.WS_EX_LAYERED;
                NativeMethods.SetWindowLongPtr(new HandleRef(null, hwnd), (int)NativeMethods.GWL.GWL_EXSTYLE, (IntPtr)styleNewWindowExtended);
            }
            NativeMethods.SetLayeredWindowAttributes(hwnd, 0, transparency, LWA_ALPHA);
        }

        public static void SetWindowStyle(IntPtr hwnd, long styleToAdd)
        {
            long currentStyle = NativeMethods.GetWindowLongPtr(hwnd, (int)NativeMethods.GWL.GWL_STYLE).ToInt64();
            long newStyle = currentStyle | styleToAdd;

            NativeMethods.SetWindowLongPtr(new HandleRef(null, hwnd), (int)NativeMethods.GWL.GWL_STYLE, (IntPtr)newStyle);
        }

        public static void SetWindowExStyle(IntPtr hwnd, long exStyleToAdd)
        {
            long currentExStyle = NativeMethods.GetWindowLongPtr(hwnd, (int)NativeMethods.GWL.GWL_EXSTYLE).ToInt64();
            long newExStyle = currentExStyle | exStyleToAdd;

            NativeMethods.SetWindowLongPtr(new HandleRef(null, hwnd), (int)NativeMethods.GWL.GWL_EXSTYLE, (IntPtr)newExStyle);
        }

        public static bool HasClass(IntPtr hwnd, string expectedClassName)
        {
            if (hwnd == IntPtr.Zero)
                return false;

            const int maxChars = 256;
            var className = new StringBuilder(maxChars);
            return NativeMethods.GetClassName((int)hwnd, className, maxChars) > 0 && 
                className.ToString().Equals(expectedClassName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasFlag(int value, uint flag)
        {
            return (value & flag) == flag;
        }

        public static bool HasExtendedStyle(IntPtr hwnd, uint style)
        {
            if (hwnd == IntPtr.Zero)
                return false;

            IntPtr exStylePtr = NativeMethods.GetWindowLongPtr(hwnd, (int)NativeMethods.GWL.GWL_EXSTYLE);
            if (exStylePtr == IntPtr.Zero)
                return false;

            return (exStylePtr.ToInt64() & style) != 0;
        }

        private static int GetExtendedWindowStyle(IntPtr hWnd)
        {
            IntPtr exStylePtr = NativeMethods.GetWindowLongPtr(hWnd, (int)NativeMethods.GWL.GWL_EXSTYLE);
            return (int)(long)exStylePtr;
        }

        static Rectangle ToRectangle(NativeMethods.RECT rect)
        {
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        private static bool IsEmpty(NativeMethods.RECT rect)
        {
            return rect.Right <= rect.Left || rect.Bottom <= rect.Top;
        }
    }
}
