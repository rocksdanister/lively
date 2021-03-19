using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Drawing;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace livelywpf.Helpers
{
    public sealed class ScreenSaverService
    {
        Point mousePosOriginal;
        private readonly Timer _timer = new Timer();
        public bool IsRunning { get; private set; } = false;
        private static readonly ScreenSaverService instance = new ScreenSaverService();

        public static ScreenSaverService Instance
        {
            get
            {
                return instance;
            }
        }

        private ScreenSaverService()
        {
            Initialize();   
        }

        private void Initialize()
        {
            _timer.Elapsed += InputCheckTimer;
            _timer.Interval = 250;
        }

        private void InputCheckTimer(object sender, ElapsedEventArgs e)
        {
            //Don't want to make a mouse hook... quick soln.
            var mousePosCurr = System.Windows.Forms.Control.MousePosition;
            if (Math.Abs(mousePosOriginal.X - mousePosCurr.X) > 25
                || Math.Abs(mousePosOriginal.Y - mousePosCurr.Y) > 25)
            {
                Stop();
            }
        }

        public void Start()
        {
            if (!IsRunning && SetupDesktop.Wallpapers.Count != 0)
            {
                IsRunning = true;
                ShowScreenSavers();
                mousePosOriginal = System.Windows.Forms.Control.MousePosition;
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _timer.Stop();
                HideScreenSavers();
            }
        }

        /// <summary>
        /// Detaches wallpapers from desktop workerw.
        /// </summary>
        private void ShowScreenSavers()
        {
            foreach (var item in SetupDesktop.Wallpapers)
            {
                //detach wallpaper.
                WindowOperations.SetParentSafe(item.GetHWND(), IntPtr.Zero);
                //show on the currently running screen, not changing size.
                if (!NativeMethods.SetWindowPos(
                    item.GetHWND(), 
                    -1, //topmost
                    Program.SettingsVM.Settings.WallpaperArrangement != WallpaperArrangement.span ? item.GetScreen().Bounds.Left : 0, 
                    Program.SettingsVM.Settings.WallpaperArrangement != WallpaperArrangement.span ? item.GetScreen().Bounds.Top : 0, 
                    0, 
                    0,
                    0x0001)) 
                {
                    NLogger.LogWin32Error("setwindowpos(1) fail ShowScreenSavers(),");
                }
            }
        }

        /// <summary>
        /// Re-attaches wallpapers to desktop workerw.
        /// </summary>
        private void HideScreenSavers()
        {
            if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
            {
                if (SetupDesktop.Wallpapers.Count > 0)
                {
                    //get spawned workerw rectangle data.
                    NativeMethods.GetWindowRect(SetupDesktop.GetWorkerW(), out NativeMethods.RECT prct);
                    WindowOperations.SetParentSafe(SetupDesktop.Wallpapers[0].GetHWND(), SetupDesktop.GetWorkerW());
                    //fill wp into the whole workerw area.
                    if (!NativeMethods.SetWindowPos(SetupDesktop.Wallpapers[0].GetHWND(), 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0x0010))
                    {
                        NLogger.LogWin32Error("setwindowpos fail HideScreenSavers(),");
                    }
                }
            }
            else
            {
                foreach (var item in SetupDesktop.Wallpapers)
                {
                    //update position & size incase window is moved.
                    if (!NativeMethods.SetWindowPos(item.GetHWND(), 1, item.GetScreen().Bounds.Left, item.GetScreen().Bounds.Top, item.GetScreen().Bounds.Width, item.GetScreen().Bounds.Height, 0x0010))
                    {
                        NLogger.LogWin32Error("setwindowpos(1) fail HideScreenSavers(),");
                    }
                    //re-calcuate position on desktop workerw.
                    NativeMethods.RECT prct = new NativeMethods.RECT();
                    NativeMethods.MapWindowPoints(item.GetHWND(), SetupDesktop.GetWorkerW(), ref prct, 2);
                    //re-attach wallpaper to desktop.
                    WindowOperations.SetParentSafe(item.GetHWND(), SetupDesktop.GetWorkerW());
                    //update position & size on desktop workerw.
                    if (!NativeMethods.SetWindowPos(item.GetHWND(), 1, prct.Left, prct.Top, item.GetScreen().Bounds.Width, item.GetScreen().Bounds.Height, 0x0010))
                    {
                        NLogger.LogWin32Error("setwindowpos(2) fail HideScreenSavers(),");
                    }
                }
            }
            SetupDesktop.RefreshDesktop();
        }

        /// <summary>
        /// Attaches screensaver preview to preview region. <br>
        /// (To be run in UI thread.)</br>
        /// </summary>
        /// <param name="hwnd"></param>
        public void CreatePreview(IntPtr hwnd)
        {
            //Issue: Multiple display setup with diff dpi - making the window child affects LivelyScreen offset values.
            if (IsRunning || ScreenHelper.IsMultiScreen())
                return;

            var preview = new Views.ScreenSaverPreview
            {
                ShowActivated = false,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStyle = System.Windows.WindowStyle.None,
                WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                Left = -9999,
            };
            preview.Show();
            var previewHandle = new WindowInteropHelper(preview).Handle;
            //Set child of target.
            WindowOperations.SetParentSafe(previewHandle, hwnd);
            //Make this a child window so it will close when the parent dialog closes.
            NativeMethods.SetWindowLongPtr(new HandleRef(null, previewHandle),
                (int)NativeMethods.GWL.GWL_STYLE,
                new IntPtr(NativeMethods.GetWindowLong(previewHandle, (int)NativeMethods.GWL.GWL_STYLE) | NativeMethods.WindowStyles.WS_CHILD));
            //Get size of target.
            NativeMethods.GetClientRect(hwnd, out NativeMethods.RECT prct);
            //Update preview size and position.
            if (!NativeMethods.SetWindowPos(previewHandle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos fail Preview Screensaver,");
            }
        }

        #region helpers

        // Fails after 50 days..
        static uint GetLastInputTime()
        {
            NativeMethods.LASTINPUTINFO lastInputInfo = new NativeMethods.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (NativeMethods.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;

                return (envTicks - lastInputTick);
            }
            else
            {
                throw new Win32Exception();
            }
        }

        #endregion //helpers
    }
}
