using Lively.Common;
using Lively.Common.Factories;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Core.Display;
using Lively.Extensions;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Views;
using Lively.Views.WindowMsg;
using Microsoft.Win32;
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
using Windows.Media.Control;
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
        private DwmThumbnailWindow dwmThumbnailWindow;
        private bool startAsyncExecuting, stopAsyncExecuting;
        private DateTime? startTime;

        private readonly IUserSettingsService userSettings;
        private readonly IDisplayManager displayManager;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly RawInputMsgWindow rawInput;

        public event EventHandler Stopped;

        public ScreensaverService(IUserSettingsService userSettings,
            RawInputMsgWindow rawInput,
            IDisplayManager displayManager,
            IWallpaperLibraryFactory wallpaperLibraryFactory)
        {
            this.userSettings = userSettings;
            this.displayManager = displayManager;
            this.rawInput = rawInput;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;

            displayManager.DisplayUpdated += DisplayManager_DisplayUpdated;
        }

        public async Task StartAsync(bool isFadeIn)
        {
            if (IsRunning || startAsyncExecuting || stopAsyncExecuting)
                return;

            startAsyncExecuting = true;
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, async () =>
            {
                // Fade-in
                // Ref: https://github.com/rocksdanister/lively/issues/2736
                BlankWindow fadeInWindow = null;
                try
                {
                    if (isFadeIn)
                    {
                        Logger.Info("Showing screensaver transition..");
                        var tcs = new TaskCompletionSource<bool>();
                        fadeInWindow = new BlankWindow(fadeInDuration: 10000, fadeOutDuration: 500)
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            BorderThickness = new Thickness(0),
                            ShowInTaskbar = false,
                            ShowActivated = false,
                            Topmost = true
                        };
                        fadeInWindow.Loaded += (s, e) =>
                        {
                            fadeInWindow.NativeResize(displayManager.VirtualScreenBounds);
                            fadeInWindow.WindowStyle = WindowStyle.None;
                            fadeInWindow.ResizeMode = ResizeMode.NoResize;
                        };
                        fadeInWindow.FadeInAnimationCompleted += (s, e) => tcs.TrySetResult(true);
                        fadeInWindow.Show();

                        void RawInput_UserInput(object sender, object e) => tcs.TrySetResult(false);
                        rawInput.MouseMoveRaw += RawInput_UserInput;
                        rawInput.MouseDownRaw += RawInput_UserInput;
                        rawInput.KeyboardClickRaw += RawInput_UserInput;

                        await tcs.Task;
                        rawInput.MouseMoveRaw -= RawInput_UserInput;
                        rawInput.MouseDownRaw -= RawInput_UserInput;
                        rawInput.KeyboardClickRaw -= RawInput_UserInput;

                        if (!tcs.Task.Result)
                            return;
                    }

                    IsRunning = true;
                    startTime = DateTime.UtcNow;
                    // Move cursor outside screen region.
                    _ = NativeMethods.SetCursorPos(int.MaxValue, 0);
                    Logger.Info("Starting screensaver..");
                    await ShowScreensavers();
                    StartInputListener();
                }
                finally
                {
                    startAsyncExecuting = false;
                    fadeInWindow?.Close();
                }
            });
        }

        public async Task StopAsync()
        {
            if (!IsRunning || startAsyncExecuting || stopAsyncExecuting)
                return;

            stopAsyncExecuting = true;
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, async () =>
            {
                try
                {
                    Logger.Info("Stopping screensaver..");
                    StopInputListener();

                    // Lock screen.
                    var elapsed = DateTime.UtcNow - (startTime ?? DateTime.UtcNow);
                    if (userSettings.Settings.ScreensaverLockOnResume && elapsed.TotalSeconds > userSettings.Settings.ScreensaverGracePeriod)
                        await LockWorkstationAndWaitAsync(userSettings.Settings.ScreensaverLockWaitTimeout);

                    IsRunning = false;
                    CloseScreensavers();
                    Stopped?.Invoke(this, EventArgs.Empty);
                }
                finally
                { 
                    stopAsyncExecuting = false;
                    startTime = null;
                }
            });
        }

        private async void DisplayManager_DisplayUpdated(object sender, EventArgs e)
        {
            await StopAsync();
        }

        private async Task ShowScreensavers()
        {
            switch (Mode)
            {
                case ScreensaverApplyMode.process:
                    await ShowWindowAsScreensaver();
                    break;
                case ScreensaverApplyMode.dwmThumbnail:
                    ShowDwmThumbnailAsScreensaver();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void CloseScreensavers()
        {
            switch (Mode)
            {
                case ScreensaverApplyMode.process:
                    CloseWindowAsScreensaver();
                    break;
                case ScreensaverApplyMode.dwmThumbnail:
                    CloseDwmThumbnailAsScreensaver();
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
                                var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(layout.Display));
                                var volume = display?.IsPrimary == true ? userSettings.Settings.ScreensaverGlobalVolume : 0;
                                if (display is null)
                                    Logger.Info($"Screen missing, skipping screensaver {layout.LivelyInfoPath} | {layout.Display.DeviceName}");
                                else
                                {
                                    Logger.Info($"Starting screensaver {model.Title} | {model.LivelyInfoFolderPath} | {layout.Display.Bounds}");
                                    await ShowPreviewWindowAsScreensaver(model, display, volume);
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
                            await ShowPreviewWindowAsScreensaver(model, displayManager.VirtualScreenBounds, userSettings.Settings.ScreensaverGlobalVolume);
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
                                await ShowPreviewWindowAsScreensaver(model, display, display.IsPrimary ? userSettings.Settings.ScreensaverGlobalVolume : 0);
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

        private async Task ShowPreviewWindowAsScreensaver(LibraryModel model, DisplayMonitor display, int volume)
        {
            var window = new WallpaperPreview(
                model,
                display,
                userSettings.Settings.ScreensaverArragement,
                isAutoLoad: false)
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
            window.SetWallpaperVolume(volume);

            screensaverWindows.Add(window);
        }

        private async Task ShowPreviewWindowAsScreensaver(LibraryModel model, Rectangle rect, int volume)
        {
            var window = new WallpaperPreview(model,
                displayManager.PrimaryDisplayMonitor,
                userSettings.Settings.ScreensaverArragement,
                isAutoLoad: false)
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
            window.SetWallpaperVolume(volume);

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
            dwmThumbnailWindow.Show();
        }

        private void CloseDwmThumbnailAsScreensaver()
        {
            if (dwmThumbnailWindow is null)
                return;

            dwmThumbnailWindow.Close();
            dwmThumbnailWindow = null;
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
            WindowUtil.TrySetParent(previewHandle, hwnd);
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

        private async void RawInputHook_KeyboardClickRaw(object sender, KeyboardClickRawArgs e) => await StopAsync();

        private async void RawInputHook_MouseDownRaw(object sender, MouseClickRawArgs e) => await StopAsync();

        private async void RawInputHook_MouseMoveRaw(object sender, MouseRawArgs e) => await StopAsync();

        private static async Task LockWorkstationAndWaitAsync(double timeoutSeconds)
        {
            var lockTcs = new TaskCompletionSource<bool>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var _ = cts.Token.Register(() => lockTcs.TrySetResult(false));

            void SessionSwitchHandler(object s, SessionSwitchEventArgs e)
            {
                if (e.Reason == SessionSwitchReason.SessionLock)
                    lockTcs.TrySetResult(true);
            }
            SystemEvents.SessionSwitch += SessionSwitchHandler;

            try
            {
                // This method behaves async, ref: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-lockworkstation
                LockWorkStationSafe();
                await lockTcs.Task;
            }
            catch (Win32Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                SystemEvents.SessionSwitch -= SessionSwitchHandler;
            }
        }

        private static void LockWorkStationSafe()
        {
            if (!NativeMethods.LockWorkStation())
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
