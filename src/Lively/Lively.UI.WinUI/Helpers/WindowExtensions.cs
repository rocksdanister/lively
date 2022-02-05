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
    //Ref: https://github.com/microsoft/microsoft-ui-xaml/issues/4056
    public static class WindowExtensions
    {
        public static void SetIcon(this Window window, string iconName)
        {
            LoadIcon(iconName, window);
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
