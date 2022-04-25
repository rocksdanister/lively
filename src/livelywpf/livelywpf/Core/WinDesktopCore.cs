using livelywpf.Core.API;
using livelywpf.Core.Suspend;
using livelywpf.Core.Wallpapers;
using livelywpf.Core.Watchdog;
using livelywpf.Factories;
using livelywpf.Helpers;
using livelywpf.Helpers.Pinvoke;
using livelywpf.Helpers.Shell;
using livelywpf.Helpers.Storage;
using livelywpf.Models;
using livelywpf.Services;
using livelywpf.ViewModels;
using livelywpf.Views;
using livelywpf.Views.Dialogues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace livelywpf.Core
{
    public class WinDesktopCore : IDesktopCore
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<IWallpaper> wallpapers = new List<IWallpaper>(2);
        public ReadOnlyCollection<IWallpaper> Wallpapers => wallpapers.AsReadOnly();
        private IntPtr progman, workerw;
        public IntPtr DesktopWorkerW => workerw;
        private bool _isInitialized = false;
        private bool disposedValue;
        private readonly List<IWallpaper> wallpapersPending = new List<IWallpaper>(2);
        private readonly List<IWallpaperLayoutModel> wallpapersDisconnected = new List<IWallpaperLayoutModel>();

        public event EventHandler WallpaperChanged;
        public event EventHandler WallpaperReset;

        private readonly IUserSettingsService userSettings;
        private readonly IWallpaperFactory wallpaperFactory;
        private readonly ITransparentTbService ttbService;
        private readonly IWatchdogService watchdog;
        //private readonly IScreensaverService screenSaver;
        //private readonly LibraryViewModel libraryVm;

        public WinDesktopCore(IUserSettingsService userSettings,
            ITransparentTbService ttbService,
            IWatchdogService watchdog,
            IWallpaperFactory wallpaperFactory)
        {
            this.userSettings = userSettings;
            this.ttbService = ttbService;
            this.watchdog = watchdog;
            //this.screenSaver = screenSaver;
            //this.libraryVm = libraryVm;
            this.wallpaperFactory = wallpaperFactory;

            ScreenHelper.DisplayUpdated += DisplaySettingsChanged_Hwnd;
            WallpaperChanged += SetupDesktop_WallpaperChanged;

            SystemEvents.SessionSwitch += (s, e) => {
                if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    //Issue: https://github.com/rocksdanister/lively/issues/802
                    if (!(IntPtr.Equals(DesktopWorkerW, IntPtr.Zero) || NativeMethods.IsWindow(DesktopWorkerW)))
                    {
                        Logger.Info("WorkerW invalid after unlock, resetting..");
                        ResetWallpaper();
                    }
                }
            };
        }

        /// <summary>
        /// Sets the given wallpaper based on layout usersettings.
        /// </summary>
        public void SetWallpaper(ILibraryModel wallpaper, ILivelyScreen display)
        {
            Logger.Info($"Setting wallpaper: {wallpaper.Title} | {wallpaper.FilePath}");
            if (!_isInitialized)
            {
                if (SystemParameters.HighContrast)
                {
                    Logger.Warn("Highcontrast mode detected, some functionalities may not work properly!");
                }

                // Fetch the Progman window
                progman = NativeMethods.FindWindow("Progman", null);

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
                workerw = IntPtr.Zero;

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

                if (IntPtr.Equals(workerw, IntPtr.Zero))
                {
                    Logger.Error("Failed to setup core, WorkerW handle not found!");
                    MessageBox.Show(Properties.Resources.LivelyExceptionWorkerWSetupFail, Properties.Resources.TextError, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }
                else
                {
                    Logger.Info("Core initialized..");
                    _isInitialized = true;
                    WallpaperReset?.Invoke(this, EventArgs.Empty);
                    watchdog.Start();
                }
            }

            if (!ScreenHelper.ScreenExists(display, DisplayIdentificationMode.deviceId))
            {
                Logger.Info($"Skipping wallpaper, screen {display.DeviceName} not found.");
                return;
            }
            else if (wallpapersPending.Exists(x => display.Equals(x.Screen)))//ScreenHelper.ScreenCompare(x.Screen, display, DisplayIdentificationMode.deviceId)))
            {
                Logger.Info("Skipping wallpaper, already queued.");
                return;
            }
            else if (!(wallpaper.LivelyInfo.IsAbsolutePath ?
                wallpaper.LivelyInfo.Type == WallpaperType.url || wallpaper.LivelyInfo.Type == WallpaperType.videostream || File.Exists(wallpaper.FilePath) :
                wallpaper.FilePath != null))
            {
                //Only checking for wallpapers outside Lively folder.
                _ = Task.Run(() => 
                    MessageBox.Show(Properties.Resources.TextFileNotFound, Properties.Resources.TextError + " " + Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information));
                Logger.Info("Skipping wallpaper, file not found.");
                WallpaperChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            try
            {
                IWallpaper instance = wallpaperFactory.CreateWallpaper(wallpaper, display, userSettings);
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    wallpaper.ItemStartup = true;
                }));
                instance.WindowInitialized += WallpaperInitialized;
                wallpapersPending.Add(instance);
                instance.Show();
            }
            catch (WallpaperFactory.MsixNotAllowedException)
            {
                Logger.Info("Skipping program wallpaper (MSIX mode.)");
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    if (wallpaper.DataType == LibraryTileType.processing)
                    {
                        App.Services.GetRequiredService<LibraryViewModel>().WallpaperDelete(wallpaper);
                    }
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }));
                _ = Task.Run(() => MessageBox.Show(Properties.Resources.TextFeatureMissing, Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }


        private readonly SemaphoreSlim semaphoreSlimWallpaperInitLock = new SemaphoreSlim(1, 1);
        private async void WallpaperInitialized(object sender, WindowInitializedArgs e)
        {
            await semaphoreSlimWallpaperInitLock.WaitAsync();
            IWallpaper wallpaper = null;
            bool reloadRequired = false;
            try
            {
                wallpaper = (IWallpaper)sender;
                wallpapersPending.Remove(wallpaper);
                wallpaper.WindowInitialized -= WallpaperInitialized;
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    wallpaper.Model.ItemStartup = false;
                }));
                if (e.Success)
                {
                    switch (wallpaper.Model.DataType)
                    {
                        case LibraryTileType.processing:
                        case LibraryTileType.cmdImport:
                        case LibraryTileType.multiImport:
                        case LibraryTileType.edit:
                            //backup..once processed is done, becomes ready.
                            var type = wallpaper.Model.DataType;
                            if (userSettings.Settings.LivelyGUIRendering == LivelyGUIState.lite &&
                                type != LibraryTileType.edit &&
                                type != LibraryTileType.multiImport)
                            {
                                //quitting running wallpaper before gif capture for low-end systemss.
                                switch (userSettings.Settings.WallpaperArrangement)
                                {
                                    case WallpaperArrangement.per:
                                        CloseWallpaper(wallpaper.Screen);
                                        break;
                                    case WallpaperArrangement.span:
                                        CloseAllWallpapers();
                                        break;
                                    case WallpaperArrangement.duplicate:
                                        CloseAllWallpapers();
                                        break;
                                }
                            }

                            var tcs = new TaskCompletionSource<object>();
                            var thread = new Thread(() =>
                            {
                                try
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
                                    {
                                        var appWindow = App.Services.GetRequiredService<MainWindow>();
                                        var pWindow = new LibraryPreviewView(wallpaper);
                                        if (type == LibraryTileType.multiImport)
                                        {
                                            pWindow.Topmost = true;
                                            pWindow.ShowActivated = true;
                                            if (appWindow.IsVisible)
                                            {
                                                pWindow.Left = appWindow.Left;
                                                pWindow.Top = appWindow.Top;
                                                pWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                            }
                                            pWindow.Closed += (s, a) => tcs.SetResult(null);
                                            pWindow.Show();
                                        }
                                        else if (type == LibraryTileType.cmdImport)
                                        {
                                            pWindow.Topmost = true;
                                            pWindow.ShowActivated = true;
                                            pWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                            pWindow.Closed += (s, a) => tcs.SetResult(null);
                                            pWindow.Show();
                                        }
                                        else
                                        {
                                            if (appWindow.IsVisible)
                                            {
                                                pWindow.Owner = appWindow;
                                                pWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                            }
                                            pWindow.ShowDialog();
                                            tcs.SetResult(null);
                                        }
                                    }));
                                }
                                catch (Exception e)
                                {
                                    tcs.SetException(e);
                                    Logger.Error(e.ToString());
                                }
                            });
                            thread.SetApartmentState(ApartmentState.STA);
                            thread.Start();
                            await tcs.Task;

                            if (!File.Exists(Path.Combine(wallpaper.Model.LivelyInfoFolderPath, "LivelyInfo.json")))
                            {
                                //user cancelled/fail!
                                wallpaper.Terminate();
                                DesktopUtil.RefreshDesktop();
                                await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                                {
                                    App.Services.GetRequiredService<LibraryViewModel>().WallpaperDelete(wallpaper.Model);
                                }));
                                return; //exit
                            }
                            else
                            {
                                if (type == LibraryTileType.edit)
                                {
                                    wallpaper.Terminate();
                                    return;
                                }
                                else if (type == LibraryTileType.multiImport)
                                {
                                    wallpaper.Terminate();
                                    //todo: do it better..
                                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                                    return;
                                }
                            }
                            break;
                        case LibraryTileType.installing:
                            break;
                        case LibraryTileType.downloading:
                            break;
                        case LibraryTileType.ready:
                            break;
                        case LibraryTileType.videoConvert:
                            //depreciated, currently unused.
                            wallpaper.Terminate();
                            DesktopUtil.RefreshDesktop();
                            return;
                        default:
                            break;
                    }

                    //reload wp, fix if the webpage code is not subscribed to js window size changed event.
                    reloadRequired = wallpaper.Category == WallpaperType.web ||
                        wallpaper.Category == WallpaperType.webaudio ||
                        wallpaper.Category == WallpaperType.url;

                    if (!ScreenHelper.IsMultiScreen())
                    {
                        CloseAllWallpapers(false, true);
                        SetWallpaperPerScreen(wallpaper.Handle, wallpaper.Screen);
                    }
                    else
                    {
                        switch (userSettings.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                                CloseWallpaper(wallpaper.Screen, false);
                                SetWallpaperPerScreen(wallpaper.Handle, wallpaper.Screen);
                                break;
                            case WallpaperArrangement.span:
                                CloseAllWallpapers(false, false);
                                SetWallpaperSpanScreen(wallpaper.Handle);
                                break;
                            case WallpaperArrangement.duplicate:
                                CloseWallpaper(wallpaper.Screen, false, false);
                                //Recursion..
                                SetWallpaperDuplicateScreen(wallpaper);
                                break;
                        }
                    }

                    if (reloadRequired)
                    {
                        wallpaper.SetPlaybackPos(0, PlaybackPosType.absolutePercent);
                    }

                    if (wallpaper.Proc != null)
                    {
                        watchdog.Add(wallpaper.Proc.Id);
                    }

                    var thumbRequiredLockscreen = userSettings.Settings.LockScreenAutoWallpaper &&
                        (!ScreenHelper.IsMultiScreen() || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span || ScreenHelper.ScreenCompare(wallpaper.Screen, ScreenHelper.GetPrimaryScreen(), DisplayIdentificationMode.deviceId));
                    var thumbRequiredAvgColor = (userSettings.Settings.SystemTaskbarTheme == TaskbarTheme.wallpaper || userSettings.Settings.SystemTaskbarTheme == TaskbarTheme.wallpaperFluent) &&
                        (!ScreenHelper.IsMultiScreen() || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span || ScreenHelper.ScreenCompare(wallpaper.Screen, ScreenHelper.GetPrimaryScreen(), DisplayIdentificationMode.deviceId));
                    //always capture screenshot if desktop wallpaper is checked, otherwise only capture if the wallpaper screen is primary/single-display system and user enabled lockscreen/taskbar color.
                    if (userSettings.Settings.DesktopAutoWallpaper || thumbRequiredAvgColor || thumbRequiredLockscreen)
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
                                    Logger.Error("Failed to set taskbar accent: " + ie1.Message);
                                }
                            }

                            //set desktop picture wallpaper..
                            if (userSettings.Settings.DesktopAutoWallpaper)
                            {
                                if (ScreenHelper.IsMultiScreen())
                                {
                                    //Has transition animation..
                                    var desktop = (Helpers.IDesktopWallpaper)new Helpers.DesktopWallpaperClass();
                                    Helpers.DesktopWallpaperPosition scaler = Helpers.DesktopWallpaperPosition.Fill;
                                    switch (userSettings.Settings.WallpaperScaling)
                                    {
                                        case WallpaperScaler.none:
                                            scaler = Helpers.DesktopWallpaperPosition.Center;
                                            break;
                                        case WallpaperScaler.fill:
                                            scaler = Helpers.DesktopWallpaperPosition.Stretch;
                                            break;
                                        case WallpaperScaler.uniform:
                                            scaler = Helpers.DesktopWallpaperPosition.Fit;
                                            break;
                                        case WallpaperScaler.uniformFill:
                                            //not exaclty the same, lively's uniform fill pivot is topleft whereas for windows its center.
                                            scaler = Helpers.DesktopWallpaperPosition.Fill;
                                            break;
                                    }
                                    desktop.SetPosition(userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span ? Helpers.DesktopWallpaperPosition.Span : scaler);
                                    desktop.SetWallpaper(userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span ? null : wallpaper.Screen.DeviceId, imgPath);
                                }
                                else
                                {
                                    //No transition animation..
                                    NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, imgPath, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDWININICHANGE);
                                }
                            }

                            //set lockscreen picture wallpaper..
                            if (thumbRequiredLockscreen)
                            {
                                await Helpers.WindowsPersonalize.SetLockScreenWallpaper(imgPath);
                            }
                        }
                        catch (Exception ie2)
                        {
                            Logger.Error("Failed to set lockscreen/desktop wallpaper: " + ie2.Message);
                        }
                    }

                    wallpapers.Add(wallpaper);
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    //failed to show wp window..
                    Logger.Error("Failed launching wallpaper: " + e.Msg + "\n" + e.Error?.ToString());
                    wallpaper.Terminate();
                    WallpaperChanged?.Invoke(this, EventArgs.Empty);
                    var appWindow = App.Services.GetRequiredService<MainWindow>();
                    if (appWindow.IsVisible)
                    {
                        MessageBox.Show(Properties.Resources.LivelyExceptionGeneral, Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (!File.Exists(Path.Combine(wallpaper.Model.LivelyInfoFolderPath, "LivelyInfo.json")))
                    {
                        await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            App.Services.GetRequiredService<LibraryViewModel>().WallpaperDelete(wallpaper.Model);
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed processing wallpaper: " + ex.ToString());
                wallpaper?.Terminate();
                WallpaperChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                semaphoreSlimWallpaperInitLock.Release();
            }
        }

        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private void SetWallpaperPerScreen(IntPtr handle, ILivelyScreen targetDisplay)
        {
            NativeMethods.RECT prct = new NativeMethods.RECT();
            Logger.Info($"Sending wallpaper(Screen): {targetDisplay.DeviceName} | {targetDisplay.Bounds}");
            //Position the wp fullscreen to corresponding display.
            if (!NativeMethods.SetWindowPos(handle, 1, targetDisplay.Bounds.X, targetDisplay.Bounds.Y, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0x0010))
            {
                LogUtil.LogWin32Error("Failed to set perscreen wallpaper(1)");
            }

            NativeMethods.MapWindowPoints(handle, workerw, ref prct, 2);
            SetParentWorkerW(handle);
            //Position the wp window relative to the new parent window(workerw).
            if (!NativeMethods.SetWindowPos(handle, 1, prct.Left, prct.Top, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0x0010))
            {
                LogUtil.LogWin32Error("Failed to set perscreen wallpaper(2)");
            }

            //SetFocusMainApp();
            DesktopUtil.RefreshDesktop();
        }

        /// <summary>
        /// Spans wp across all screens.
        /// </summary>
        private void SetWallpaperSpanScreen(IntPtr handle)
        {
            //get spawned workerw rectangle data.
            NativeMethods.GetWindowRect(workerw, out NativeMethods.RECT prct);
            SetParentWorkerW(handle);

            //fill wp into the whole workerw area.
            Logger.Info($"Sending wallpaper(Span): ({prct.Left}, {prct.Top}, {prct.Right - prct.Left}, {prct.Bottom - prct.Top}).");
            if (!NativeMethods.SetWindowPos(handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0x0010))
            {
                LogUtil.LogWin32Error("Failed to set span wallpaper");
            }

            //SetFocusMainApp();
            DesktopUtil.RefreshDesktop();
        }

        /// <summary>
        /// Recursively calls SetWallpaper() till the wp is applied to all screens.
        /// </summary>
        /// <param name="wallpaper">wallpaper to apply.</param>
        private void SetWallpaperDuplicateScreen(IWallpaper wallpaper)
        {
            SetWallpaperPerScreen(wallpaper.Handle, wallpaper.Screen);

            var remainingScreens = ScreenHelper.GetScreen().ToList();
            var currDuplicates = wallpapers.FindAll(x => x.Model == wallpaper.Model);
            remainingScreens.RemoveAll(x => ScreenHelper.ScreenCompare(wallpaper.Screen, x, DisplayIdentificationMode.deviceId) ||
                currDuplicates.FindIndex(y => ScreenHelper.ScreenCompare(y.Screen, x, DisplayIdentificationMode.deviceId)) != -1);
            if (remainingScreens.Count != 0)
            {
                Logger.Info("Sending/Queuing wallpaper(Duplicate): " + remainingScreens[0].DeviceName);
                SetWallpaper(wallpaper.Model, remainingScreens[0]);
            }
            else
            {
                Logger.Info("Synchronizing wallpaper (duplicate.)");
                var videoSync = wallpaper.Category == WallpaperType.video || wallpaper.Category == WallpaperType.videostream;
                wallpapers.ForEach(x =>
                {
                    if (videoSync)
                    {
                        //disable audio track of everything except the latest `wallpaper` (not added to Wallpaper list yet..)
                        Logger.Info($"Disabling audio track on screen {x.Screen.DeviceName} (duplicate.)");
                        x.SetMute(true);
                    }
                    x.SetPlaybackPos(0, PlaybackPosType.absolutePercent);
                });
                if (videoSync)
                {
                    //in theory this is not needed since its the latest - it should stay sync with the rest..
                    wallpaper.SetPlaybackPos(0, PlaybackPosType.absolutePercent);
                }
            }
        }

        /// <summary>
        /// Reset workerw.
        /// </summary>
        public void ResetWallpaper()
        {
            Logger.Info("Restarting wallpaper service..");
            _isInitialized = false;
            if (Wallpapers.Count > 0)
            {
                var originalWallpapers = Wallpapers.ToList();
                CloseAllWallpapers(true);
                foreach (var item in originalWallpapers)
                {
                    SetWallpaper(item.Model, item.Screen);
                    if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                        break;
                }
            }
            //WallpaperReset?.Invoke(this, EventArgs.Empty);
        }

        private void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            SaveWallpaperLayout();
        }

        static readonly object _layoutWriteLock = new object();
        private void SaveWallpaperLayout()
        {
            lock (_layoutWriteLock)
            {
                userSettings.WallpaperLayout.Clear();
                wallpapers.ForEach(wallpaper =>
                {
                    userSettings.WallpaperLayout.Add(new WallpaperLayoutModel(
                            (LivelyScreen)wallpaper.Screen,
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
                    userSettings.Save<List<IWallpaperLayoutModel>>();
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
        }

        static readonly SemaphoreSlim semaphoreSlimDisplaySettingsChangedLock = new SemaphoreSlim(1, 1);
        private async void DisplaySettingsChanged_Hwnd(object sender, EventArgs e)
        {
            await semaphoreSlimDisplaySettingsChangedLock.WaitAsync();
            try
            {
                Logger.Info("Display settings changed, screen(s):");
                ScreenHelper.GetScreen().ForEach(x => Logger.Info(x.DeviceName + " " + x.Bounds));
                App.Services.GetRequiredService<IScreensaverService>().Stop();
                RefreshWallpaper();
                await RestoreDisconnectedWallpapers();
            }
            finally
            {
                semaphoreSlimDisplaySettingsChangedLock.Release();
            }
        }

        private void RefreshWallpaper()
        {
            try
            {
                //Wallpapers still running on disconnected screens.
                var allScreens = ScreenHelper.GetScreen();
                var orphanWallpapers = wallpapers.FindAll(
                    wallpaper => allScreens.Find(
                        screen => ScreenHelper.ScreenCompare(wallpaper.Screen, screen, DisplayIdentificationMode.deviceId)) == null);

                //Updating user selected screen to primary if disconnected.
                userSettings.Settings.SelectedDisplay =
                    allScreens.Find(x => ScreenHelper.ScreenCompare(userSettings.Settings.SelectedDisplay, x, DisplayIdentificationMode.deviceId)) ??
                    ScreenHelper.GetPrimaryScreen();
                userSettings.Save<ISettingsModel>();

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
                                    newOrphan => ScreenHelper.ScreenCompare(newOrphan.LivelyScreen, oldOrphan.Screen, DisplayIdentificationMode.deviceId)) == null);
                            foreach (var item in newOrphans)
                            {
                                wallpapersDisconnected.Add(new WallpaperLayoutModel((LivelyScreen)item.Screen, item.Model.LivelyInfoFolderPath));
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
                WallpaperChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private void UpdateWallpaperRect()
        {
            if (ScreenHelper.IsMultiScreen() && userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span)
            {
                if (wallpapers.Count != 0)
                {
                    //Wallpapers[0].Play();
                    var screenArea = ScreenHelper.GetVirtualScreenBounds();
                    Logger.Info($"Updating wallpaper rect(Span): ({screenArea.Width}, {screenArea.Height}).");
                    //For play/pause, setting the new metadata.
                    Wallpapers[0].Screen = ScreenHelper.GetPrimaryScreen();
                    NativeMethods.SetWindowPos(Wallpapers[0].Handle, 1, 0, 0, screenArea.Width, screenArea.Height, 0x0010);
                }
            }
            else
            {
                int i;
                foreach (var screen in ScreenHelper.GetScreen().ToList())
                {
                    if ((i = wallpapers.FindIndex(x => ScreenHelper.ScreenCompare(screen, x.Screen, DisplayIdentificationMode.deviceId))) != -1)
                    {
                        //Wallpapers[i].Play();
                        Logger.Info($"Updating wallpaper rect(Screen): {Wallpapers[i].Screen.Bounds} -> {screen.Bounds}.");
                        //For play/pause, setting the new metadata.
                        Wallpapers[i].Screen = screen;

                        var screenArea = ScreenHelper.GetVirtualScreenBounds();
                        if (!NativeMethods.SetWindowPos(Wallpapers[i].Handle,
                                                        1,
                                                        (screen.Bounds.X - screenArea.Location.X),
                                                        (screen.Bounds.Y - screenArea.Location.Y),
                                                        (screen.Bounds.Width),
                                                        (screen.Bounds.Height),
                                                        0x0010))
                        {
                            LogUtil.LogWin32Error("Failed to update wallpaper rect");
                        }
                    }
                }
            }
            DesktopUtil.RefreshDesktop();
        }

        private async Task RestoreDisconnectedWallpapers()
        {
            try
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        //Finding screens for previously removed wallpaper if screen reconnected..
                        var wallpapersToRestore = wallpapersDisconnected.FindAll(wallpaper => ScreenHelper.GetScreen().FirstOrDefault(
                            screen => ScreenHelper.ScreenCompare(wallpaper.LivelyScreen, screen, DisplayIdentificationMode.deviceId)) != null);
                        await RestoreWallpaper(wallpapersToRestore);
                        break;
                    case WallpaperArrangement.span:
                        //UpdateWallpaperRect() should handle it normally.
                        //todo: if all screens disconnect?
                        break;
                    case WallpaperArrangement.duplicate:
                        if ((ScreenHelper.ScreenCount() > Wallpapers.Count) && Wallpapers.Count != 0)
                        {
                            var newScreen = ScreenHelper.GetScreen().FirstOrDefault(screen => Wallpapers.FirstOrDefault(
                                wp => ScreenHelper.ScreenCompare(wp.Screen, screen, DisplayIdentificationMode.deviceId)) == null);
                            if (newScreen != null)
                            {
                                //SetWallpaperDuplicateScreen uses recursion, so only one call is required for multiple screens.
                                SetWallpaper(Wallpapers[0].Model, newScreen);
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

        private async Task RestoreWallpaper(List<IWallpaperLayoutModel> wallpaperLayout)
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                foreach (var layout in wallpaperLayout)
                {
                    var library = App.Services.GetRequiredService<LibraryViewModel>();
                    var libraryItem = library.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(layout.LivelyInfoPath));
                    var screen = ScreenHelper.GetScreen(layout.LivelyScreen.DeviceId, layout.LivelyScreen.DeviceName,
                        layout.LivelyScreen.Bounds, layout.LivelyScreen.WorkingArea, DisplayIdentificationMode.deviceId);
                    if (libraryItem == null)
                    {
                        Logger.Info($"Wallpaper missing in library, skipping restoration of {layout.LivelyInfoPath}");
                        wallpapersDisconnected.Remove(layout);
                    }
                    else if (screen == null)
                    {
                        Logger.Info($"Screen missing, skipping restoration of {layout.LivelyInfoPath} | {layout.LivelyScreen.DeviceName}");
                        if (!wallpapersDisconnected.Contains(layout))
                        {
                            Logger.Info($"Wallpaper queued to disconnected screenlist {layout.LivelyInfoPath} | {layout.LivelyScreen.DeviceName}");
                            wallpapersDisconnected.Add(new WallpaperLayoutModel((LivelyScreen)layout.LivelyScreen, layout.LivelyInfoPath));
                        }
                    }
                    else
                    {
                        Logger.Info($"Restoring wallpaper {libraryItem.Title} | {libraryItem.LivelyInfoFolderPath}");
                        SetWallpaper(libraryItem, screen);
                        wallpapersDisconnected.Remove(layout);
                    }
                }
            }));
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
                        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                        {
                            var library = App.Services.GetRequiredService<LibraryViewModel>();
                            var libraryItem = library.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(wallpaperLayout[0].LivelyInfoPath));
                            if (libraryItem != null)
                            {
                                SetWallpaper(libraryItem, ScreenHelper.GetPrimaryScreen());
                            }
                        }));
                    }
                }
                else if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.per)
                {
                    _ = RestoreWallpaper(wallpaperLayout);
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

        public void CloseWallpaper(ILivelyScreen display, bool terminate = false)
        {
            CloseWallpaper(display: display, fireEvent: true, terminate: terminate);
        }

        private void CloseWallpaper(ILivelyScreen display, bool fireEvent, bool terminate)
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

        public void CloseWallpaper(ILibraryModel wp, bool terminate = false)
        {
            CloseWallpaper(wp: wp, fireEvent: true, terminate: terminate);
        }

        private void CloseWallpaper(ILibraryModel wp, bool fireEvent, bool terminate)
        {
            var tmp = wallpapers.FindAll(x => x.Model == wp);
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

        public void SendMessageWallpaper(ILibraryModel wp, IpcMessage msg)
        {
            wallpapers.ForEach(x =>
            {
                if (x.Model == wp)
                {
                    x.SendMessage(msg);
                }
            });
        }

        public void SendMessageWallpaper(ILivelyScreen display, ILibraryModel wp, IpcMessage msg)
        {
            wallpapers.ForEach(x =>
            {
                if (x.Screen.Equals(display) && wp == x.Model)
                    x.SendMessage(msg);
            });
        }

        public void SeekWallpaper(ILibraryModel wp, float seek, PlaybackPosType type)
        {
            wallpapers.ForEach(x =>
            {
                if (x.Model == wp)
                {
                    x.SetPlaybackPos(seek, type);
                }
            });
        }

        public void SeekWallpaper(ILivelyScreen display, float seek, PlaybackPosType type)
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
                    ScreenHelper.DisplayUpdated -= DisplaySettingsChanged_Hwnd;
                    WallpaperChanged -= SetupDesktop_WallpaperChanged;
                    if (_isInitialized)
                    {
                        try
                        {
                            CloseAllWallpapers(false, true);
                            DesktopUtil.RefreshDesktop();

                            //not required.. (need to restart if used.)
                            //NativeMethods.SendMessage(workerw, (int)NativeMethods.WM.CLOSE, IntPtr.Zero, IntPtr.Zero);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Failed to shutdown core: " + e.ToString());
                        }
                    }
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

        /// <summary>
        /// Adds the wp as child of spawned desktop-workerw window.
        /// </summary>
        /// <param name="windowHandle">handle of window</param>
        private void SetParentWorkerW(IntPtr windowHandle)
        {
            //Legacy, Windows 7
            if (System.Environment.OSVersion.Version.Major == 6 && System.Environment.OSVersion.Version.Minor == 1)
            {
                if (!workerw.Equals(progman)) //this should fix the win7 wallpaper disappearing issue.
                    NativeMethods.ShowWindow(workerw, (uint)0);

                IntPtr ret = NativeMethods.SetParent(windowHandle, progman);
                if (ret.Equals(IntPtr.Zero))
                {
                    LogUtil.LogWin32Error("Failed to set window parent");
                    throw new Exception("Failed to set window parent.");
                }
                //workerw is assumed as progman in win7, this is untested with all fn's: addwallpaper(), wp pause, resize events.. 
                workerw = progman;
            }
            else
            {
                IntPtr ret = NativeMethods.SetParent(windowHandle, workerw);
                if (ret.Equals(IntPtr.Zero))
                {
                    LogUtil.LogWin32Error("Failed to set window parent");
                    throw new Exception("Failed to set window parent.");
                }
            }
        }

        #endregion // helpers
    }
}
