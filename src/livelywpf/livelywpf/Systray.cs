using livelywpf.Core;
using livelywpf.Helpers;
using livelywpf.Helpers.UI;
using livelywpf.Helpers.Updater;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using livelywpf.Models;
using livelywpf.Core.Suspend;

namespace livelywpf
{
    class Systray : IDisposable
    {
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private ToolStripMenuItem pauseTrayBtn, customiseWallpaperBtn, updateTrayBtn;
        private static readonly Random rnd = new Random();
        private bool disposedValue;

        public Systray(bool visibility = true)
        {
            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Error: "The root Visual of a VisualTarget cannot have a parent.."
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            _notifyIcon.DoubleClick += (s, args) => Program.ShowMainWindow();
            _notifyIcon.Icon = Properties.Icons.appicon;
            _notifyIcon.Text = Properties.Resources.TitleAppName;

            CreateContextMenu();
            _notifyIcon.Visible = visibility;
            Program.SettingsVM.TrayIconVisibilityChange += SettingsVM_TrayIconVisibilityChange;
            SetupDesktop.WallpaperChanged += SetupDesktop_WallpaperChanged;
            Playback.PlaybackStateChanged += Playback_PlaybackStateChanged;
        }

        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip
            {
                ForeColor = Color.AliceBlue,
                Padding = new Padding(0),
                Margin = new Padding(0),
                //Font = new System.Drawing.Font("Segoe UI", 10F),
            };
            _notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;

