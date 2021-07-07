using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.ComponentModel;
using livelywpf.Views;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using Timer = System.Timers.Timer;
using H.Hooks;
using Point = System.Drawing.Point;

namespace livelywpf.Helpers
{
    public sealed class ScreensaverService
    {
        #region init

        private uint idleWaitTime = 300000;
        private readonly Timer idleTimer = new Timer();
        public bool IsRunning { get; private set; } = false;
        private static readonly ScreensaverService instance = new ScreensaverService();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<ScreensaverBlank> blankWindows = new List<ScreensaverBlank>();

        public static ScreensaverService Instance
        {
            get
            {
                return instance;
            }
        }

        private ScreensaverService()
        {
            idleTimer.Elapsed += IdleCheckTimer;
            idleTimer.Interval = 30000;
        }

        #endregion //init

        #region public

        public void Start()
        {
            if (!IsRunning)
            {
                //moving cursor outisde screen..
                _ = NativeMethods.SetCursorPos(int.MaxValue, 0);
                Logger.Info("Starting screensaver..");
                IsRunning = true;
                ShowScreensavers();
                ShowBlankScreensavers();
                StartInputListener();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                Logger.Info("Stopping screensaver..");
                IsRunning = false;
                StopInputListener();
                HideScreensavers();
                CloseBlankScreensavers();

                if (Program.SettingsVM.Settings.ScreensaverLockOnResume)
                {
                    try
                    {
                        //async..
                        LockWorkStationSafe();
                    }
                    catch (Win32Exception e)
                    {
                        Logger.Error("Failed to lock pc: " + e.Message);
                    }
                }
            }
        }

        public void StartIdleTimer(uint idleTime)
        {
            if (idleTime == 0)
            {
                StopIdleTimer();
            }
            else
            {
                Logger.Info("Starting screensaver idle wait {0}ms..", idleTime);
                idleWaitTime = idleTime;
                idleTimer.Start();
            }
        }

        public void StopIdleTimer()
        {
            if (idleTimer.Enabled)
            {
                Logger.Info("Stopping screensaver idle wait..");
                idleTimer.Stop();
            }
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
            {
                return;
            }

            //Verify if the hwnd is screensaver demo area.
            const int maxChars = 256;
            StringBuilder className = new StringBuilder(maxChars);
            if (NativeMethods.GetClassName(hwnd, className, maxChars) > 0)
            {
                string cName = className.ToString();
                if (!string.Equals(cName, "SSDemoParent", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Skipping ss preview, wrong hwnd class {0}.", cName);
                    return;
                }
            }
            else
            {
                Logger.Info("Skipping ss preview, failed to get hwnd class.");
                return;
            }

            Logger.Info("Showing ss preview..");
            var preview = new ScreensaverPreview
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
                NLogger.LogWin32Error("Setwindowpos fail Preview Screensaver,");
            }
        }

        #endregion //public

        #region screensavers

        /// <summary>
        /// Detaches wallpapers from desktop workerw.
        /// </summary>
        private void ShowScreensavers()
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
        private void HideScreensavers()
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

        private void ShowBlankScreensavers()
        {
            if (!Program.SettingsVM.Settings.ScreensaverEmptyScreenShowBlack ||
                (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span && SetupDesktop.Wallpapers.Count > 0))
            {
                return;
            }

            _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
              {
                  var freeScreens = ScreenHelper.GetScreen().FindAll(
                      x => !SetupDesktop.Wallpapers.Exists(y => y.GetScreen().Equals(x)));

                  foreach (var item in freeScreens)
                  {
                      var bWindow = new ScreensaverBlank
                      {
                          Left = item.Bounds.Left,
                          Top = item.Bounds.Top,
                          WindowState = WindowState.Maximized,
                          WindowStyle = WindowStyle.None,
                          Topmost = true,
                      };
                      bWindow.Show();
                      blankWindows.Add(bWindow);
                  }
              }));
        }

        private void CloseBlankScreensavers()
        {
            _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
              {
                  blankWindows.ForEach(x => x.Close());
                  blankWindows.Clear();
              }));
        }

        #endregion //screensavers

        #region input checks

        private void IdleCheckTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (GetLastInputTime() >= idleWaitTime && !IsExclusiveFullScreenAppRunning())
                {
                    Start();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                //StopIdleTimer();
            }
        }

        private void StartInputListener()
        {
            SetupDesktop.RawInputHook.MouseMoveRaw += RawInputHook_MouseMoveRaw;
            SetupDesktop.RawInputHook.MouseDownRaw += RawInputHook_MouseDownRaw;
            SetupDesktop.RawInputHook.KeyboardClickRaw += RawInputHook_KeyboardClickRaw;
        }

        private void StopInputListener()
        {
            SetupDesktop.RawInputHook.MouseMoveRaw -= RawInputHook_MouseMoveRaw;
            SetupDesktop.RawInputHook.MouseDownRaw -= RawInputHook_MouseDownRaw;
            SetupDesktop.RawInputHook.KeyboardClickRaw -= RawInputHook_KeyboardClickRaw;
        }

        private void RawInputHook_KeyboardClickRaw(object sender, Core.KeyboardClickRawArgs e)
        {
            Stop();
        }

        private void RawInputHook_MouseDownRaw(object sender, Core.MouseClickRawArgs e)
        {
            Stop();
        }

        private void RawInputHook_MouseMoveRaw(object sender, Core.MouseRawArgs e)
        {
            Stop();
        }

        #endregion //input checks

        #region helpers

        private static void LockWorkStationSafe()
        {
            if (!NativeMethods.LockWorkStation())
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        // Fails after 50 days (uint limit.)
        private static uint GetLastInputTime()
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
                throw new Win32Exception("GetLastInputTime fail.");
            }
        }

        private static bool IsExclusiveFullScreenAppRunning()
        {
            if (NativeMethods.SHQueryUserNotificationState(out NativeMethods.QUERY_USER_NOTIFICATION_STATE state) == 0)
            {
                return state switch
                {
                    NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_NOT_PRESENT => false,
                    NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY => false,
                    NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE => false,
                    NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_ACCEPTS_NOTIFICATIONS => false,
                    NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME => false,
                    NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN => true,
                    _ => false,
                };
            }
            else
            {
                throw new Win32Exception("SHQueryUserNotificationState fail.");
            }
        }

        #endregion //helpers
    }
}
