using livelywpf.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;

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

        private static System.Windows.Forms.ToolStripMenuItem pauseTrayBtn;
        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip
            {
                ForeColor = Color.AliceBlue,
                Padding = new Padding(0),
                Margin = new Padding(0),
                //Font = new System.Drawing.Font("Segoe UI", 10F),
            };

            _notifyIcon.ContextMenuStrip.Renderer = new Helpers.CustomContextMenu.RendererDark();
            _notifyIcon.ContextMenuStrip.Items.Add("Open Lively", Properties.Icons.icons8_home_64).Click += (s, e) => Program.ShowMainWindow();

            _notifyIcon.ContextMenuStrip.Items.Add("Close Wallpapers", null).Click += (s, e) => SetupDesktop.CloseAllWallpapers();

            pauseTrayBtn = new System.Windows.Forms.ToolStripMenuItem("Pause Wallpapers", Properties.Icons.icons8_pause_52);
            pauseTrayBtn.Click += (s, e) => ToggleWallpaperPlaybackState();
            _notifyIcon.ContextMenuStrip.Items.Add(pauseTrayBtn);

            _notifyIcon.ContextMenuStrip.Items.Add("Customize Wallpapers", null).Click += Systray_Click;

            //_notifyIcon.ContextMenuStrip.Items.Add("-");
            _notifyIcon.ContextMenuStrip.Items.Add(new Helpers.CustomContextMenu.StripSeparatorCustom().stripSeparator);
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", Properties.Icons.icons8_delete_52).Click += (s, e) => Program.ExitApplication();
        }

        private void Systray_Click(object sender, EventArgs e)
        {
            //testing only.
            if (Program.LibraryVM.SelectedItem != null)
            {
                if (Program.LibraryVM.SelectedItem.LivelyPropertyPath != null)
                {
                    var settingsWidget = new Cef.LivelyPropertiesTrayWidget(Program.LibraryVM.SelectedItem, Program.LibraryVM.GetLivelyPropertyCopyPath(Program.LibraryVM.SelectedItem));
                    settingsWidget.Show();
                }
            }
        }

        private static void ToggleWallpaperPlaybackState()
        {
            if (Playback.PlaybackState == PlaybackState.play)
            {
                Playback.PlaybackState = PlaybackState.paused;
                pauseTrayBtn.Checked = true;
            }
            else
            {
                Playback.PlaybackState = PlaybackState.play;
                pauseTrayBtn.Checked = false;
            }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Icon.Dispose();
            _notifyIcon.Dispose();
        }
    }
}