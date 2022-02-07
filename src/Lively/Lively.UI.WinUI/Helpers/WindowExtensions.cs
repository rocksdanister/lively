using Lively.Common.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace Microsoft.UI.Xaml
{
    //Note: Don't prefer extensions, remove when not required; suffix all methods with Ex.
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

            var hwnd = GetWindowHandleEx(window);
            var dpi = NativeMethods.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            NativeMethods.SetWindowPos(hwnd, 0, 0, 0, width, height, (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE);
        }

        public static IntPtr GetWindowHandleEx(this Window window)
        {
            var windowNative = window.As<IWindowNative>();
            return windowNative.WindowHandle;
        }

        #region helpers

        private const int IMAGE_ICON = 1;
        private const int LR_LOADFROMFILE = 0x0010;

        private static void LoadIcon(string iconName, Window window)
        {
            //Get the Window's HWND
            var hwnd = window.As<IWindowNative>().WindowHandle;
            IntPtr hIcon = NativeMethods.LoadImage(IntPtr.Zero, iconName,
                      IMAGE_ICON, 32, 32, LR_LOADFROMFILE);

            NativeMethods.SendMessage(hwnd, (int)NativeMethods.WM.SETICON, (IntPtr)0, hIcon);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        #endregion //helpers
    }
}
