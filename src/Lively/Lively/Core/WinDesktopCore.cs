using Lively.Common;
using Lively.Common.Com;
using Lively.Common.Exceptions;
using Lively.Common.Extensions;
using Lively.Common.Factories;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Helpers.Shell;
using Lively.Common.Services;
using Lively.Core.Display;
using Lively.Core.Watchdog;
using Lively.Extensions;
using Lively.Factories;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using Lively.ViewModels;
using Lively.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WinEventHook;

namespace Lively.Core
{
    public class WinDesktopCore : IDesktopCore
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly SemaphoreSlim semaphoreSlimWallpaperLoadingLock = new(1, 1);
        private readonly List<IWallpaper> wallpapers = new(2);
        public ReadOnlyCollection<IWallpaper> Wallpapers => wallpapers.AsReadOnly();
        private IntPtr workerw;
        public IntPtr DesktopWorkerW => workerw;
        private bool disposedValue;
        private readonly List<WallpaperLayoutModel> wallpapersDisconnected = new();

        public event EventHandler<WallpaperUpdateArgs> WallpaperUpdated;
        public event EventHandler<Exception> WallpaperError;
        public event EventHandler WallpaperChanged;
        public event EventHandler WallpaperReset;

        private readonly IUserSettingsService userSettings;
        private readonly IWallpaperPluginFactory wallpaperFactory;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly ITransparentTbService ttbService;
        private readonly IWatchdogService watchdog;
        private readonly IDisplayManager displayManager;
        private readonly IRunnerService runner;
        private readonly WindowEventHook workerWHook;

        public WinDesktopCore(IUserSettingsService userSettings,
            IDisplayManager displayManager,
            ITransparentTbService ttbService,
            IWatchdogService watchdog,
            IRunnerService runner,
            IWallpaperPluginFactory wallpaperFactory,
            IWallpaperLibraryFactory wallpaperLibraryFactory)
        {
            this.userSettings = userSettings;
            this.displayManager = displayManager;
            this.ttbService = ttbService;
            this.watchdog = watchdog;
            this.runner = runner;
            this.wallpaperFactory = wallpaperFactory;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;

            if (App.IsExclusiveScreensaverMode)
                return;

            if (SystemParameters.HighContrast)
                Logger.Warn("Highcontrast mode detected, some functionalities may not work properly.");

            this.displayManager.DisplayUpdated += DisplaySettingsChanged_Hwnd;
            WallpaperChanged += SetupDesktop_WallpaperChanged;

            SystemEvents.SessionSwitch += async(s, e) => {
                if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    //Issue: https://github.com/rocksdanister/lively/issues/802
                    if (!(DesktopWorkerW == IntPtr.Zero || NativeMethods.IsWindow(DesktopWorkerW)))
                    {
                        Logger.Info("WorkerW invalid after unlock, resetting..");
                        await ResetWallpaperAsync();
                    }
                    else
                    {
                        if (Wallpapers.Any(x => x.IsExited))
                        {
                            Logger.Info("Wallpaper crashed after unlock, resetting..");
                            await ResetWallpaperAsync();
                        }
                    }
                }
            };

            // Initialize WorkerW
            UpdateWorkerW();

