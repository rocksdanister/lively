using Lively.Common;
using Lively.Common.Helpers.Pinvoke;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;

namespace Lively.Services
{
    public class RunnerService : IRunnerService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Process processUI;
        private bool disposedValue;
        private readonly string uiClientFileName;
        private readonly bool uiOutputRedirect;
        private readonly bool _isElevated;
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

            if (IsElevated)
            {
                _isElevated = true;
                Logger.Warn("Process is running as admin, disabling winui.");
            }
        }

        public void ShowUI()
        {
            if (processUI != null)
            {
                try
                {
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
                    //Ref: https://github.com/rocksdanister/lively/issues/1060
                    if (_isElevated && Constants.ApplicationType.Client == ClientType.winui)
                    {
                        _ = MessageBox.Show("Lively UI cannot run as administrator because WindowsAppSDK does not currently support this.\n\nMake sure UAC driver is enabled by:\n" +
                            "Search and open Local Security Policy from startmenu > Security Settings > Local Policies > Security Options > " +
                            "Double-click User Account Control: Run all administrators in Admin Approval Mode and make sure its enabled.\nIf disabled then enable it and restart system.",
                            "Running as Administrator!",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

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
        public void ShowControlPanel() => processUI?.StandardInput.WriteLine("LM SHOWCONTROLPANEL");

        //TODO: Make it work by launching process in background.
        public void ShowCustomisWallpaperePanel() => processUI?.StandardInput.WriteLine("LM SHOWCUSTOMISEPANEL");

        public void SetBusyUI(bool isBusy) => processUI?.StandardInput.WriteLine(isBusy ? "LM SHOWBUSY" : "LM HIDEBUSY");

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

        #region helpers

        public bool IsElevated
        {
            get
            {
                try
                {
                    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch
                {
                    return false;
                }
            }
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
