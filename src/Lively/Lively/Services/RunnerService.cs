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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "UI")
                    },
                    EnableRaisingEvents = true
                };
                processUI.Exited += Proc_UI_Exited;
                processUI.OutputDataReceived += Proc_OutputDataReceived;
                processUI.Start();
                processUI.BeginOutputReadLine();
            }
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.Info($"UI: {e.Data}");
            }
        }

        private void Proc_UI_Exited(object sender, EventArgs e)
        {
            processUI.Exited -= Proc_UI_Exited;
            processUI.OutputDataReceived -= Proc_OutputDataReceived;
            processUI?.Dispose();
            processUI = null;
        }

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
