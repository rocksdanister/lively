using livelywpf.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Helpers.Shell
{
    public static class DesktopUtil
    {
        /// <summary>
        /// Initial system desktop icon visibility settings.<br>
        /// Issue: does not update if user changes setting.</br>
        /// </summary>
        public static bool DesktopIconVisibilityDefault { get; }

        static DesktopUtil()
        {
            DesktopIconVisibilityDefault = GetDesktopIconVisibility();
        }

        public static bool GetDesktopIconVisibility()
        {
            NativeMethods.SHELLSTATE state = new NativeMethods.SHELLSTATE();
            NativeMethods.SHGetSetSettings(ref state, NativeMethods.SSF.SSF_HIDEICONS, false); //get state
            return !state.fHideIcons;
        }

        //ref: https://stackoverflow.com/questions/6402834/how-to-hide-desktop-icons-programmatically/
        public static void SetDesktopIconVisibility(bool isVisible)
        {
            //Does not work in Win10
            //NativeMethods.SHGetSetSettings(ref state, NativeMethods.SSF.SSF_HIDEICONS, true);

            if (GetDesktopIconVisibility() ^ isVisible) //XOR!!!
            {
                NativeMethods.SendMessage(GetDesktopSHELLDLL_DefView(), (int)NativeMethods.WM.COMMAND, (IntPtr)0x7402, IntPtr.Zero);
            }
        }

        private static IntPtr GetDesktopSHELLDLL_DefView()
        {
            var hShellViewWin = IntPtr.Zero;
            var hWorkerW = IntPtr.Zero;

            var hProgman = NativeMethods.FindWindow("Progman", "Program Manager");
            var hDesktopWnd = NativeMethods.GetDesktopWindow();

            // If the main Program Manager window is found
            if (hProgman != IntPtr.Zero)
            {
                // Get and load the main List view window containing the icons.
                hShellViewWin = NativeMethods.FindWindowEx(hProgman, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (hShellViewWin == IntPtr.Zero)
                {
                    // When this fails (picture rotation is turned ON), then look for the WorkerW windows list to get the
                    // correct desktop list handle.
                    // As there can be multiple WorkerW windows, iterate through all to get the correct one
                    do
                    {
                        hWorkerW = NativeMethods.FindWindowEx(hDesktopWnd, hWorkerW, "WorkerW", null);
                        hShellViewWin = NativeMethods.FindWindowEx(hWorkerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    } while (hShellViewWin == IntPtr.Zero && hWorkerW != IntPtr.Zero);
                }
            }
            return hShellViewWin;
        }

        /// <summary>
        /// Force redraw desktop - clears wallpaper persisting on screen even after close.
        /// </summary>
        public static void RefreshDesktop()
        {
            //todo: Find a better way to do this?
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, null, NativeMethods.SPIF_UPDATEINIFILE);
        }
    }
}
