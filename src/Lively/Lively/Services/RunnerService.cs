using Lively.Common;
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
        private readonly string uiClientFileName;
        private readonly bool uiOutputRedirect;
        //private NativeMethods.RECT rctUI;

        public RunnerService()
        {
            uiClientFileName = Constants.ApplicationType.Client switch
            {
                ClientType.wpf => "Lively.UI.Wpf.exe",
                ClientType.winui => "Lively.UI.WinUI.exe",
                _ => throw new NotImplementedException(),
            };
            //winui source not using Debug.Writeline() for debugging.. wtf?
            uiOutputRedirect = Constants.ApplicationType.Client != ClientType.winui;
        }

        public void ShowUI()
        {
            if (processUI != null)
            {
                try
                {
                    /*
                    if (!processUI.Responding)
                    {
                        RestartUI();
                    }
                    else
                    {
                        processUI.StandardInput.WriteLine("WM SHOW");
                    }
                    */
                    processUI.StandardInput.WriteLine("WM SHOW");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
            else
            {
                try
                {
                    processUI = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "UI", uiClientFileName),
                            RedirectStandardInput = true,
                            RedirectStandardOutput = uiOutputRedirect,
                            RedirectStandardError = uiOutputRedirect,
                            UseShellExecute = false,
                            WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "UI")
                        },
                        EnableRaisingEvents = true
                    };
                    processUI.Exited += Proc_UI_Exited;
                    processUI.OutputDataReceived += Proc_OutputDataReceived;
                    processUI.Start();
                    if (uiOutputRedirect)
                    {
                        processUI.BeginOutputReadLine();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        public void RestartUI()
        {
            if (processUI != null)
            {
                try
                {
                    processUI.Exited -= Proc_UI_Exited;
                    processUI.OutputDataReceived -= Proc_OutputDataReceived;
                    //NativeMethods.GetWindowRect(processUI.MainWindowHandle, out rctUI);
                    if (!processUI.Responding || !processUI.CloseMainWindow() || !processUI.WaitForExit(2500))
                    {
                        processUI.Kill();
                    }
                    processUI.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                finally
                {
                    processUI = null;
                }
            }
            ShowUI();
        }

        public void CloseUI()
        {
            if (processUI == null)
                return;

            try
            {
                //NativeMethods.GetWindowRect(processUI.MainWindowHandle, out rctUI);
                if (!processUI.Responding || !processUI.CloseMainWindow() || !processUI.WaitForExit(2500))
                {
                    processUI.Kill();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        //TODO: Make it work by launching process in background.
        public void ShowControlPanel()
        {
            if (processUI != null)
            {
                processUI.StandardInput.WriteLine("LM SHOWCONTROLPANEL");
            }
        }

        //TODO: Make it work by launching process in background.
        public void ShowCustomisWallpaperePanel()
        {
            if (processUI != null)
            {
                processUI.StandardInput.WriteLine("LM SHOWCUSTOMISEPANEL");
            }
        }

        public IntPtr HwndUI => processUI?.MainWindowHandle ?? IntPtr.Zero;

        public bool IsVisibleUI =>
            processUI != null && NativeMethods.IsWindowVisible(processUI.MainWindowHandle);

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!string.IsNullOrEmpty(e.Data))
            {
                //Ref: https://github.com/cyanfish/grpc-dotnet-namedpipes/issues/8
                Logger.Info($"UI: {e.Data}");
            }
        }

        private void Proc_UI_Exited(object sender, EventArgs e)
        {
            processUI.Exited -= Proc_UI_Exited;
            processUI.OutputDataReceived -= Proc_OutputDataReceived;
            processUI.Dispose();
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
