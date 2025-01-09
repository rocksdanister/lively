using Lively.Common.Helpers.Pinvoke;
using Lively.Models.Enums;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Common.Extensions
{
    public static class ProcessExtensions
    {
        /// <summary>
        /// Function to search for window of spawned program.
        /// </summary>
        public static async Task<IntPtr> WaitForProcesWindow(this Process proc, int timeOut, CancellationToken ct, bool nativeSearch = false)
        {
            if (proc == null)
                return IntPtr.Zero;

            proc.Refresh();
            //waiting for program messageloop to be ready (GUI is not guaranteed to be ready.)
            while (proc.WaitForInputIdle(-1) != true)
                ct.ThrowIfCancellationRequested();

            IntPtr wHWND = IntPtr.Zero;
            //Find process window.
            for (int i = 0; i < timeOut && proc.HasExited == false; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (!IntPtr.Equals((wHWND = GetProcessWindow(proc, nativeSearch)), IntPtr.Zero))
                    break;
                await Task.Delay(1, ct);
            }
            return wHWND;
        }

        public static async Task<IntPtr> WaitForProcessOrGameWindow(this Process proc, WallpaperType type, int timeOut, CancellationToken ct, bool nativeSearch = false)
        {
            if (proc == null)
            {
                return IntPtr.Zero;
            }

            proc.Refresh();
            //waiting for program messageloop to be ready (GUI is not guaranteed to be ready.)
            while (proc.WaitForInputIdle(-1) != true)
            {
                ct.ThrowIfCancellationRequested();
            }

            IntPtr wHWND = IntPtr.Zero;
            if (type == WallpaperType.godot)
            {
                for (int i = 0; i < timeOut && proc.HasExited == false; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    //todo: verify pid of window.
                    wHWND = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Engine", null);
                    if (!IntPtr.Equals(wHWND, IntPtr.Zero))
                        break;
                    await Task.Delay(1, ct);
                }
                return wHWND;
            }

            //Find process window.
            for (int i = 0; i < timeOut && proc.HasExited == false; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (!IntPtr.Equals((wHWND = proc.GetProcessWindow(nativeSearch)), IntPtr.Zero))
                    break;
                await Task.Delay(1, ct);
            }

            //Player settings dialog of Unity (if exists.)
            IntPtr cHWND = NativeMethods.FindWindowEx(wHWND, IntPtr.Zero, "Button", "Play!");
            if (!IntPtr.Equals(cHWND, IntPtr.Zero))
            {
                //Simulate Play! button click. (Unity config window)
                NativeMethods.SendMessage(cHWND, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                //Refreshing..
                wHWND = IntPtr.Zero;
                await Task.Delay(1, ct);

                //Search for Unity main Window.
                for (int i = 0; i < timeOut && proc.HasExited == false; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!IntPtr.Equals((wHWND = proc.GetProcessWindow(nativeSearch)), IntPtr.Zero))
                    {
                        break;
                    }
                    await Task.Delay(1, ct);
                }
            }
            return wHWND;
        }

        /// <summary>
        /// Retrieve window handle of process.
        /// </summary>
        /// <param name="proc">Process to search for.</param>
        /// <param name="nativeSearch">Use win32 method to find window.</param>
        /// <returns></returns>
        public static IntPtr GetProcessWindow(this Process proc, bool nativeSearch = false)
        {
            if (proc == null)
                return IntPtr.Zero;

            if (nativeSearch)
            {
                return FindWindowByProcessId(proc.Id);
            }
            else
            {
                proc.Refresh();
                //Issue(.net core) MainWindowHandle zero: https://github.com/dotnet/runtime/issues/32690
                return proc.MainWindowHandle;
            }
        }

        private static IntPtr FindWindowByProcessId(int pid)
        {
            IntPtr HWND = IntPtr.Zero;
            NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                _ = NativeMethods.GetWindowThreadProcessId(tophandle, out int cur_pid);
                if (cur_pid == pid)
                {
                    if (NativeMethods.IsWindowVisible(tophandle))
                    {
                        HWND = tophandle;
                        return false;
                    }
                }

                return true;
            }), IntPtr.Zero);

            return HWND;
        }
    }
}
