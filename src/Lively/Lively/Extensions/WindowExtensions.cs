using Lively.Common.Helpers.Pinvoke;
using Lively.Core;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;

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
    }
}
