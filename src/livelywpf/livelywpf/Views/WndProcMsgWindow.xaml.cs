using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for WndProcMessageWindow.xaml
    /// </summary>
    public partial class WndProcMsgWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public WndProcMsgWindow()
        {
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

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SHOWLIVELY)
            {
                Logger.Info("WM_SHOWLIVELY msg received.");
                Program.ShowMainWindow();
            }
            else if (msg == NativeMethods.WM_TASKBARCREATED)
            {
                //explorer crash detection, new taskbar is created everytime explorer is started..
                Logger.Info("WM_TASKBARCREATED: New taskbar created.");
                SetupDesktop.ResetWorkerW();
            }
            else if (msg == (uint)NativeMethods.WM.QUERYENDSESSION && Program.IsMSIX)
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
    }
}
