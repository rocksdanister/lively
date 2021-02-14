using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Drawing;

namespace livelywpf.Helpers
{
    public sealed class ScreenSaverService
    {
        private bool isRunning;
        Point mousePosOriginal;
        private readonly Timer _timer = new Timer();
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
            _timer.Elapsed += MouseTimer_Elapsed;
            _timer.Interval = 250;
        }

        private void MouseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Don't want to make a mouse hook... quick soln.
            var mousePosCurr = System.Windows.Forms.Control.MousePosition;
            if (Math.Abs(mousePosOriginal.X - mousePosCurr.X) > 25
                || Math.Abs(mousePosOriginal.Y - mousePosCurr.Y) > 25)
            {
                StopService();
            }
        }

        public void StartService()
        {
            if (!isRunning)
            {
                isRunning = true;
                ShowScreenSavers();
                mousePosOriginal = System.Windows.Forms.Control.MousePosition;
                _timer.Start();
            }
        }

        public void StopService()
        {
            if (isRunning)
            {
                isRunning = false;
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
                    0 | 0x0001)) 
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
                    if (!NativeMethods.SetWindowPos(SetupDesktop.Wallpapers[0].GetHWND(), 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0 | 0x0010))
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
                    if (!NativeMethods.SetWindowPos(item.GetHWND(), 1, item.GetScreen().Bounds.Left, item.GetScreen().Bounds.Top, item.GetScreen().Bounds.Width, item.GetScreen().Bounds.Height, 0 | 0x0010))
                    {
                        NLogger.LogWin32Error("setwindowpos(1) fail HideScreenSavers(),");
                    }
                    //re-calcuate position on desktop workerw.
                    NativeMethods.RECT prct = new NativeMethods.RECT();
                    NativeMethods.MapWindowPoints(item.GetHWND(), SetupDesktop.GetWorkerW(), ref prct, 2);
                    //re-attach wallpaper to desktop.
                    WindowOperations.SetParentSafe(item.GetHWND(), SetupDesktop.GetWorkerW());
                    //update position & size on desktop workerw.
                    if (!NativeMethods.SetWindowPos(item.GetHWND(), 1, prct.Left, prct.Top, item.GetScreen().Bounds.Width, item.GetScreen().Bounds.Height, 0 | 0x0010))
                    {
                        NLogger.LogWin32Error("setwindowpos(2) fail HideScreenSavers(),");
                    }
                }
            }
            SetupDesktop.RefreshDesktop();
        }
    }
}
