using Lively.Common;
using Lively.Core;
using Lively.Core.Suspend;
using Lively.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Lively
{

    //TODO: Switch to wpf-notifyicon library instead.
    public class Systray : ISystray
    {
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private ToolStripMenuItem pauseTrayBtn, customiseWallpaperBtn, updateTrayBtn;
        private bool disposedValue;

        private readonly IRunnerService runner;
        private readonly IDesktopCore desktopCore;
        private readonly IUserSettingsService userSettings;
        private readonly IPlayback playbackMonitor;

        public Systray(IRunnerService runner, IUserSettingsService userSettings, IDesktopCore desktopCore, IPlayback playbackMonitor)
        {
            this.runner = runner;
            this.desktopCore = desktopCore;
            this.userSettings = userSettings;
            this.playbackMonitor = playbackMonitor;

            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Error: "The root Visual of a VisualTarget cannot have a parent.."
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            //Show UI
            _notifyIcon.DoubleClick += (s, args) => runner.ShowUI();
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.Icon = Properties.Resources.appicon;
            _notifyIcon.Text = "Lively Wallpaper";
            _notifyIcon.Visible = userSettings.Settings.SysTrayIcon;
            _notifyIcon.ContextMenuStrip.Items.Add("Open Lively").Click += (s, e) => runner.ShowUI();
            _notifyIcon.ContextMenuStrip.Items.Add("Close all wallpaper(s)", null).Click += (s, e) => desktopCore.CloseAllWallpapers(true);
            pauseTrayBtn = new ToolStripMenuItem("Pause Wallpaper(s)", null);
            pauseTrayBtn.Click += (s, e) =>
            {
                playbackMonitor.WallpaperPlayback = (playbackMonitor.WallpaperPlayback == PlaybackState.play) ? 
                    playbackMonitor.WallpaperPlayback = PlaybackState.paused : PlaybackState.play;
            };
            _notifyIcon.ContextMenuStrip.Items.Add(pauseTrayBtn);
            customiseWallpaperBtn = new ToolStripMenuItem("Customise Wallpaper", null)
            {
                Enabled = false,
                Visible = false,
            };
            customiseWallpaperBtn.Click += CustomiseWallpaper;
            _notifyIcon.ContextMenuStrip.Items.Add(customiseWallpaperBtn);
            _notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => App.ShutDown();

            playbackMonitor.PlaybackStateChanged += Playback_PlaybackStateChanged;
            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
        }

        public void Visibility(bool visible)
        {
            _notifyIcon.Visible = visible;
        }

        public void ShowBalloonNotification(int timeout, string title, string msg)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, msg, ToolTipIcon.None);
        }

        private void Playback_PlaybackStateChanged(object sender, PlaybackState e)
        {
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                pauseTrayBtn.Checked = e == PlaybackState.paused;
                //_notifyIcon.Icon = (e == PlaybackState.paused) ? Properties.Icons.appicon_gray : Properties.Icons.appicon;
            }));
        }

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                customiseWallpaperBtn.Enabled = desktopCore.Wallpapers.Any(x => x.LivelyPropertyCopyPath != null);
            }));
        }

        private void CustomiseWallpaper(object sender, EventArgs e)
        {
            var items = desktopCore.Wallpapers.Where(x => x.LivelyPropertyCopyPath != null);
            if (items.Count() == 0)
            {
                //not possible, menu should be disabled.
                //nothing..
            }
            else if (items.Count() == 1)
            {
                //quick wallpaper customise tray widget.
                runner.ShowCustomisWallpaperePanel();
                
            }
            else if (items.Count() > 1)
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        //multiple different wallpapers.. open control panel.
                        runner.ShowControlPanel();
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        runner.ShowCustomisWallpaperePanel();
                        break;
                }
            }
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
