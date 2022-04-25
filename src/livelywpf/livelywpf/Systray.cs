using livelywpf.Core;
using livelywpf.Helpers;
using livelywpf.Helpers.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using livelywpf.Models;
using livelywpf.Core.Suspend;
using livelywpf.Services;
using livelywpf.ViewModels;
using livelywpf.Views;
using livelywpf.Views.LivelyProperty.Dialogues;

namespace livelywpf
{
    public class Systray : ISystray
    {
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private ToolStripMenuItem pauseTrayBtn, customiseWallpaperBtn, updateTrayBtn;
        private static readonly Random rnd = new Random();
        private bool disposedValue;

        private readonly IUserSettingsService userSettings;
        private readonly IPlayback playbackMonitor;
        private readonly IDesktopCore desktopCore;
        private readonly IAppUpdaterService appUpdater;
        private readonly LibraryViewModel libraryVm;
        private readonly SettingsViewModel settingsVm;
        private readonly MainWindow appWindow;

        public Systray(IUserSettingsService userSettings, 
            IPlayback playbackMonitor, 
            IDesktopCore desktopCore, 
            LibraryViewModel libraryVm, 
            IAppUpdaterService appUpdater, 
            SettingsViewModel settingsVm,
            MainWindow appWindow)
        {
            this.userSettings = userSettings;
            this.playbackMonitor = playbackMonitor;
            this.desktopCore = desktopCore;
            this.appUpdater = appUpdater;
            this.libraryVm = libraryVm;
            this.settingsVm = settingsVm;
            this.appWindow = appWindow;

            //NotifyIcon Fix: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            //Error: "The root Visual of a VisualTarget cannot have a parent.."
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            _notifyIcon.DoubleClick += (s, args) => Program.ShowMainWindow();
            _notifyIcon.Icon = Properties.Icons.appicon;
            _notifyIcon.Text = Properties.Resources.TitleAppName;

            CreateContextMenu();
            _notifyIcon.Visible = userSettings.Settings.SysTrayIcon;
            settingsVm.TrayIconVisibilityChange += SettingsVM_TrayIconVisibilityChange;
            desktopCore.WallpaperChanged += SetupDesktop_WallpaperChanged;
            playbackMonitor.PlaybackStateChanged += Playback_PlaybackStateChanged;
            appUpdater.UpdateChecked += (s, e) => { SetUpdateMenu(e.UpdateStatus); };
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

            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TextCloseWallpapers, null).Click += (s, e) => desktopCore.CloseAllWallpapers(true);

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

            if (!Constants.ApplicationType.IsMSIX)
            {
                _notifyIcon.ContextMenuStrip.Items.Add(new CustomContextMenu.StripSeparatorCustom().stripSeparator);
                updateTrayBtn = new ToolStripMenuItem(Properties.Resources.TextUpdateChecking, null)
                {
                    Enabled = false
                };
                updateTrayBtn.Click += (s, e) => Program.AppUpdateDialog(appUpdater.LastCheckUri, appUpdater.LastCheckChangelog);
                _notifyIcon.ContextMenuStrip.Items.Add(updateTrayBtn);
            }

            _notifyIcon.ContextMenuStrip.Items.Add(new CustomContextMenu.StripSeparatorCustom().stripSeparator);
            _notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.TextSupport, Properties.Icons.icons8_heart_48).Click += (s, e) =>
                            Helpers.LinkHandler.OpenBrowser("https://rocksdanister.github.io/lively/coffee/");
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

        private void SetUpdateMenu(AppUpdateStatus status)
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
            var items = desktopCore.Wallpapers.Where(x => x.LivelyPropertyCopyPath != null);
            if (items.Count() == 0)
            {
                //not possible, menu should be disabled.
                //nothing..
            }
            else if (items.Count() == 1)
            {
                //quick wallpaper customise tray widget.
                var settingsWidget = new LivelyPropertiesTrayWidget(items.First().Model);
                settingsWidget.Show();
            }
            else if (items.Count() > 1)
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        //multiple different wallpapers.. open control panel.
                        appWindow?.ShowControlPanelDialog();
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        var settingsWidget = new LivelyPropertiesTrayWidget(items.First().Model);
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
            if (libraryVm.LibraryItems.Count == 0)
            {
                return;
            }

            switch (userSettings.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    {
                        if (desktopCore.Wallpapers.Count == 0)
                        {
                            foreach (var screen in ScreenHelper.GetScreen())
                            {
                                desktopCore.SetWallpaper(libraryVm.LibraryItems[rnd.Next(libraryVm.LibraryItems.Count)], screen);
                            }
                        }
                        else
                        {
                            var wallpapers = desktopCore.Wallpapers.ToList();
                            foreach (var wp in wallpapers)
                            {
                                var index = libraryVm.LibraryItems.IndexOf((LibraryModel)wp.Model);
                                if (index != -1)
                                {
                                    index = (index + 1) != libraryVm.LibraryItems.Count ? (index + 1) : 0;
                                    desktopCore.SetWallpaper(libraryVm.LibraryItems[index], wp.Screen);
                                }
                            }
                        }
                    }
                    break;
                case WallpaperArrangement.span:
                case WallpaperArrangement.duplicate:
                    {
                        var wallpaper = desktopCore.Wallpapers.Count != 0 ?
                             desktopCore.Wallpapers[0].Model : libraryVm.LibraryItems[rnd.Next(libraryVm.LibraryItems.Count)];
                        var index = libraryVm.LibraryItems.IndexOf((LibraryModel)wallpaper);
                        if (index != -1)
                        {
                            index = (index + 1) != libraryVm.LibraryItems.Count ? (index + 1) : 0;
                            desktopCore.SetWallpaper(libraryVm.LibraryItems[index], ScreenHelper.GetPrimaryScreen());
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
            playbackMonitor.WallpaperPlayback = (playbackMonitor.WallpaperPlayback == PlaybackState.play) ? playbackMonitor.WallpaperPlayback = PlaybackState.paused : PlaybackState.play;
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                customiseWallpaperBtn.Enabled = desktopCore.Wallpapers.Any(x => x.LivelyPropertyCopyPath != null);
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