            try
            {
                if (workerw != IntPtr.Zero)
                {
                    Logger.Info("Hooking WorkerW events..");
                    var dwThreadId = NativeMethods.GetWindowThreadProcessId(workerw, out int dwProcessId);
                    workerWHook = new WindowEventHook(WindowEvent.EVENT_OBJECT_DESTROY);
                    workerWHook.HookToThread(dwThreadId);
                    workerWHook.EventReceived += WorkerWHook_EventReceived;
                }
                else 
                {
                    Logger.Error("Failed to initialize Core, WorkerW is NULL");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"WorkerW hook failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the given wallpaper based on layout usersettings.
        /// </summary>
        public async Task SetWallpaperAsync(LibraryModel wallpaper, DisplayMonitor display)
        {
            await semaphoreSlimWallpaperLoadingLock.WaitAsync();

            try
            {
                Logger.Info($"Setting wallpaper: {wallpaper.Title} | {wallpaper.FilePath}");

                // Verify file exists if outside wallpaper install folder
                var fileExists = !wallpaper.LivelyInfo.IsAbsolutePath ? 
                    File.Exists(wallpaper.FilePath) : wallpaper.LivelyInfo.Type.IsOnlineWallpaper() || File.Exists(wallpaper.FilePath);
                if (!fileExists)
                {
                    Logger.Info($"Skipping wallpaper, file {wallpaper.LivelyInfo.FileName} not found.");
                    WallpaperError?.Invoke(this, new WallpaperNotFoundException($"{Properties.Resources.TextFileNotFound}\n{wallpaper.LivelyInfo.FileName}"));
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (!watchdog.IsRunning)
                    watchdog.Start();

                try
                {
                    switch (userSettings.Settings.WallpaperArrangement)
                    {
                        case WallpaperArrangement.per:
                            {
                                IWallpaper instance = wallpaperFactory.CreateWallpaper(wallpaper, display, userSettings.Settings.WallpaperArrangement, userSettings);
                                await instance.ShowAsync();
                                var dialogOk = await ShowWallpaperDialog(instance);
                                if (!dialogOk)
                                    return;

                                CloseWallpaper(instance.Screen, fireEvent: false, terminate: true);
                                if (!TrySetWallpaperPerScreen(instance.Handle, instance.Screen))
                                    Logger.Error("Failed to set wallpaper as child of WorkerW");

                                // Reload incase page does not handle resize event
                                if (instance.Category.IsWebWallpaper())
                                    instance.SetPlaybackPos(0, PlaybackPosType.absolutePercent);

                                await SetDesktopPictureOrLockscreen(instance);

                                if (instance.Proc != null)
                                    watchdog.Add(instance.Proc.Id);

                                wallpapers.Add(instance);
                            }
                            break;
                        case WallpaperArrangement.span:
                            {
                                IWallpaper instance = wallpaperFactory.CreateWallpaper(wallpaper, display, userSettings.Settings.WallpaperArrangement, userSettings);
                                await instance.ShowAsync();
                                var dialogOk = await ShowWallpaperDialog(instance);
                                if (!dialogOk)
                                    return;

                                CloseAllWallpapers(fireEvent: false, terminate: true);
                                if (!TrySetWallpaperSpanScreen(instance.Handle))
                                    Logger.Error("Failed to set wallpaper as child of WorkerW");

                                if (instance.Category.IsWebWallpaper())
                                    instance.SetPlaybackPos(0, PlaybackPosType.absolutePercent);

                                await SetDesktopPictureOrLockscreen(instance);

                                if (instance.Proc != null)
                                    watchdog.Add(instance.Proc.Id);

                                wallpapers.Add(instance);
                            }
                            break;
                        case WallpaperArrangement.duplicate:
                            {
                                CloseAllWallpapers(false, true);
                                foreach (var item in displayManager.DisplayMonitors)
                                {
                                    IWallpaper instance = wallpaperFactory.CreateWallpaper(wallpaper, item, userSettings.Settings.WallpaperArrangement, userSettings);
                                    await instance.ShowAsync();
                                    var dialogOk = await ShowWallpaperDialog(instance);
                                    if (!dialogOk)
                                        return;

                                    if (!TrySetWallpaperPerScreen(instance.Handle, instance.Screen))
                                        Logger.Error("Failed to set wallpaper as child of WorkerW");

                                    if (instance.Category.IsWebWallpaper())
                                        instance.SetPlaybackPos(0, PlaybackPosType.absolutePercent);

                                    await SetDesktopPictureOrLockscreen(instance);

                                    if (instance.Proc != null)
                                        watchdog.Add(instance.Proc.Id);

                                    wallpapers.Add(instance);
                                }

                                // Synchronizing position and audio
                                foreach (var item in wallpapers)
                                {
                                    if (item.Category.IsVideoWallpaper() && !item.Screen.IsPrimary)
                                    {
                                        Logger.Info($"Disabling audio track on screen {item.Screen.DeviceName} (duplicate.)");
                                        item.SetMute(true);
                                    }
                                    item.SetPlaybackPos(0, PlaybackPosType.absolutePercent);
                                }
                            }
                            break;
                    }
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (WallpaperPluginFactory.MsixNotAllowedException ex1)
                {
                    Logger.Error(ex1);
                    WallpaperError?.Invoke(this, new WallpaperPluginNotFoundException(ex1.Message));
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);

                    if (wallpaper.DataType == LibraryItemType.processing)
                    {
                        WallpaperUpdated?.Invoke(this, new WallpaperUpdateArgs() { Category = UpdateWallpaperType.remove, Info = wallpaper.LivelyInfo, InfoPath = wallpaper.LivelyInfoFolderPath });
                        //Deleting from core because incase UI client not running.
                        await FileUtil.TryDeleteDirectoryAsync(wallpaper.LivelyInfoFolderPath, 0, 1000);
                    }
                }
                catch (Win32Exception ex2)
                {
                    Logger.Error(ex2);
                    if (ex2.NativeErrorCode == 2) //ERROR_FILE_NOT_FOUND
                        WallpaperError?.Invoke(this, new WallpaperPluginNotFoundException(ex2.Message));
                    else
                        WallpaperError?.Invoke(this, ex2);
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex3)
                {
                    Logger.Error(ex3);
                    WallpaperError?.Invoke(this, ex3);
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                semaphoreSlimWallpaperLoadingLock.Release();
            }
        }

        private void UpdateWorkerW()
        {
            Logger.Info("WorkerW initializing..");
            var retries = 5;
            while (true)
            {
                workerw = CreateWorkerW();
                if (workerw != IntPtr.Zero) {
                    break;
                }
                else
                {
                    retries--;
                    if (retries == 0)
                        break;

                    Logger.Error($"Failed to create WorkerW, retrying ({retries})..");
                }
            }
            Logger.Info($"WorkerW initialized {workerw}");
            WallpaperReset?.Invoke(this, EventArgs.Empty);
        }

        private async void WorkerWHook_EventReceived(object sender, WinEventHookEventArgs e)
        {
            if (e.WindowHandle == workerw && e.EventType == WindowEvent.EVENT_OBJECT_DESTROY)
            {
                Logger.Error("WorkerW destroyed.");
                await ResetWallpaperAsync();
            }
        }

        private async Task<bool> ShowWallpaperDialog(IWallpaper wallpaper)
        {
            var cancelled = false;
            switch (wallpaper.Model.DataType)
            {
                case LibraryItemType.edit:
                case LibraryItemType.processing:
                case LibraryItemType.multiImport:
                case LibraryItemType.cmdImport:
                    try
                    {
                        runner.SetBusyUI(true);
                        //backup.. once processed is done, becomes ready.
                        var type = wallpaper.Model.DataType;
                        if (type == LibraryItemType.edit)
                        {
                            CloseWallpaper(wallpaper.Model, terminate: true);
                        }
                        var tcs = new TaskCompletionSource<object>();
                        var thread = new Thread(() =>
                        {
                            try
                            {
                                _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
                                {
                                    var pWindow = new LibraryPreview(wallpaper)
                                    {
                                        Topmost = true,
                                        ShowActivated = true,
                                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                                    };
                                    //pWindow.Closed += (s, a) => tcs.SetResult(null);
                                    var vm = (LibraryPreviewViewModel)pWindow.DataContext;
                                    vm.DetailsUpdated += (s, e) =>
                                    {
                                        cancelled = e.Category == UpdateWallpaperType.remove;
                                        if (cancelled || e.Category == UpdateWallpaperType.done)
                                        {
                                            tcs.SetResult(null);
                                        }
                                        WallpaperUpdated?.Invoke(this, e);
                                    };
                                    pWindow.Show();
                                    if (runner.IsVisibleUI)
                                        pWindow.CenterToWindow(runner.HwndUI);
                                }));
                            }
                            catch (Exception e)
                            {
                                tcs.SetException(e);
                                Logger.Error(e);
                            }
                        });
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        await tcs.Task;

                        if (type == LibraryItemType.edit)
                        {
                            wallpaper.Terminate();
                            return false;
                        }
                        else if (type == LibraryItemType.multiImport)
                        {
                            wallpaper.Terminate();
                            WallpaperChanged?.Invoke(this, EventArgs.Empty);
                            return false;
                        }
                    }
                    finally
                    {
                        runner.SetBusyUI(false);
                    }
                    break;
                case LibraryItemType.ready:
                    break;
                default:
                    break;
            }

            if (cancelled)
            {
                //User cancelled/fail!
                wallpaper.Terminate();
                DesktopUtil.RefreshDesktop();

                try
                {
                    //Deleting here incase UI client is not running
                    await FileUtil.TryDeleteDirectoryAsync(wallpaper.Model.LivelyInfoFolderPath, 0, 1000);
                    if (wallpaper.LivelyPropertyCopyPath != null)
                        await FileUtil.TryDeleteDirectoryAsync(Directory.GetParent(Path.GetDirectoryName(wallpaper.LivelyPropertyCopyPath)).FullName, 0, 1000);
                }
                catch (Exception ie)
                {
                    Logger.Error(ie);
                }
            }

            return !cancelled;
        }

        private async Task SetDesktopPictureOrLockscreen(IWallpaper wallpaper)
        {
            //Only consider PrimaryScreen for calculating average color
            var thumbRequiredAvgColor = (userSettings.Settings.SystemTaskbarTheme == TaskbarTheme.wallpaper || userSettings.Settings.SystemTaskbarTheme == TaskbarTheme.wallpaperFluent)
                && (!displayManager.IsMultiScreen() || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span || wallpaper.Screen.IsPrimary);
            if (userSettings.Settings.DesktopAutoWallpaper || thumbRequiredAvgColor)
            {
                try
                {
                    int maxIterations = 50;
                    //upto ~5sec wait for wallpaper to get ready..
                    for (int i = 1; i <= maxIterations; i++)
                    {
                        if (i == maxIterations)
                            throw new Exception("Timed out..");

                        if (wallpaper.IsLoaded)
                            break;

                        await Task.Delay(100);
                    }

                    //capture frame from wallpaper..
                    var imgPath = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName() + ".jpg");
                    await wallpaper.ScreenCapture(imgPath);
                    if (!File.Exists(imgPath))
                    {
                        throw new FileNotFoundException();
                    }

                    //set accent color of taskbar..
                    if (thumbRequiredAvgColor)
                    {
                        try
                        {
                            var color = await Task.Run(() => ttbService.GetAverageColor(imgPath));
                            ttbService.SetAccentColor(color);
                        }
                        catch (Exception ie1)
                        {
                            Logger.Error($"Failed to set taskbar accent: {ie1.Message}");
                        }
                    }

                    //set desktop picture wallpaper..
                    if (userSettings.Settings.DesktopAutoWallpaper)
                    {
                        if (true)//displayManager.IsMultiScreen())
                        {
                            //Has transition animation..
                            var desktop = (IDesktopWallpaper)new DesktopWallpaperClass();
                            DesktopWallpaperPosition scaler = DesktopWallpaperPosition.Fill;
                            switch (userSettings.Settings.WallpaperScaling)
                            {
                                case WallpaperScaler.none:
                                    scaler = DesktopWallpaperPosition.Center;
                                    break;
                                case WallpaperScaler.fill:
                                    scaler = DesktopWallpaperPosition.Stretch;
                                    break;
                                case WallpaperScaler.uniform:
                                    scaler = DesktopWallpaperPosition.Fit;
                                    break;
                                case WallpaperScaler.uniformFill:
                                    //not exaclty the same, lively's uniform fill pivot is topleft whereas for windows its center.
                                    scaler = DesktopWallpaperPosition.Fill;
                                    break;
                            }
                            desktop.SetPosition(userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span ? DesktopWallpaperPosition.Span : scaler);
                            desktop.SetWallpaper(userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span ? null : wallpaper.Screen.DeviceId, DesktopBridgeUtil.GetVirtualizedPath(imgPath));
                        }
                        else
                        {
                            //No transition animation..
                            _ = NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER,
                                0,
                                DesktopBridgeUtil.GetVirtualizedPath(imgPath),
                                NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDWININICHANGE);
                        }
                    }
                }
                catch (Exception ie2)
                {
                    Logger.Error($"Failed to set lockscreen/desktop wallpaper: {ie2.Message}");
                }
            }
        }

        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private bool TrySetWallpaperPerScreen(IntPtr handle, DisplayMonitor targetDisplay)
        {
            NativeMethods.RECT prct = new NativeMethods.RECT();
            Logger.Info($"Sending wallpaper(Screen): {targetDisplay.DeviceName} | {targetDisplay.Bounds}");
            //Position the wp fullscreen to corresponding display.
            if (!NativeMethods.SetWindowPos(handle, 1, targetDisplay.Bounds.X, targetDisplay.Bounds.Y, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0x0010))
            {
                //LogUtil.LogWin32Error("Failed to set perscreen wallpaper(1)");
            }

            NativeMethods.MapWindowPoints(handle, workerw, ref prct, 2);
            var success = TrySetParentWorkerW(handle);

            //Position the wp window relative to the new parent window(workerw).
            if (!NativeMethods.SetWindowPos(handle, 1, prct.Left, prct.Top, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0x0010))
            {
                //LogUtil.LogWin32Error("Failed to set perscreen wallpaper(2)");
            }
            DesktopUtil.RefreshDesktop();
            return success;
        }

