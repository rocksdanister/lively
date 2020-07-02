using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace livelywpf
{
    class Systray : IDisposable
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        public Systray()
        {
            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Rarely I get this error "The root Visual of a VisualTarget cannot have a parent..", hard to pinpoint not knowing how to recreate the error.
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            _notifyIcon.DoubleClick += (s, args) => Program.ShowMainWindow();
            _notifyIcon.Icon = Properties.Icons.appicon;
            _notifyIcon.Text = "Lively Wallpaper";

            CreateContextMenu();
            _notifyIcon.Visible = true;
            
        }

        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip =
             new System.Windows.Forms.ContextMenuStrip();

            _notifyIcon.ContextMenuStrip.Items.Add("Open Lively", Properties.Icons.icons8_home_page_961).Click += (s, e) => Program.ShowMainWindow();
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", Properties.Icons.icons8_close_window_961).Click += (s, e) => Program.ExitApplication();
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Icon.Dispose();
            _notifyIcon.Dispose();
        }
    }
}
