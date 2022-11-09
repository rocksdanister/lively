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
using UAC = UACHelper.UACHelper;

namespace Lively.Services
{
    public class RunnerService : IRunnerService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Process processUI;
        private bool disposedValue;
        private readonly IDisplayManager displayManager;
        private bool _isFirstRun = true;
        private NativeMethods.RECT prevWindowRect = new() { Left = 50, Top = 50, Right = 925, Bottom = 925 };
        private readonly string fileName, workingDirectory;

        public RunnerService(IDisplayManager displayManager)
        {
            this.displayManager = displayManager;

            if (UAC.IsElevated)
            {
                //Ref: https://github.com/rocksdanister/lively/issues/1060
                Logger.Warn("Process is running elevated, UI may not function properly.");
            }

            if (Constants.ApplicationType.IsMSIX)
            {
                fileName = Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\")), "Lively.UI.WinUI.exe");
                workingDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\"));
            }
            else
            {
                fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "UI", "Lively.UI.WinUI.exe");
                workingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "UI");
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
                    processUI = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = fileName,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = false,
                            RedirectStandardError = false,
                            UseShellExecute = false,
                            WorkingDirectory = workingDirectory,
                        },
                        EnableRaisingEvents = true
                    };
                    processUI.Exited += Proc_UI_Exited;
                    processUI.OutputDataReceived += Proc_OutputDataReceived;
                    processUI.Start();
                    //winui writing debug information into output stream :/
                    //processUI.BeginOutputReadLine();
                    //processUI.BeginErrorReadLine();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    processUI = null;
                    _ = MessageBox.Show($"{Properties.Resources.LivelyExceptionGeneral}\nEXCEPTION:\n{e.Message}",
                        Properties.Resources.TextError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                if (!_isFirstRun)
                {
                    try
                    {
                        SetWindowRect(processUI, prevWindowRect);
                    }
                    catch (Exception ie)
                    {
                        Logger.Error($"Failed to restore windowrect: {ie.Message}");
                    }
                }
                _isFirstRun = false;
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

        public void SaveRectUI()
        {
            if (processUI == null)
                return;

            NativeMethods.GetWindowRect(processUI.MainWindowHandle, out prevWindowRect);
        }

        public void ShowCustomisWallpaperePanel()
        {
            if (processUI is null)
            {
                try
                {
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = fileName,
                            UseShellExecute = false,
                            Arguments ="--trayWidget true",
                            WorkingDirectory = workingDirectory,
                        },
                    };
                    proc.Start();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
            else
            {
                processUI?.StandardInput.WriteLine("LM SHOWCUSTOMISEPANEL");
            }
        }

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

        private void SetWindowRect(Process proc, NativeMethods.RECT rect)
        {
            if (proc == null)
                throw new ArgumentNullException(nameof(proc));

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
