using livelywpf.Core;
using livelywpf.Helpers.Pinvoke;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace livelywpf.Views
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
        private readonly IDesktopCore desktopCore;

        public WndProcMsgWindow(IDesktopCore desktopCore)
        {
            this.desktopCore = desktopCore;

            InitializeComponent();
            //Starting a hidden window outside screen region, rawinput receives msg through WndProc
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = -99999;
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
            else if (msg == (uint)NativeMethods.WM.QUERYENDSESSION && Constants.ApplicationType.IsMSIX)
            {
                _ = NativeMethods.RegisterApplicationRestart(
                    null,
                    (int)NativeMethods.RestartFlags.RESTART_NO_CRASH |
                    (int)NativeMethods.RestartFlags.RESTART_NO_HANG |
                    (int)NativeMethods.RestartFlags.RESTART_NO_REBOOT);
            }
            //screen message processing...
            _ = Core.DisplayManager.Instance?.OnWndProc(hwnd, (uint)msg, wParam, lParam);

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
