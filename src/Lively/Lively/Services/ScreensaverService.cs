using Lively.Common;
using Lively.Common.Factories;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Extensions;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Views;
using Lively.Views.WindowMsg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace Lively.Services
{
    public class ScreensaverService : IScreensaverService
    {
        public bool IsRunning { get; private set; } = false;
        public ScreensaverApplyMode Mode => ScreensaverApplyMode.process;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<Window> blankWindows = [];
        private readonly List<WallpaperPreview> screensaverWindows = [];
        private readonly Timer idleTimer = new();
        private DwmThumbnailWindow dwmThumbnailWindow;
        private Window inputWindow;
        private uint idleWaitTime = 300000;
        private bool startAsyncExecuting;

        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly RawInputMsgWindow rawInput;

        public event EventHandler Stopped;

        public ScreensaverService(IUserSettingsService userSettings,
            IDesktopCore desktopCore,
            RawInputMsgWindow rawInput,
            IDisplayManager displayManager,
            IWallpaperLibraryFactory wallpaperLibraryFactory)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.rawInput = rawInput;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;

            displayManager.DisplayUpdated += DisplayManager_DisplayUpdated;
            idleTimer.Elapsed += IdleCheckTimer;
            idleTimer.Interval = 30000;
        }

        public async Task StartAsync()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            startAsyncExecuting = true;
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(async () =>
            {
                // Move cursor outside screen region.
                _ = NativeMethods.SetCursorPos(int.MaxValue, 0);
                Logger.Info("Starting screensaver..");
                await ShowScreensavers();
                startAsyncExecuting = false;
            }));
        }

        public void Stop()
        {
            if (!IsRunning || startAsyncExecuting)
                return;

            IsRunning = false;
            _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                Logger.Info("Stopping screensaver..");
                CloseScreensavers();
                // Lock screen.
                if (userSettings.Settings.ScreensaverLockOnResume)
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

                Stopped?.Invoke(this, EventArgs.Empty);
            }));
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

        private void DisplayManager_DisplayUpdated(object sender, EventArgs e)
        {
            Stop();
        }

        private async Task ShowScreensavers()
        {
            switch (Mode)
            {
                case ScreensaverApplyMode.wallpaper:
                    {
                        ShowRunningWallpaperAsScreensaver();
                        StartInputListener();
                    }
                    break;
                case ScreensaverApplyMode.process:
                    {
                        await ShowWindowAsScreensaver();
                        StartInputListener();
                    }
                    break;
                case ScreensaverApplyMode.dwmThumbnail:
                    {
                        ShowDwmThumbnailAsScreensaver();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void CloseScreensavers()
        {
            switch (Mode)
            {
                case ScreensaverApplyMode.wallpaper:
                    {
                        CloseRunningWallpaperAsScreensaver();
                        CloseBlankWindowAsScreensaver();
                        StopInputListener();
                    }
                    break;
                case ScreensaverApplyMode.process:
                    {
                        CloseWindowAsScreensaver();
                        StopInputListener();
                    }
                    break;
                case ScreensaverApplyMode.dwmThumbnail:
                    {
                        CloseDwmThumbnailAsScreensaver();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task ShowWindowAsScreensaver()
        {
            WallpaperArrangement arrangement = WallpaperArrangement.per;
            List<WallpaperLayoutModel> wallpaperLayout = null;
            switch (userSettings.Settings.ScreensaverType)
            {
                case ScreensaverType.wallpaper:
                    {
                        wallpaperLayout = userSettings.WallpaperLayout;
                        arrangement = userSettings.Settings.WallpaperArrangement;
                    }
                    break;
                case ScreensaverType.different:
                    {
                        try
                        {
                            var screensavers = JsonStorage<List<ScreenSaverLayoutModel>>.LoadData(Constants.CommonPaths.ScreenSaverLayoutPath);
                            arrangement = userSettings.Settings.ScreensaverArragement;
                            wallpaperLayout = screensavers.Find(x => x.Layout == arrangement)?.Wallpapers;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to read Screensaver config file. | {ex}");
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (wallpaperLayout is null || wallpaperLayout.Count == 0)
            {
                // Protect screen regardless wallpaper state.
                ShowBlankWindowAsScreensaver(displayManager.VirtualScreenBounds);
                return;
            }

            switch (arrangement)
            {
                case WallpaperArrangement.per:
                    {
                        foreach (var layout in wallpaperLayout)
                        {
                            try
                            {
                                var model = wallpaperLibraryFactory.CreateFromDirectory(layout.LivelyInfoPath);
                                var screen = displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(layout.Display));
                                if (screen is null)
                                    Logger.Info($"Screen missing, skipping screensaver {layout.LivelyInfoPath} | {layout.Display.DeviceName}");
                                else
                                {
                                    Logger.Info($"Starting screensaver {model.Title} | {model.LivelyInfoFolderPath} | {layout.Display.Bounds}");
                                    await ShowPreviewWindowAsScreensaver(model, layout.Display);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Info($"Failed to load Screensaver {layout.LivelyInfoPath} | {ex}");
                                // Protect screen regardless wallpaper state.
                                ShowBlankWindowAsScreensaver(layout.Display);
                            }
                        }
                        // Show black screen to protect display if no wallpaper.
                        foreach (var display in displayManager.DisplayMonitors.Where(x => !wallpaperLayout.Exists(y => y.Display.Equals(x))))
                            ShowBlankWindowAsScreensaver(display);
                    }
                    break;
                case WallpaperArrangement.span:
                    {       
                        try
                        {
                            var model = wallpaperLibraryFactory.CreateFromDirectory(wallpaperLayout.FirstOrDefault()?.LivelyInfoPath);
                            await ShowPreviewWindowAsScreensaver(model, displayManager.VirtualScreenBounds);
                        }
                        catch (Exception ex)
                        {
                            Logger.Info($"Failed to load Screensaver {wallpaperLayout.FirstOrDefault()?.LivelyInfoPath} | {ex}");
                            // Protect screen regardless wallpaper state.
                            ShowBlankWindowAsScreensaver(displayManager.VirtualScreenBounds);
                        }
                    }
                    break;
                case WallpaperArrangement.duplicate:
                    {
                        try
                        {
                            var model = wallpaperLibraryFactory.CreateFromDirectory(wallpaperLayout.FirstOrDefault()?.LivelyInfoPath);
                            foreach (var display in displayManager.DisplayMonitors)
                            {
                                await ShowPreviewWindowAsScreensaver(model, display);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Info($"Failed to load Screensaver {wallpaperLayout.FirstOrDefault()?.LivelyInfoPath} | {ex}");
                            // Protect screen regardless wallpaper state.
                            ShowBlankWindowAsScreensaver(displayManager.VirtualScreenBounds);
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task ShowPreviewWindowAsScreensaver(LibraryModel model, DisplayMonitor display)
        {
            var window = new WallpaperPreview(model, display, userSettings.Settings.ScreensaverArragement, false, false)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                BorderThickness = new Thickness(0),
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
            };
            window.Show();
            window.NativeMove(display.Bounds);
            window.WindowState = WindowState.Maximized;
            await window.LoadWallpaperAsync();

            screensaverWindows.Add(window);
        }

        private async Task ShowPreviewWindowAsScreensaver(LibraryModel model, Rectangle rect)
        {
            var window = new WallpaperPreview(model, displayManager.PrimaryDisplayMonitor, userSettings.Settings.ScreensaverArragement, false, false)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                BorderThickness = new Thickness(0),
                Topmost = true,
            };
            window.Show();
            window.NativeResize(rect);
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            await window.LoadWallpaperAsync();

            screensaverWindows.Add(window);
        }

        private void CloseWindowAsScreensaver()
        {
            screensaverWindows.ForEach(x => x.Close());
            screensaverWindows.Clear();
            CloseBlankWindowAsScreensaver();
        }

        private void ShowBlankWindowAsScreensaver(DisplayMonitor display)
        {
            var window = new BlankWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                BorderThickness = new Thickness(0),
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
            };
            window.Show();
            window.NativeMove(display.Bounds);
            window.WindowState = WindowState.Maximized;

            blankWindows.Add(window);
        }

        private void ShowBlankWindowAsScreensaver(Rectangle bounds)
        {
            var window = new BlankWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                BorderThickness = new Thickness(0),
                Topmost = true,
            };
            window.Show();
            window.NativeResize(bounds);
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;

            blankWindows.Add(window);
        }

        private void CloseBlankWindowAsScreensaver()
        {
            blankWindows.ForEach(x => x.Close());
            blankWindows.Clear();
        }

        private void ShowRunningWallpaperAsScreensaver()
        {
            foreach (var item in desktopCore.Wallpapers)
            {
                //detach wallpaper.
                WindowUtil.SetParentSafe(item.Handle, IntPtr.Zero);
                //show on the currently running screen, not changing size.
                if (!NativeMethods.SetWindowPos(
                    item.Handle,
                    -1, //topmost
                    userSettings.Settings.WallpaperArrangement != WallpaperArrangement.span ? item.Screen.Bounds.Left : 0,
                    userSettings.Settings.WallpaperArrangement != WallpaperArrangement.span ? item.Screen.Bounds.Top : 0,
                    item.Screen.Bounds.Width,
                    item.Screen.Bounds.Height,
                    userSettings.Settings.WallpaperArrangement != WallpaperArrangement.span ? 0x0040 : 0x0001)) //ignore WxH if span
                {
                    Logger.Error(LogUtil.GetWin32Error("Screensaver show fail"));
                }
            }
        }

        private void CloseRunningWallpaperAsScreensaver()
        {
            if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span)
            {
                if (desktopCore.Wallpapers.Count > 0)
                {
                    //get spawned workerw rectangle data.
                    NativeMethods.GetWindowRect(desktopCore.DesktopWorkerW, out NativeMethods.RECT prct);
                    WindowUtil.SetParentSafe(desktopCore.Wallpapers[0].Handle, desktopCore.DesktopWorkerW);
                    //fill wp into the whole workerw area.
                    if (!NativeMethods.SetWindowPos(desktopCore.Wallpapers[0].Handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0x0010))
                    {
                        Logger.Error(LogUtil.GetWin32Error("Screensaver hide fail"));
                    }
                }
            }
            else
            {
                foreach (var item in desktopCore.Wallpapers)
                {
                    //update position & size incase window is moved.
                    if (!NativeMethods.SetWindowPos(item.Handle, 1, item.Screen.Bounds.Left, item.Screen.Bounds.Top, item.Screen.Bounds.Width, item.Screen.Bounds.Height, 0x0010))
                    {
                        //LogUtil.LogWin32Error("Failed to hide screensaver(2)");
                    }
                    //re-calcuate position on desktop workerw.
                    NativeMethods.RECT prct = new NativeMethods.RECT();
                    NativeMethods.MapWindowPoints(item.Handle, desktopCore.DesktopWorkerW, ref prct, 2);
                    //re-attach wallpaper to desktop.
                    WindowUtil.SetParentSafe(item.Handle, desktopCore.DesktopWorkerW);
                    //update position & size on desktop workerw.
                    if (!NativeMethods.SetWindowPos(item.Handle, 1, prct.Left, prct.Top, item.Screen.Bounds.Width, item.Screen.Bounds.Height, 0x0010))
                    {
                        //LogUtil.LogWin32Error("Failed to hide screensaver(3)");
                    }
                }
            }
            DesktopUtil.RefreshDesktop();
        }

        private void ShowDwmThumbnailAsScreensaver()
        {
            var progman = NativeMethods.FindWindow("Progman", null);
            _ = NativeMethods.GetWindowRect(progman, out NativeMethods.RECT prct);
            int width = prct.Right - prct.Left,
                height = prct.Bottom - prct.Top;

            dwmThumbnailWindow = new(progman, new Rectangle(0, 0, width, height), new Rectangle(prct.Left, prct.Top, width, height))
            {
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Topmost = true,
                AutoSizeDwmWindow = true
            };
            dwmThumbnailWindow.InputReceived += DwmThumbnailWindow_InputReceived;
            dwmThumbnailWindow.Show();
        }

        private void CloseDwmThumbnailAsScreensaver()
        {
            if (dwmThumbnailWindow is null)
                return;

            dwmThumbnailWindow.InputReceived -= DwmThumbnailWindow_InputReceived;
            dwmThumbnailWindow.Close();
            dwmThumbnailWindow = null;
        }

        private void DwmThumbnailWindow_InputReceived(object sender, EventArgs e) => Stop();

        private async void IdleCheckTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (GetLastInputTime() >= idleWaitTime && !IsExclusiveFullScreenAppRunning())
                {
                    await StartAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                //StopIdleTimer();
            }
        }

        /// <summary>
        /// Attaches screensaver preview to preview region. <br>
        /// (To be run in UI thread.)</br>
        /// </summary>
        /// <param name="hwnd"></param>
        public void CreatePreview(IntPtr hwnd)
        {
            //Issue: Multiple display setup with diff dpi - making the window child affects DisplayMonitor offset values.
            if (IsRunning || displayManager.IsMultiScreen())
                return;

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
            var preview = new ScreenSaverPreview
            {
                ShowActivated = false,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -9999,
            };
            preview.Show();
            var previewHandle = new WindowInteropHelper(preview).Handle;
            //Set child of target.
            WindowUtil.SetParentSafe(previewHandle, hwnd);
            //Make this a child window so it will close when the parent dialog closes.
            NativeMethods.SetWindowLongPtr(new HandleRef(null, previewHandle),
                (int)NativeMethods.GWL.GWL_STYLE,
                new IntPtr(NativeMethods.GetWindowLong(previewHandle, (int)NativeMethods.GWL.GWL_STYLE) | NativeMethods.WindowStyles.WS_CHILD));
            //Get size of target.
            NativeMethods.GetClientRect(hwnd, out NativeMethods.RECT prct);
            //Update preview size and position.
            if (!NativeMethods.SetWindowPos(previewHandle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0x0010))
            {
                //TODO
            }
        }

        private void StartInputListener()
        {
            rawInput.MouseMoveRaw += RawInputHook_MouseMoveRaw;
            rawInput.MouseDownRaw += RawInputHook_MouseDownRaw;
            rawInput.KeyboardClickRaw += RawInputHook_KeyboardClickRaw;
        }

        private void StopInputListener()
        {
            rawInput.MouseMoveRaw -= RawInputHook_MouseMoveRaw;
            rawInput.MouseDownRaw -= RawInputHook_MouseDownRaw;
            rawInput.KeyboardClickRaw -= RawInputHook_KeyboardClickRaw;
        }

        //private void StartInputWindowListener()
        //{
        //    var window = new TransparentWindow
        //    {
        //        WindowStartupLocation = WindowStartupLocation.CenterScreen,
        //        ShowActivated = true,
        //        Topmost = true,
        //    };
        //    window.Show();
        //    window.NativeResize(displayManager.VirtualScreenBounds);
        //    window.PreviewTouchDown += (_, _) =>
        //    {
        //        Stop();
        //    };
        //    window.PreviewMouseDown += (_, _) =>
        //    {
        //        Stop();
        //    };
        //    inputWindow = window;
        //}

        //private void StopInputWindowListener()
        //{
        //    inputWindow?.Close();
        //    inputWindow = null;
        //}

        private void RawInputHook_KeyboardClickRaw(object sender, KeyboardClickRawArgs e) => Stop();

        private void RawInputHook_MouseDownRaw(object sender, MouseClickRawArgs e) => Stop();

        private void RawInputHook_MouseMoveRaw(object sender, MouseRawArgs e) => Stop();

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

        //private Rectangle GetDesktopRect()
        //{
        //    var progman = NativeMethods.FindWindow("Progman", null);
        //    _ = NativeMethods.GetWindowRect(progman, out NativeMethods.RECT prct);
        //    int width = prct.Right - prct.Left,
        //        height = prct.Bottom - prct.Top;
        //    return new Rectangle(prct.Left, prct.Top, width, height);
        //}
    }
}
