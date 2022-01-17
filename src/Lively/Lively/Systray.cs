using Lively.Core;
using Lively.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace Lively
{

    //TODO: Switch to wpf-notifyicon library instead.
    public class Systray : ISystray
    {
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private bool disposedValue;

        public Systray(IRunnerService runner, IDesktopCore desktopCore)
        {
            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Error: "The root Visual of a VisualTarget cannot have a parent.."
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            //Show UI
            _notifyIcon.DoubleClick += (s, args) => runner.ShowUI();
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().ManifestModule.Name);
            _notifyIcon.Text = "Lively Wallpaper";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip.Items.Add("Open Lively").Click += (s, e) => runner.ShowUI();
            _notifyIcon.ContextMenuStrip.Items.Add("Close all wallpaper(s)", null).Click += (s, e) => desktopCore.CloseAllWallpapers(true);
            _notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => App.ShutDown();
        }

        public void ShowBalloonNotification(int timeout, string title, string msg)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, msg, ToolTipIcon.None);
        }

        #region dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon?.Icon?.Dispose();
                    _notifyIcon?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Systray()
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