            _notifyIcon.ContextMenuStrip.Renderer = new CustomContextMenu.RendererDark();
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TextOpenLively, Properties.Icons.icons8_home_64).Click += (s, e) => Program.ShowMainWindow();

            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TextCloseWallpapers, null).Click += (s, e) => SetupDesktop.TerminateAllWallpapers();

            pauseTrayBtn = new ToolStripMenuItem(Properties.Resources.TextPauseWallpapers, Properties.Icons.icons8_pause_52);
            pauseTrayBtn.Click += (s, e) => ToggleWallpaperPlaybackState();
            _notifyIcon.ContextMenuStrip.Items.Add(pauseTrayBtn);

            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TextChangeWallpaper, null).Click += (s, e) => SetNextWallpaper();
            customiseWallpaperBtn = new ToolStripMenuItem(Properties.Resources.TextCustomiseWallpaper, null)
            {
                Enabled = false
            };
            customiseWallpaperBtn.Click += CustomiseWallpaper;
            _notifyIcon.ContextMenuStrip.Items.Add(customiseWallpaperBtn);

            if (!Program.IsMSIX)
            {
                _notifyIcon.ContextMenuStrip.Items.Add(new CustomContextMenu.StripSeparatorCustom().stripSeparator);
                updateTrayBtn = new ToolStripMenuItem(Properties.Resources.TextUpdateChecking, null)
                {
                    Enabled = false
                };
                updateTrayBtn.Click += (s, e) => Program.AppUpdateDialog(AppUpdaterService.Instance.LastCheckUri, AppUpdaterService.Instance.LastCheckChangelog);
                _notifyIcon.ContextMenuStrip.Items.Add(updateTrayBtn);
            }

            _notifyIcon.ContextMenuStrip.Items.Add(new CustomContextMenu.StripSeparatorCustom().stripSeparator);
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TextSupport, Properties.Icons.icons8_heart_48).Click += (s, e) =>
                            Helpers.LinkHandler.OpenBrowser("https://ko-fi.com/rocksdanister");
            _notifyIcon.ContextMenuStrip.Items.Add(new CustomContextMenu.StripSeparatorCustom().stripSeparator);
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TitleReportBug, Properties.Icons.icons8_bug_50).Click += (s, e) =>
                            Helpers.LinkHandler.OpenBrowser("https://github.com/rocksdanister/lively/wiki/Common-Problems");

            _notifyIcon.ContextMenuStrip.Items.Add(new CustomContextMenu.StripSeparatorCustom().stripSeparator);
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TextExit, Properties.Icons.icons8_delete_52).Click += (s, e) => Program.ExitApplication();
        }

        /// <summary>
        /// Fix for traymenu opening to the nearest screen instead of the screen in which cursor is located.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ContextMenuStrip menuStrip = (sender as ContextMenuStrip);
            if (ScreenHelper.IsMultiScreen())
            {
                //Finding screen in which cursor is present.
                var screen = ScreenHelper.GetScreenFromPoint(Cursor.Position);

                var mousePos = Cursor.Position;
                //Converting global cursor pos. to given screen pos.
                mousePos.X += -1 * screen.Bounds.X;
                mousePos.Y += -1 * screen.Bounds.Y;

                //guessing taskbar pos. based on cursor pos. on display.
                bool isLeft = mousePos.X < screen.Bounds.Width * .5;
                bool isTop = mousePos.Y < screen.Bounds.Height * .5;

                //menu popup pos. rule.
                if (isLeft && isTop)
                {
                    //not possible?
                    menuStrip.Show(Cursor.Position, ToolStripDropDownDirection.Default);
                }
                if (isLeft && !isTop)
                {
                    menuStrip.Show(Cursor.Position, ToolStripDropDownDirection.AboveRight);
                }
                else if (!isLeft && isTop)
                {
                    menuStrip.Show(Cursor.Position, ToolStripDropDownDirection.BelowLeft);
                }
                else if (!isLeft && !isTop)
                {
                    menuStrip.Show(Cursor.Position, ToolStripDropDownDirection.AboveLeft);
                }
            }
            else
            {
                menuStrip.Show(Cursor.Position, ToolStripDropDownDirection.AboveLeft);
            }
        }

        public void ShowBalloonNotification(int timeout, string title, string msg)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, msg, ToolTipIcon.None);
        }

        public void SetUpdateMenu(AppUpdateStatus status)
        {
            switch (status)
            {
                case AppUpdateStatus.uptodate:
                    updateTrayBtn.Enabled = false;
                    updateTrayBtn.Text = Properties.Resources.TextUpdateUptodate;
                    break;
                case AppUpdateStatus.available:
                    updateTrayBtn.Enabled = true;
                    updateTrayBtn.Text = Properties.Resources.TextUpdateAvailable;
                    break;
                case AppUpdateStatus.invalid:
                    updateTrayBtn.Enabled = false;
                    updateTrayBtn.Text = ">_<";
                    break;
                case AppUpdateStatus.notchecked:
                    updateTrayBtn.Enabled = false;
                    break;
                case AppUpdateStatus.error:
                    updateTrayBtn.Enabled = true;
                    updateTrayBtn.Text = Properties.Resources.TextupdateCheckFail;
                    break;
            }
        }

        private void CustomiseWallpaper(object sender, EventArgs e)
        {
            var items = SetupDesktop.Wallpapers.FindAll(x => x.GetWallpaperData().LivelyPropertyPath != null);
            if (items.Count == 0)
            {
                //not possible, menu should be disabled.
                //nothing..
            }
            else if (items.Count == 1)
            {
                //quick wallpaper customise tray widget.
                var settingsWidget = new Cef.LivelyPropertiesTrayWidget(items[0].GetWallpaperData());
                settingsWidget.Show();
            }
            else if (items.Count > 1)
            {
                switch (Program.SettingsVM.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        //multiple different wallpapers.. open control panel.
                        App.AppWindow?.ShowControlPanelDialog();
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        var settingsWidget = new Cef.LivelyPropertiesTrayWidget(items[0].GetWallpaperData());
                        settingsWidget.Show();
                        break;
                }
            }
        }

        /// <summary>
        /// Sets next library item as wallpaper.<para>
        /// Selection is random if no wallpaper is running.</para>
        /// </summary>
        private void SetNextWallpaper()
        {
            if (Program.LibraryVM.LibraryItems.Count == 0)
            {
                return;
            }

            switch (Program.SettingsVM.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    {
                        if (SetupDesktop.Wallpapers.Count == 0)
                        {
                            foreach (var screen in ScreenHelper.GetScreen())
                            {
                                SetupDesktop.SetWallpaper(Program.LibraryVM.LibraryItems[rnd.Next(Program.LibraryVM.LibraryItems.Count)], screen);
                            }
                        }
                        else
                        {
                            var wallpapers = SetupDesktop.Wallpapers.ToList();
                            foreach (var wp in wallpapers)
                            {
                                var index = Program.LibraryVM.LibraryItems.IndexOf(wp.GetWallpaperData());
                                if (index != -1)
                                {
                                    index = (index + 1) != Program.LibraryVM.LibraryItems.Count ? (index + 1) : 0;
                                    SetupDesktop.SetWallpaper(Program.LibraryVM.LibraryItems[index], wp.GetScreen());
                                }
                            }
                        }
                    }
                    break;
                case WallpaperArrangement.span:
                case WallpaperArrangement.duplicate:
                    {
                        var wallpaper = SetupDesktop.Wallpapers.Count != 0 ?
                             SetupDesktop.Wallpapers[0].GetWallpaperData() : Program.LibraryVM.LibraryItems[rnd.Next(Program.LibraryVM.LibraryItems.Count)];
                        var index = Program.LibraryVM.LibraryItems.IndexOf(wallpaper);
                        if (index != -1)
                        {
                            index = (index + 1) != Program.LibraryVM.LibraryItems.Count ? (index + 1) : 0;
                            SetupDesktop.SetWallpaper(Program.LibraryVM.LibraryItems[index], ScreenHelper.GetPrimaryScreen());
                        }
                    }
                    break;
            }
        }

        private void Playback_PlaybackStateChanged(object sender, PlaybackState e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                pauseTrayBtn.Checked = (e == PlaybackState.paused);
                _notifyIcon.Icon = (e == PlaybackState.paused) ? Properties.Icons.appicon_gray : Properties.Icons.appicon;
            }));
        }

        private void ToggleWallpaperPlaybackState()
        {
            Playback.WallpaperPlaybackState = (Playback.WallpaperPlaybackState == PlaybackState.play) ? Playback.WallpaperPlaybackState = PlaybackState.paused : PlaybackState.play;
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                customiseWallpaperBtn.Enabled = SetupDesktop.Wallpapers.FindAll(x => x.GetWallpaperData().LivelyPropertyPath != null).Count != 0;
            }));
        }

        private void SettingsVM_TrayIconVisibilityChange(object sender, bool visibility)
        {
            TrayIconVisibility(visibility);
        }

        private void TrayIconVisibility(bool visibility)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = visibility;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Program.SettingsVM.TrayIconVisibilityChange -= SettingsVM_TrayIconVisibilityChange;
                    Playback.PlaybackStateChanged -= Playback_PlaybackStateChanged;
                    _notifyIcon.Visible = false;
                    _notifyIcon?.Icon.Dispose();
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
    }
}