        /// <summary>
        /// Spans wp across all screens.
        /// </summary>
        private bool TrySetWallpaperSpanScreen(IntPtr handle)
        {
            //get spawned workerw rectangle data.
            NativeMethods.GetWindowRect(workerw, out NativeMethods.RECT prct);
            var success = TrySetParentWorkerW(handle);

            //fill wp into the whole workerw area.
            Logger.Info($"Sending wallpaper(Span): ({prct.Left}, {prct.Top}, {prct.Right - prct.Left}, {prct.Bottom - prct.Top}).");
            if (!NativeMethods.SetWindowPos(handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0x0010))
            {
                //LogUtil.LogWin32Error("Failed to set span wallpaper");
            }
            DesktopUtil.RefreshDesktop();
            return success;
        }

        /// <summary>
        /// Reset workerw.
        /// </summary>
        public async Task ResetWallpaperAsync()
        {
            await semaphoreSlimWallpaperLoadingLock.WaitAsync();

            try
            {
                Logger.Info("Restarting wallpaper service..");
                // Copy existing wallpapers
                var originalWallpapers = Wallpapers.ToList();
                CloseAllWallpapers(true);
                // Restart workerw
                UpdateWorkerW();
                if (workerw == IntPtr.Zero)
                {
                    // Final attempt
                    Logger.Info("Retry creating WorkerW after delay..");
                    await Task.Delay(500);
                    UpdateWorkerW();
                }
                foreach (var item in originalWallpapers)
                {
                    SetWallpaperAsync(item.Model, item.Screen);
                    if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                        break;
                }
            }
            finally
            {
                semaphoreSlimWallpaperLoadingLock.Release();
            }
        }

