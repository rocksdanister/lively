using Lively.Common.Helpers.Pinvoke;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;

namespace Lively.UI.WinUI.Extensions
{
    public static class WindowExtensions
    {
        public static void SetIconEx(this Window window, string iconName)
        {
            //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/4056
            LoadIcon(iconName, window);
        }

        public static void SetWindowSizeEx(this Window window, int width, int height)
        {
            //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/6353
            //IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
            //var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            //var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            //appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 720));
            //SetWindowSize(m_windowHandle, 875, 875);

            var hwnd = window.GetWindowHandleEx();
            var dpi = NativeMethods.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            NativeMethods.SetWindowPos(hwnd, 0, 0, 0, width, height, (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE);
        }

        public static nint GetWindowHandleEx(this Window window)
        {
            var windowNative = window.As<IWindowNative>();
            return windowNative.WindowHandle;
        }

        //References:
        //https://github.com/microsoft/WindowsAppSDK/issues/41
        //https://docs.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
        public static bool UseImmersiveDarkModeEx(this Window window, bool enabled)
        {
            var status = false;
            if (IsWindows10OrGreater(17763))
            {
                var hwnd = window.GetWindowHandleEx();
                int useImmersiveDarkMode = enabled ? 1 : 0;
                var attribute = IsWindows10OrGreater(18985) ? NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE : NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                status = NativeMethods.DwmSetWindowAttribute(hwnd, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;

                NativeMethods.ShowWindow(hwnd, (uint)NativeMethods.SHOWWINDOW.SW_HIDE);
                NativeMethods.ShowWindow(hwnd, (uint)NativeMethods.SHOWWINDOW.SW_SHOW);
                /*
                NativeMethods.SendMessage(hwnd, (int)NativeMethods.WM.NCPAINT, IntPtr.Zero, IntPtr.Zero);
                NativeMethods.SetWindowPos(hwnd, 0, 0, 0, 0, 0, 
                    (int)(NativeMethods.SetWindowPosFlags.SWP_DRAWFRAME | 
                    NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE | 
                    NativeMethods.SetWindowPosFlags.SWP_NOMOVE | 
                    NativeMethods.SetWindowPosFlags.SWP_NOSIZE | 
                    NativeMethods.SetWindowPosFlags.SWP_NOZORDER));
                */
            }
            return status;
        }

        public static void SetDragRegionForCustomTitleBar(this Window window,
            ColumnDefinition rightPaddingColumn,
            ColumnDefinition leftPaddingColumn,
            ColumnDefinition iconColumn,
            ColumnDefinition titleColumn,
            ColumnDefinition leftDragColumn,
            ColumnDefinition rightDragColumn,
            ColumnDefinition searchColumn,
            TextBlock titleTextBlock,
            Grid appTitleBar)
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
                return;

            var appWindow = window.AppWindow;
            if (AppWindowTitleBar.IsCustomizationSupported()
                && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = window.GetScaleAdjustment();

                rightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
                leftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

                List<Windows.Graphics.RectInt32> dragRectsList = new();

                Windows.Graphics.RectInt32 dragRectL;
                dragRectL.X = (int)((leftPaddingColumn.ActualWidth) * scaleAdjustment);
                dragRectL.Y = 0;
                dragRectL.Height = (int)(appTitleBar.ActualHeight * scaleAdjustment);
                dragRectL.Width = (int)((iconColumn.ActualWidth
                                        + titleColumn.ActualWidth
                                        + leftDragColumn.ActualWidth) * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                Windows.Graphics.RectInt32 dragRectR;
                dragRectR.X = (int)((leftPaddingColumn.ActualWidth
                                    + iconColumn.ActualWidth
                                    + titleTextBlock.ActualWidth
                                    + leftDragColumn.ActualWidth
                                    + searchColumn.ActualWidth) * scaleAdjustment);
                dragRectR.Y = 0;
                dragRectR.Height = (int)(appTitleBar.ActualHeight * scaleAdjustment);
                dragRectR.Width = (int)(rightDragColumn.ActualWidth * scaleAdjustment);
                dragRectsList.Add(dragRectR);

                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }

        private static double GetScaleAdjustment(this Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        private const int IMAGE_ICON = 1;
        private const int LR_LOADFROMFILE = 0x0010;

        private static void LoadIcon(string iconName, Window window)
        {
            //Get the Window's HWND
            var hwnd = window.As<IWindowNative>().WindowHandle;
            nint hIcon = NativeMethods.LoadImage(nint.Zero, iconName,
                      IMAGE_ICON, 32, 32, LR_LOADFROMFILE);

            NativeMethods.SendMessage(hwnd, (int)NativeMethods.WM.SETICON, 0, hIcon);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            nint WindowHandle { get; }
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }
    }
}
