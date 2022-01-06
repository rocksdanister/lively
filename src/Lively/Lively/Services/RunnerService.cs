using Lively.Common.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lively.Services
{
    public class RunnerService : IRunnerService
    {
        private Process processUI;
        private bool disposedValue;

        public RunnerService()
        {

        }

        public void ShowUI()
        {
            if (processUI != null)
            {
                if (NativeMethods.IsIconic(processUI.MainWindowHandle))
                {
                    _ = NativeMethods.ShowWindow(processUI.MainWindowHandle, (uint)NativeMethods.SHOWWINDOW.SW_RESTORE);
                }
                else
                {
                    _ = NativeMethods.SetForegroundWindow(processUI.MainWindowHandle);
                }
            }
            else
            {
                processUI = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "UI", "Lively.UI.Wpf.exe"),
                        //RedirectStandardInput = true,
                        //RedirectStandardOutput = true,
                        //RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "UI")
                    },
                    EnableRaisingEvents = true
                };
                processUI.Exited += Proc_UI_Exited;
                processUI.Start();
            }
        }

        private void Proc_UI_Exited(object sender, EventArgs e)
        {
            processUI?.Dispose();
            processUI = null;
        }

        #region helpers

        private IntPtr FindWindowByProcessId(int pid)
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

        #endregion //helpers

        #region dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        processUI?.Kill();
                    }
                    catch { }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RunnerService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion //dispose
    }
}
