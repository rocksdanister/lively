using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace livelywpf
{
    class Systray : IDisposable
    {
        private System.Windows.Forms.ContextMenuStrip trayMenu = new System.Windows.Forms.ContextMenuStrip();
        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        public Systray()
        {
            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Rarely I get this error "The root Visual of a VisualTarget cannot have a parent..", hard to pinpoint not knowing how to recreate the error.
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            ///notifyIcon = new System.Windows.Forms.NotifyIcon();
            //_notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            _notifyIcon.Icon = Properties.Icons.appicon;
            _notifyIcon.Text = "Lively Wallpaper";

            CreateContextMenu();
            _notifyIcon.ContextMenuStrip = trayMenu;
            _notifyIcon.Visible = true;
            
        }

        private void CreateContextMenu()
        {

        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Icon.Dispose();
            _notifyIcon.Dispose();
        }
    }
}
