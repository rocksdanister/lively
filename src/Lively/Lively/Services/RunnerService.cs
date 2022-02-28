using Lively.Common;
using Lively.Common.Helpers.Pinvoke;
using Lively.Core.Display;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.Services
{
    public class RunnerService : IRunnerService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Process processUI;
        private bool disposedValue;
        private readonly IDisplayManager displayManager;
        private readonly string uiClientFileName;
        private readonly bool uiOutputRedirect;
        private readonly bool _isElevated;
        private bool _isFirstRun = true;
        private NativeMethods.RECT prevWindowRect = new() { Left = 50, Top = 50, Right = 925, Bottom = 925 };

        public RunnerService(IDisplayManager displayManager)
        {
            this.displayManager = displayManager;

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

                    if (!_isFirstRun)
                    {
                        SetWindowRect(processUI, prevWindowRect);
                    }
                    _isFirstRun = false;
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
                    NativeMethods.GetWindowRect(processUI.MainWindowHandle, out prevWindowRect);
                    if (!processUI.Responding || !processUI.CloseMainWindow() || !processUI.WaitForExit(500))
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
                NativeMethods.GetWindowRect(processUI.MainWindowHandle, out prevWindowRect);
                if (!processUI.Responding || !processUI.CloseMainWindow() || !processUI.WaitForExit(3500))
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

        private static bool IsElevated
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

        private void SetWindowRect(Process proc, NativeMethods.RECT rect)
        {
            if (proc == null)
                return;

            while (proc.WaitForInputIdle(-1) != true || proc.MainWindowHandle == IntPtr.Zero)
            {
                proc.Refresh();
            }

            NativeMethods.SetWindowPos(proc.MainWindowHandle,
                0,
                rect.Left,
                rect.Top,
                (rect.Right - rect.Left),
                (rect.Bottom - rect.Top),
                (int)NativeMethods.SetWindowPosFlags.SWP_SHOWWINDOW);

            //Display disconnected fallback.
            if (!IsOnScreen(proc.MainWindowHandle))
            {
                NativeMethods.SetWindowPos(proc.MainWindowHandle,
                         0,
                         displayManager.PrimaryDisplayMonitor.Bounds.Left + 50,
                         displayManager.PrimaryDisplayMonitor.Bounds.Top + 50,
                         0,
                         0,
                         (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE);
            }
        }

        /// <summary>
        /// Checks if Window is fully outside screen region.
        /// </summary>
        private bool IsOnScreen(IntPtr hwnd)
        {
            if (NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT winRect) != 0)
            {
                var rect = new Rectangle(
                    winRect.Left,
                    winRect.Top,
                    (winRect.Right - winRect.Left),
                    (winRect.Bottom - winRect.Top));
                return displayManager.DisplayMonitors.Any(s => s.WorkingArea.IntersectsWith(rect));
            }
            return false;
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
