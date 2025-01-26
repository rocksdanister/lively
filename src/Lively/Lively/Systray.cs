using Lively.Common;
using Lively.Common.Factories;
using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Core.Suspend;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Services;
using Lively.Services;
using Lively.Themes;
using Lively.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Lively
{
    public class Systray : ISystray
    {
        private readonly Random rng = new Random();
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private bool disposedValue;

        private readonly ToolStripMenuItem openAppTrayMenu;
        private readonly ToolStripMenuItem closeWallpaperTrayMenu;
        private readonly ToolStripMenuItem changeWallpaperTrayMenu;
        private readonly ToolStripMenuItem reportBugTrayMenu;
        private readonly ToolStripMenuItem exitAppTrayMenu;
        private readonly ToolStripMenuItem pauseTrayMenu;
        private readonly ToolStripMenuItem customiseWallpaperMenu;
        private readonly ToolStripMenuItem updateTrayMenu;

        private readonly IRunnerService runner;
        private readonly IResourceService i18n;
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;
        private readonly IUserSettingsService userSettings;
        private readonly IAppUpdaterService appUpdater;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;

        private DiagnosticMenu diagnosticMenu;
        private AppTheme? currentTheme = null;

        public Systray(IResourceService i18n,
            IRunnerService runner,
            IUserSettingsService userSettings,
            IDesktopCore desktopCore,
            IAppUpdaterService appUpdater,
            IDisplayManager displayManager,
            IPlayback playbackMonitor,
            IWallpaperLibraryFactory wallpaperLibraryFactory)
        {
            this.i18n = i18n;
            this.runner = runner;
            this.desktopCore = desktopCore;
            this.userSettings = userSettings;
            this.displayManager = displayManager;
            this.appUpdater = appUpdater;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;

            // NotifyIcon Issue: "The root Visual of a VisualTarget cannot have a parent.."
            // Ref: https://stackoverflow.com/questions/28833702/wpf-notifyicon-crash-on-first-run-the-root-visual-of-a-visualtarget-cannot-hav/29116917
            System.Windows.Controls.ToolTip tt = new System.Windows.Controls.ToolTip();
            tt.IsOpen = true;
            tt.IsOpen = false;

            // Properties
            _notifyIcon.DoubleClick += (s, args) => runner.ShowUI();
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.Icon = Properties.Icons.appicon;
            _notifyIcon.Text = "Lively Wallpaper";
            _notifyIcon.Visible = userSettings.Settings.SysTrayIcon;
            var toolStripColor = Color.FromArgb(55, 55, 55);
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip
            {
                Padding = new Padding(0),
                Margin = new Padding(0),
                //Font = new System.Drawing.Font("Segoe UI", 10F),
            };
            SetTheme(userSettings.Settings.ApplicationTheme);
            _notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;

            // Menu registrations
            openAppTrayMenu = new ToolStripMenuItem(i18n.GetString("TextOpenLively"), Properties.Icons.icons8_application_window_96);
            openAppTrayMenu.Click += (s, e) => runner.ShowUI();
            _notifyIcon.ContextMenuStrip.Items.Add(openAppTrayMenu);

            closeWallpaperTrayMenu = new ToolStripMenuItem(i18n.GetString("TextCloseWallpapers"), null);
            closeWallpaperTrayMenu.Click += (s, e) => desktopCore.CloseAllWallpapers(true);
            _notifyIcon.ContextMenuStrip.Items.Add(CreateToolStripSeparator(toolStripColor));
            _notifyIcon.ContextMenuStrip.Items.Add(closeWallpaperTrayMenu);

            pauseTrayMenu = new ToolStripMenuItem(i18n.GetString("TextPauseWallpapers"), null);
            pauseTrayMenu.Click += (s, e) =>
            {
                playbackMonitor.WallpaperPlayback = (playbackMonitor.WallpaperPlayback == PlaybackState.play) ? PlaybackState.paused : PlaybackState.play;
            };
            _notifyIcon.ContextMenuStrip.Items.Add(pauseTrayMenu);

            changeWallpaperTrayMenu = new ToolStripMenuItem(i18n.GetString("TextChangeWallpaper"), null);
            changeWallpaperTrayMenu.Click += async (s, e) => await SetRandomWallpapers();
            _notifyIcon.ContextMenuStrip.Items.Add(changeWallpaperTrayMenu);

            customiseWallpaperMenu = new ToolStripMenuItem(i18n.GetString("TextCustomiseWallpaper"), null)
            {
                //Systray is initialized first before restoring wallpaper
                Enabled = false,
            };
            customiseWallpaperMenu.Click += CustomiseWallpaper;
            _notifyIcon.ContextMenuStrip.Items.Add(customiseWallpaperMenu);

            // Update check, only create on installer build.
            if (!Constants.ApplicationType.IsMSIX)
            {
                updateTrayMenu = new ToolStripMenuItem(i18n.GetString("TextUpdateChecking"), null)
                {
                    Enabled = false
                };
                updateTrayMenu.Click += (s, e) => runner.ShowAppUpdatePage();
                _notifyIcon.ContextMenuStrip.Items.Add(CreateToolStripSeparator(toolStripColor));
                _notifyIcon.ContextMenuStrip.Items.Add(updateTrayMenu);
            }

            reportBugTrayMenu = new ToolStripMenuItem(i18n.GetString("ReportBug/Header"), Properties.Icons.icons8_website_bug_96);
            reportBugTrayMenu.Click += (s, e) =>
            {
                if (diagnosticMenu is null)
                {
                    diagnosticMenu = new DiagnosticMenu();
                    diagnosticMenu.Closed += (s, e) => diagnosticMenu = null;
                    diagnosticMenu.Show();
                }
            };
            _notifyIcon.ContextMenuStrip.Items.Add(CreateToolStripSeparator(toolStripColor));
            _notifyIcon.ContextMenuStrip.Items.Add(reportBugTrayMenu);

            exitAppTrayMenu = new ToolStripMenuItem(i18n.GetString("TextExit"), Properties.Icons.icons8_close_96);
            exitAppTrayMenu.Click += (s, e) => App.QuitApp();
            _notifyIcon.ContextMenuStrip.Items.Add(CreateToolStripSeparator(toolStripColor));
            _notifyIcon.ContextMenuStrip.Items.Add(exitAppTrayMenu);

            playbackMonitor.PlaybackStateChanged += Playback_PlaybackStateChanged;
            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            appUpdater.UpdateChecked += (s, e) => { SetUpdateMenu(e.UpdateStatus); };
            i18n.CultureChanged += I18n_CultureChanged;
        }

        public void Visibility(bool visible)
        {
            _notifyIcon.Visible = visible;
        }

        public void ShowBalloonNotification(int timeout, string title, string msg)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, msg, ToolTipIcon.None);
        }

        public void SetTheme(AppTheme theme)
        {
            theme = theme == AppTheme.Auto ? ThemeUtil.GetWindowsTheme() : theme;
            if (currentTheme != null && currentTheme == theme)
                return;

            switch (theme)
            {
                case AppTheme.Auto: // not applicable
                case AppTheme.Dark:
                    {
                        _notifyIcon.ContextMenuStrip.ForeColor = Color.AliceBlue;
                        ToolStripManager.Renderer = new ToolStripRendererDark();
                    }
                    break;
                case AppTheme.Light:
                    {
                        _notifyIcon.ContextMenuStrip.ForeColor = Color.Black;
                        ToolStripManager.Renderer = new ToolStripRendererLight();
                    }
                    break;
            }
            currentTheme = theme;
        }


        /// <summary>
        /// Fix for traymenu opening to the nearest screen instead of the screen in which cursor is located.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ContextMenuStrip menuStrip = (sender as ContextMenuStrip);
            if (displayManager.IsMultiScreen())
            {
                //Finding screen in which cursor is present.
                var screen = displayManager.GetDisplayMonitorFromPoint(Cursor.Position);

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

        private void Playback_PlaybackStateChanged(object sender, PlaybackState e)
        {
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                pauseTrayMenu.Checked = e == PlaybackState.paused;
                //_notifyIcon.Icon = (e == PlaybackState.paused) ? Properties.Icons.appicon_gray : Properties.Icons.appicon;
            }));
        }

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                customiseWallpaperMenu.Enabled = desktopCore.Wallpapers.Any(x => x.LivelyPropertyCopyPath != null);
            }));
        }

        private void CustomiseWallpaper(object sender, EventArgs e)
        {
            var items = desktopCore.Wallpapers.Where(x => x.LivelyPropertyCopyPath != null);
            if (items.Any())
            {
                runner.ShowCustomisWallpaperePanel();
            }
        }

        private void SetUpdateMenu(AppUpdateStatus status)
        {
            switch (status)
            {
                case AppUpdateStatus.uptodate:
                    updateTrayMenu.Enabled = false;
                    updateTrayMenu.Text = i18n.GetString("TextUpdateUptodate");
                    break;
                case AppUpdateStatus.available:
                    updateTrayMenu.Enabled = true;
                    updateTrayMenu.Text = i18n.GetString("TextUpdateAvailable");
                    break;
                case AppUpdateStatus.invalid:
                    updateTrayMenu.Enabled = false;
                    updateTrayMenu.Text = "Fancy~";
                    break;
                case AppUpdateStatus.notchecked:
                    updateTrayMenu.Enabled = false;
                    updateTrayMenu.Text = i18n.GetString("TextUpdateChecking");
                    break;
                case AppUpdateStatus.error:
                    updateTrayMenu.Enabled = true;
                    updateTrayMenu.Text = i18n.GetString("TextupdateCheckFail");
                    break;
            }
        }

        private void I18n_CultureChanged(object sender, string e)
        {
            openAppTrayMenu.Text = i18n.GetString("TextOpenLively");
            closeWallpaperTrayMenu.Text = i18n.GetString("TextCloseWallpapers");
            pauseTrayMenu.Text = i18n.GetString("TextPauseWallpapers");
            changeWallpaperTrayMenu.Text = i18n.GetString("TextChangeWallpaper");
            customiseWallpaperMenu.Text = i18n.GetString("TextCustomiseWallpaper");
            SetUpdateMenu(appUpdater.Status);
            reportBugTrayMenu.Text = i18n.GetString("ReportBug/Header");
            exitAppTrayMenu.Text = i18n.GetString("TextExit");
        }

        /// <summary>
        /// Sets random library item as wallpaper.
        /// </summary>
        private async Task SetRandomWallpapers()
        {
            switch (userSettings.Settings.WallpaperArrangement)
            {
                case WallpaperArrangement.per:
                    {
                        var screenCount = displayManager.DisplayMonitors.Count;
                        var wallpapersRandom = GetRandomWallpaper().Take(screenCount);
                        var wallpapersCount = wallpapersRandom.Count();
                        if (wallpapersCount > 0)
                        {
                            for (int i = 0; i < screenCount; i++)
                            {
                                await desktopCore.SetWallpaperAsync(wallpapersRandom.ElementAt(i > wallpapersCount - 1 ? 0 : i), displayManager.DisplayMonitors[i]);
                            }
                        }
                    }
                    break;
                case WallpaperArrangement.span:
                case WallpaperArrangement.duplicate:
                    {
                        try
                        {
                            await desktopCore.SetWallpaperAsync(GetRandomWallpaper().First(), displayManager.PrimaryDisplayMonitor);
                        }
                        catch (InvalidOperationException)
                        {
                            //No wallpapers present.
                        }
                    }
                    break;
            }
        }

        #region helpers

        private IEnumerable<LibraryModel> GetRandomWallpaper()
        {
            var dir = new List<string>();
            string[] folderPaths = {
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
            };
            for (int i = 0; i < folderPaths.Count(); i++)
            {
                try
                {
                    dir.AddRange(Directory.GetDirectories(folderPaths[i], "*", SearchOption.TopDirectoryOnly));
                }
                catch { /* TODO */ }
            }

            //Fisher-Yates shuffle
            int n = dir.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = dir[k];
                dir[k] = dir[n];
                dir[n] = value;
            }

            for (int i = 0; i < dir.Count; i++)
            {
                LibraryModel libItem = null;
                try
                {
                    libItem = wallpaperLibraryFactory.CreateFromDirectory(dir[i]);
                }
                catch { }

                if (libItem != null)
                {
                    yield return libItem;
                }
            }
        }

        private static ToolStripSeparator CreateToolStripSeparator(Color color)
        {
            ToolStripSeparator separator = new ToolStripSeparator();
            separator.Paint += (s, e) =>
            {
                ToolStripSeparator stripSeparator = s as ToolStripSeparator;
                ContextMenuStrip menuStrip = stripSeparator.Owner as ContextMenuStrip;
                e.Graphics.FillRectangle(new SolidBrush(Color.Transparent), new Rectangle(0, 0, stripSeparator.Width, stripSeparator.Height));
                using var pen = new Pen(color, 1);
                e.Graphics.DrawLine(pen, new System.Drawing.Point(23, stripSeparator.Height / 2), new System.Drawing.Point(menuStrip.Width, stripSeparator.Height / 2));
            };
            return separator;
        }

        #endregion //helpers

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