        public async Task RestartWallpaper()
        {
            // Copy existing wallpapers
            var originalWallpapers = Wallpapers.ToList();
            CloseAllWallpapers(true);
            foreach (var item in originalWallpapers)
            {
                await SetWallpaperAsync(item.Model, item.Screen);
                if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                    break;
            }

        }

        public async Task RestartWallpaper(DisplayMonitor display)
        {
            // Copy existing wallpapers
            var originalWallpapers = Wallpapers.Where(x => x.Screen.Equals(display)).ToList();
            CloseWallpaper(display, true);
            foreach (var item in originalWallpapers)
            {
                await SetWallpaperAsync(item.Model, item.Screen);
                if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                    break;
            }

        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            SaveWallpaperLayout();
        }

        readonly object layoutWriteLock = new object();
        private void SaveWallpaperLayout()
        {
            lock (layoutWriteLock)
            {
                userSettings.WallpaperLayout.Clear();
                wallpapers.ForEach(wallpaper =>
                {
                    userSettings.WallpaperLayout.Add(new WallpaperLayoutModel(
                            (DisplayMonitor)wallpaper.Screen,
                            wallpaper.Model.LivelyInfoFolderPath));
                });
                if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.per)
                {
                    userSettings.WallpaperLayout.AddRange(wallpapersDisconnected);
                }
                /*
                layout.AddRange(wallpapersDisconnected.Except(wallpapersDisconnected.FindAll(
                    layout => Wallpapers.FirstOrDefault(wp => ScreenHelper.ScreenCompare(layout.LivelyScreen, wp.GetScreen(), DisplayIdentificationMode.deviceId)) != null)));
                */
                try
                {
                    userSettings.Save<List<WallpaperLayoutModel>>();
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
        }

        private async void DisplaySettingsChanged_Hwnd(object sender, EventArgs e)
        {
            // SetWallpaperAsync() is called here but not awaited so should be fine.
            // Only possible case of deadlock is if both methods gets executed simulataneously which is unlikely.
            // If required add a timeout to WaitAsync() for all the semaphore calls.
            await semaphoreSlimWallpaperLoadingLock.WaitAsync();
            try
            {
                Logger.Info("Display settings changed, screen(s):");
                displayManager.DisplayMonitors.ToList().ForEach(x => Logger.Info(x.DeviceName + " " + x.Bounds));
                RefreshWallpaper();
                RestoreDisconnectedWallpapers();
            }
            finally
            {
                semaphoreSlimWallpaperLoadingLock.Release();
            }
        }

        private void RefreshWallpaper()
        {
            try
            {
                //Wallpapers still running on disconnected screens.
                var allScreens = displayManager.DisplayMonitors.ToList();
                var orphanWallpapers = wallpapers.FindAll(
                    wallpaper => allScreens.Find(
                        screen => wallpaper.Screen.Equals(screen)) == null);

                //Updating user selected screen to primary if disconnected.
                userSettings.Settings.SelectedDisplay =
                    allScreens.Find(x => userSettings.Settings.SelectedDisplay.Equals(x)) ??
                    displayManager.PrimaryDisplayMonitor;
                userSettings.Save<SettingsModel>();

                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        //No screens running wallpaper needs to be removed.
                        if (orphanWallpapers.Count != 0)
                        {
                            orphanWallpapers.ForEach(x =>
                            {
                                Logger.Info($"Disconnected Screen: {x.Screen.DeviceName} {x.Screen.Bounds}");
                                x.Close();
                            });

                            var newOrphans = orphanWallpapers.FindAll(
                                oldOrphan => wallpapersDisconnected.Find(
                                    newOrphan => newOrphan.Display.Equals(oldOrphan.Screen)) == null);
                            foreach (var item in newOrphans)
                            {
                                wallpapersDisconnected.Add(new WallpaperLayoutModel((DisplayMonitor)item.Screen, item.Model.LivelyInfoFolderPath));
                            }
                            wallpapers.RemoveAll(x => orphanWallpapers.Contains(x));
                        }
                        break;
                    case WallpaperArrangement.duplicate:
                        if (orphanWallpapers.Count != 0)
                        {
                            orphanWallpapers.ForEach(x =>
                            {
                                Logger.Info($"Disconnected Screen: {x.Screen.DeviceName} {x.Screen.Bounds}");
                                x.Close();
                            });
                            wallpapers.RemoveAll(x => orphanWallpapers.Contains(x));
                        }
                        break;
                    case WallpaperArrangement.span:
                        //Only update wallpaper rect.
                        break;
                }
                //Desktop size change when screen is added/removed/property changed.
                UpdateWallpaperRect();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            finally
            {
                //Notifying display/wallpaper change.
                WallpaperChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateWallpaperRect()
        {
            if (displayManager.IsMultiScreen() && userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span)
            {
                if (wallpapers.Count != 0)
                {
                    //Wallpapers[0].Play();
                    var screenArea = displayManager.VirtualScreenBounds;
                    Logger.Info($"Updating wallpaper rect(Span): ({screenArea.Width}, {screenArea.Height}).");
                    //For play/pause, setting the new metadata.
                    Wallpapers[0].Screen = displayManager.PrimaryDisplayMonitor;
                    NativeMethods.SetWindowPos(Wallpapers[0].Handle, 1, 0, 0, screenArea.Width, screenArea.Height, 0x0010);
                }
            }
            else
            {
                int i;
                foreach (var screen in displayManager.DisplayMonitors.ToList())
                {
                    if ((i = wallpapers.FindIndex(x => x.Screen.Equals(screen))) != -1)
                    {
                        //Wallpapers[i].Play();
                        Logger.Info($"Updating wallpaper rect(Screen): {Wallpapers[i].Screen.Bounds} -> {screen.Bounds}.");
                        //For play/pause, setting the new metadata.
                        Wallpapers[i].Screen = screen;

                        var screenArea = displayManager.VirtualScreenBounds;
                        if (!NativeMethods.SetWindowPos(Wallpapers[i].Handle,
                                                        1,
                                                        (screen.Bounds.X - screenArea.Location.X),
                                                        (screen.Bounds.Y - screenArea.Location.Y),
                                                        (screen.Bounds.Width),
                                                        (screen.Bounds.Height),
                                                        0x0010))
                        {
                            //LogUtil.LogWin32Error("Failed to update wallpaper rect");
                        }
                    }
                }
            }
            DesktopUtil.RefreshDesktop();
        }

        private void RestoreDisconnectedWallpapers()
        {
            try
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        //Finding screens for previously removed wallpaper if screen reconnected..
                        var wallpapersToRestore = wallpapersDisconnected.FindAll(wallpaper => displayManager.DisplayMonitors.FirstOrDefault(
                            screen => wallpaper.Display.Equals(screen)) != null);
                        RestoreWallpaper(wallpapersToRestore);
                        break;
                    case WallpaperArrangement.span:
                        //UpdateWallpaperRect() should handle it normally.
                        //todo: if all screens disconnect?
                        break;
                    case WallpaperArrangement.duplicate:
                        if ((displayManager.DisplayMonitors.Count > Wallpapers.Count) && Wallpapers.Count != 0)
                        {
                            var newScreen = displayManager.DisplayMonitors.FirstOrDefault(screen => Wallpapers.FirstOrDefault(
                                wp => wp.Screen.Equals(screen)) == null);
                            if (newScreen != null)
                            {
                                //Only one call is required for multiple screens.
                                _ = SetWallpaperAsync(Wallpapers[0].Model, newScreen);
                            }
                        }
                        //todo: if all screens disconnect?
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to restore disconnected wallpaper(s): " + e.ToString());
            }
        }

        private void RestoreWallpaper(List<WallpaperLayoutModel> wallpaperLayout)
        {
            foreach (var layout in wallpaperLayout)
            {
                LibraryModel libraryItem = null;
                try
                {
                    libraryItem = wallpaperLibraryFactory.CreateFromDirectory(layout.LivelyInfoPath);
                }
                catch (Exception e)
                {
                    Logger.Info($"Skipping restoration of {layout.LivelyInfoPath} | {e.Message}");
                    wallpapersDisconnected.Remove(layout);
                }

                var screen = displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(layout.Display));
                if (screen == null)
                {
                    Logger.Info($"Screen missing, skipping restoration of {layout.LivelyInfoPath} | {layout.Display.DeviceName}");
                    if (!wallpapersDisconnected.Contains(layout))
                    {
                        Logger.Info($"Wallpaper queued to disconnected screenlist {layout.LivelyInfoPath} | {layout.Display.DeviceName}");
                        wallpapersDisconnected.Add(new WallpaperLayoutModel((DisplayMonitor)layout.Display, layout.LivelyInfoPath));
                    }
                }
                else
                {
                    Logger.Info($"Restoring wallpaper {libraryItem.Title} | {libraryItem.LivelyInfoFolderPath}");
                    _ = SetWallpaperAsync(libraryItem, screen);
                    wallpapersDisconnected.Remove(layout);
                }
            }
        }

        /// <summary>
        /// Restore wallpaper from save.
        /// </summary>
        public void RestoreWallpaper()
        {
            try
            {
                var wallpaperLayout = userSettings.WallpaperLayout;
                if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span ||
                    userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                {
                    if (wallpaperLayout.Count != 0)
                    {
                        var libraryItem = wallpaperLibraryFactory.CreateFromDirectory(wallpaperLayout[0].LivelyInfoPath);
                        SetWallpaperAsync(libraryItem, displayManager.PrimaryDisplayMonitor);
                    }
                }
                else if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.per)
                {
                    RestoreWallpaper(wallpaperLayout);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to restore wallpaper: {e}");
            }
        }

        public void CloseAllWallpapers(bool terminate = false)
        {
            CloseAllWallpapers(fireEvent: true, terminate: terminate);
        }

        private void CloseAllWallpapers(bool fireEvent, bool terminate)
        {
            if (Wallpapers.Count > 0)
            {
                if (terminate)
                {
                    wallpapers.ForEach(x => x.Terminate());
                }
                else
                {
                    wallpapers.ForEach(x => x.Close());
                }
                wallpapers.Clear();
                watchdog.Clear();

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void CloseWallpaper(DisplayMonitor display, bool terminate = false)
        {
            CloseWallpaper(display: display, fireEvent: true, terminate: terminate);
        }

        private void CloseWallpaper(DisplayMonitor display, bool fireEvent, bool terminate)
        {
            var tmp = wallpapers.FindAll(x => x.Screen.Equals(display));
            if (tmp.Count > 0)
            {
                tmp.ForEach(x =>
                {
                    if (x.Proc != null)
                    {
                        watchdog.Remove(x.Proc.Id);
                    }

                    if (terminate)
                    {
                        x.Terminate();
                    }
                    else
                    {
                        x.Close();
                    }
                });
                wallpapers.RemoveAll(x => tmp.Contains(x));

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void CloseWallpaper(WallpaperType type, bool terminate = false)
        {
            var tmp = wallpapers.FindAll(x => x.Category == type);
            if (tmp.Count > 0)
            {
                tmp.ForEach(x =>
                {
                    if (x.Proc != null)
                    {
                        watchdog.Remove(x.Proc.Id);
                    }

                    if (terminate)
                    {
                        x.Terminate();
                    }
                    else
                    {
                        x.Close();
                    }
                });
                wallpapers.RemoveAll(x => tmp.Contains(x));
                WallpaperChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CloseWallpaper(LibraryModel wp, bool terminate = false)
        {
            CloseWallpaper(wp: wp, fireEvent: true, terminate: terminate);
        }

        private void CloseWallpaper(LibraryModel wp, bool fireEvent, bool terminate)
        {
            //NOTE: To maintain compatibility with existing code ILibraryModel is still used.
            var tmp = wallpapers.FindAll(x => x.Model.LivelyInfoFolderPath == wp.LivelyInfoFolderPath);
            if (tmp.Count > 0)
            {
                tmp.ForEach(x =>
                {
                    if (x.Proc != null)
                    {
                        watchdog.Remove(x.Proc.Id);
                    }

                    if (terminate)
                    {
                        x.Terminate();
                    }
                    else
                    {
                        x.Close();
                    }
                });
                wallpapers.RemoveAll(x => tmp.Contains(x));

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void SendMessageWallpaper(string info_path, IpcMessage msg)
        {
            wallpapers.ForEach(x =>
            {
                if (x.Model.LivelyInfoFolderPath == info_path)
                {
                    x.SendMessage(msg);
                }
            });
        }

        public void SendMessageWallpaper(DisplayMonitor display, string info_path, IpcMessage msg)
        {
            wallpapers.ForEach(x =>
            {
                if (x.Screen.Equals(display) && info_path == x.Model.LivelyInfoFolderPath)
                    x.SendMessage(msg);
            });
        }

        public void SeekWallpaper(LibraryModel wp, float seek, PlaybackPosType type)
        {
            wallpapers.ForEach(x =>
            {
                if (x.Model == wp)
                {
                    x.SetPlaybackPos(seek, type);
                }
            });
        }

        public void SeekWallpaper(DisplayMonitor display, float seek, PlaybackPosType type)
        {
            wallpapers.ForEach(x =>
            {
                if (x.Screen.Equals(display))
                    x.SetPlaybackPos(seek, type);
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    WallpaperChanged -= SetupDesktop_WallpaperChanged;
                    workerWHook?.Dispose();
                    CloseAllWallpapers(false, true);
                    DesktopUtil.RefreshDesktop();

                    //not required.. (need to restart if used.)
                    //NativeMethods.SendMessage(workerw, (int)NativeMethods.WM.CLOSE, IntPtr.Zero, IntPtr.Zero);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WinDesktopCore()
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

        #region helpers

        private static IntPtr CreateWorkerW()
        {
            // Fetch the Progman window
            var progman = NativeMethods.FindWindow("Progman", null);

            IntPtr result = IntPtr.Zero;

            // Send 0x052C to Progman. This message directs Progman to spawn a 
            // WorkerW behind the desktop icons. If it is already there, nothing 
            // happens.
            NativeMethods.SendMessageTimeout(progman,
                                   0x052C,
                                   new IntPtr(0xD),
                                   new IntPtr(0x1),
                                   NativeMethods.SendMessageTimeoutFlags.SMTO_NORMAL,
                                   1000,
                                   out result);
            // Spy++ output
            // .....
            // 0x00010190 "" WorkerW
            //   ...
            //   0x000100EE "" SHELLDLL_DefView
            //     0x000100F0 "FolderView" SysListView32
            // 0x00100B8A "" WorkerW       <-- This is the WorkerW instance we are after!
            // 0x000100EC "Program Manager" Progman
            var workerw = IntPtr.Zero;

            // We enumerate all Windows, until we find one, that has the SHELLDLL_DefView 
            // as a child. 
            // If we found that window, we take its next sibling and assign it to workerw.
            NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = NativeMethods.FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            IntPtr.Zero);

                if (p != IntPtr.Zero)
                {
                    // Gets the WorkerW Window after the current one.
                    workerw = NativeMethods.FindWindowEx(IntPtr.Zero,
                                                    tophandle,
                                                    "WorkerW",
                                                    IntPtr.Zero);
                }

                return true;
            }), IntPtr.Zero);

            // Some Windows 11 builds have a different Progman window layout.
            // If the above code failed to find WorkerW, we should try this.
            // Spy++ output
            // 0x000100EC "Program Manager" Progman
            //   0x000100EE "" SHELLDLL_DefView
            //     0x000100F0 "FolderView" SysListView32
            //   0x00100B8A "" WorkerW       <-- This is the WorkerW instance we are after!
            if (workerw == IntPtr.Zero)
            {
                workerw = NativeMethods.FindWindowEx(progman,
                                                IntPtr.Zero,
                                                "WorkerW",
                                                IntPtr.Zero);
            }
            return workerw;
        }

        /// <summary>
        /// Adds the wp as child of spawned desktop-workerw window.
        /// </summary>
        /// <param name="windowHandle">handle of window</param>
        private bool TrySetParentWorkerW(IntPtr windowHandle)
        {
            //Win7
            if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
            {
                var progman = NativeMethods.FindWindow("Progman", null);
                if (!workerw.Equals(progman)) //this should fix the win7 wallpaper disappearing issue.
                    NativeMethods.ShowWindow(workerw, (uint)0);

                IntPtr ret = NativeMethods.SetParent(windowHandle, progman);
                if (ret.Equals(IntPtr.Zero))
                    return false;

                //workerw is assumed as progman in win7, this is untested with all fn's: addwallpaper(), wp pause, resize events.. 
                workerw = progman;
            }
            else
            {
                IntPtr ret = NativeMethods.SetParent(windowHandle, workerw);
                if (ret.Equals(IntPtr.Zero))
                    return false;
            }
            return true;
        }

        #endregion // helpers
    }
}
