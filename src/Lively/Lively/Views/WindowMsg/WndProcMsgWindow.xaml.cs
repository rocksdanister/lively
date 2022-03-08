using Lively.Common;
using Lively.Common.Helpers.Pinvoke;
using Lively.Core;
using Lively.Core.Display;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Lively.Views.WindowMsg
{
    /// <summary>
    /// Interaction logic for WndProcMessageWindow.xaml
    /// </summary>
    public partial class WndProcMsgWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly int WM_TASKBARCREATED = NativeMethods.RegisterWindowMessage("TaskbarCreated");
        private int prevExplorerPid = GetTaskbarExplorerPid();
        private DateTime prevCrashTime = DateTime.MinValue;

        private readonly IDisplayManager displayManager;
        private readonly IDesktopCore desktopCore;

        public WndProcMsgWindow(IDesktopCore desktopCore, IDisplayManager displayManager)
        {
            this.displayManager = displayManager;
            this.desktopCore = desktopCore;

            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        //TODO: create event instead?
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TASKBARCREATED)
            {
                Logger.Info("WM_TASKBARCREATED: New taskbar created.");
                int newExplorerPid = GetTaskbarExplorerPid();
                if (prevExplorerPid != newExplorerPid)
                {
                    //Explorer crash detection, dpi change also sends WM_TASKBARCREATED..
                    Logger.Info($"Explorer crashed, pid mismatch: {prevExplorerPid} != {newExplorerPid}");
                    if ((DateTime.Now - prevCrashTime).TotalSeconds > 30)
                    {
                        desktopCore.ResetWallpaper();
                    }
                    else
                    {
                        Logger.Warn("Explorer restarted multiple times in the last 30s.");
                        _ = Task.Run(() => MessageBox.Show(Properties.Resources.DescExplorerCrash,
                                $"{Properties.Resources.TitleAppName} - {Properties.Resources.TextError}",
                                MessageBoxButton.OK, MessageBoxImage.Error));
                        desktopCore.CloseAllWallpapers(true);
                        desktopCore.ResetWallpaper();
                    }
                    prevCrashTime = DateTime.Now;
                    prevExplorerPid = newExplorerPid;
                }
            }
            else if (msg == (uint)NativeMethods.WM.QUERYENDSESSION)
            {
                if (lParam != IntPtr.Zero && lParam == (IntPtr)0x00000001) // ENDSESSION_CLOSEAPP
                {
                    //The app is being queried if it can close for an update.
                    _ = NativeMethods.RegisterApplicationRestart(
                        null,
                        (int)NativeMethods.RestartFlags.RESTART_NO_CRASH |
                        (int)NativeMethods.RestartFlags.RESTART_NO_HANG |
                        (int)NativeMethods.RestartFlags.RESTART_NO_REBOOT);

                    return (IntPtr)1;
                }
            }
            else if (msg == (uint)NativeMethods.WM.ENDSESSION)
            {
                //Gracefully close app.
                App.ShutDown();
            }

            //Screen message processing...
            _ = displayManager.OnWndProc(hwnd, (uint)msg, wParam, lParam);

            return IntPtr.Zero;
        }

        #region helpers

        private static int GetTaskbarExplorerPid()
        {
            _ = NativeMethods.GetWindowThreadProcessId(NativeMethods.FindWindow("Shell_TrayWnd", null), out int pid);
            return pid;
        }

        #endregion //helpers
    }
}
