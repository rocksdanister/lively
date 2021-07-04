using livelywpf.Core;
using livelywpf.Core.API;
using livelywpf.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.ApplicationModel.Wallet.System;

namespace livelywpf
{
    public static class SetupDesktop
    {
        #region init

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static IntPtr progman, workerw;
        private static bool _isInitialized = false;
        private static Playback processMonitor;
        private static readonly List<IWallpaper> wallpapersPending = new List<IWallpaper>(2);
        private static readonly List<WallpaperLayoutModel> wallpapersDisconnected = new List<WallpaperLayoutModel>();
        public static List<IWallpaper> Wallpapers { get; } = new List<IWallpaper>(2);
        public static event EventHandler WallpaperChanged;

        static SetupDesktop()
        {
            ScreenHelper.DisplayUpdated += DisplaySettingsChanged_Hwnd;
            WallpaperChanged += SetupDesktop_WallpaperChanged;
        }

        #endregion //init

        #region core

        public static void SetWallpaper(LibraryModel wallpaper, LivelyScreen display)
        {
            Logger.Info($"Setting wallpaper: {wallpaper.Title} | {wallpaper.FilePath}");
            if (!_isInitialized)
            {
                if (SystemParameters.HighContrast)
                {
                    Logger.Info("Highcontrast mode detected, some functionalities of Lively may not work properly.");
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
                    WallpaperChanged?.Invoke(null, null);
                    return;
                }
                else
                {
                    Logger.Info("Core initialized..");
                    _isInitialized = true;
                    processMonitor = new Playback();
                    processMonitor.Start();
                    WatchdogProcess.Instance.Start();
                }
            }

            //Creating copy of display.
            var target = new LivelyScreen(display);
            if (!ScreenHelper.ScreenExists(target, DisplayIdentificationMode.deviceId))
            {
                Logger.Info($"Skipping wallpaper, screen {target.DeviceName} not found.");
                return;
            }
            else if (wallpapersPending.Exists(x => ScreenHelper.ScreenCompare(x.GetScreen(), target, DisplayIdentificationMode.deviceId)))
            {
                Logger.Info("Skipping wallpaper, already queued.");
                return;
            }
            else if (!(wallpaper.LivelyInfo.IsAbsolutePath ?
                wallpaper.LivelyInfo.Type == WallpaperType.url || wallpaper.LivelyInfo.Type == WallpaperType.videostream || File.Exists(wallpaper.FilePath) :
                wallpaper.FilePath != null))
            {
                //Only checking for wallpapers outside Lively folder.
                _ = Task.Run(() => MessageBox.Show(Properties.Resources.TextFileNotFound, Properties.Resources.TextError + " " + Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information));
                Logger.Info("Skipping wallpaper, file not found.");
                WallpaperChanged?.Invoke(null, null);
                return;
            }

            IWallpaper wpInstance = null;
            switch (wallpaper.LivelyInfo.Type)
            {
                case WallpaperType.web:
                case WallpaperType.webaudio:
                case WallpaperType.url:
                    switch (Program.SettingsVM.Settings.WebBrowser)
                    {
                        case LivelyWebBrowser.cef:
                            wpInstance = new WebProcess(wallpaper.FilePath, wallpaper, target);
                            break;
                        case LivelyWebBrowser.webview2:
                            wpInstance = new WebEdge(wallpaper.FilePath, wallpaper, target);
                            break;
                    }
                    break;
                case WallpaperType.video:
                    //How many videoplayers you need? Yes.
                    switch (Program.SettingsVM.Settings.VideoPlayer)
                    {
                        case LivelyMediaPlayer.wmf:
                            wpInstance = new VideoPlayerWPF(wallpaper.FilePath, wallpaper,
                                target, Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyMediaPlayer.libvlc:
                            //depreciated
                            Logger.Info("Skipping wallpaper, libvlc depreciated player selected.");
                            break;
                        case LivelyMediaPlayer.libmpv:
                            //depreciated
                            Logger.Info("Skipping wallpaper, libmpv depreciated player selected.");
                            break;
                        case LivelyMediaPlayer.libvlcExt:
                            wpInstance = new VideoPlayerVLCExt(wallpaper.FilePath, wallpaper, target);
                            break;
                        case LivelyMediaPlayer.libmpvExt:
                            wpInstance = new VideoPlayerMPVExt(wallpaper.FilePath, wallpaper, target,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyMediaPlayer.mpv:
                            wpInstance = new VideoMpvPlayer(wallpaper.FilePath, wallpaper, target,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyMediaPlayer.vlc:
                            wpInstance = new VideoVlcPlayer(wallpaper.FilePath, wallpaper, target,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                    }
                    break;
                case WallpaperType.gif:
                case WallpaperType.picture:
                    switch (Program.SettingsVM.Settings.GifPlayer)
                    {
                        case LivelyGifPlayer.win10Img:
                            wpInstance = new GIFPlayerUWP(wallpaper.FilePath, wallpaper,
                                target, Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyGifPlayer.libmpvExt:
                            wpInstance = new VideoPlayerMPVExt(wallpaper.FilePath, wallpaper, target,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                        case LivelyGifPlayer.mpv:
                            wpInstance = new VideoMpvPlayer(wallpaper.FilePath, wallpaper, target,
                                Program.SettingsVM.Settings.WallpaperScaling);
                            break;
                    }
                    break;
                case WallpaperType.app:
                case WallpaperType.bizhawk:
                case WallpaperType.unity:
                case WallpaperType.unityaudio:
                case WallpaperType.godot:
                    if (Program.IsMSIX)
                    {
                        Logger.Info("Skipping program wallpaper on MSIX package.");
                        _= System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            if (wallpaper.DataType == LibraryTileType.processing)
                            {
                                Program.LibraryVM.WallpaperDelete(wallpaper);
                            }
                            WallpaperChanged?.Invoke(null, null);
                        }));
                        _= Task.Run(() => (MessageBox.Show(Properties.Resources.TextFeatureMissing, Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Information)));
                    }
                    else
                    {
                        wpInstance = new ExtPrograms(wallpaper.FilePath, wallpaper, target,
                            Program.SettingsVM.Settings.WallpaperWaitTime);
                    }
                    break;
                case WallpaperType.videostream:
                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "youtube-dl.exe")))
                    {
                        wpInstance = new VideoMpvPlayer(wallpaper.FilePath, wallpaper, target,
                               Program.SettingsVM.Settings.WallpaperScaling, Program.SettingsVM.Settings.StreamQuality);
                    }
                    else
                    {
                        Logger.Info("Yt-dl not found, using fallback browser instead.");
                        //note: wallpaper type will be videostream, don't forget..
                        wpInstance = new WebProcess(wallpaper.FilePath, wallpaper, target);
                    }
                    break;
            }

            if (wpInstance != null)
            {
                _= System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    wallpaper.ItemStartup = true;
                }));
                wpInstance.WindowInitialized += SetupDesktop_WallpaperInitialized;
                wallpapersPending.Add(wpInstance);
                wpInstance.Show();
            }
        }

        static readonly SemaphoreSlim semaphoreSlimWallpaperInitLock = new SemaphoreSlim(1, 1);
        private static async void SetupDesktop_WallpaperInitialized(object sender, WindowInitializedArgs e)
        {
            await semaphoreSlimWallpaperInitLock.WaitAsync();
            IWallpaper wallpaper = null;
            bool reloadRequired = false;
            try
            {
                wallpaper = (IWallpaper)sender;
                wallpapersPending.Remove(wallpaper);
                wallpaper.WindowInitialized -= SetupDesktop_WallpaperInitialized;
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    wallpaper.GetWallpaperData().ItemStartup = false;
                }));
                if (e.Success)
                {
                    switch (wallpaper.GetWallpaperData().DataType)
                    {
                        case LibraryTileType.processing:
                        case LibraryTileType.cmdImport:
                        case LibraryTileType.multiImport:
                        case LibraryTileType.edit:
                            //backup..once processed is done, becomes ready.
                            var type = wallpaper.GetWallpaperData().DataType;
                            if (Program.SettingsVM.Settings.LivelyGUIRendering == LivelyGUIState.lite &&
                                type != LibraryTileType.edit &&
                                type != LibraryTileType.multiImport)
                            {
                                //quitting running wallpaper before gif capture for low-end systemss.
                                switch (Program.SettingsVM.Settings.WallpaperArrangement)
                                {
                                    case WallpaperArrangement.per:
                                        CloseWallpaper(wallpaper.GetScreen());
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
                                        var pWindow = new LibraryPreviewView(wallpaper);
                                        if (type == LibraryTileType.multiImport)
                                        {
                                            pWindow.Topmost = true;
                                            pWindow.ShowActivated = true;
                                            if (App.AppWindow.IsVisible)
                                            {
                                                pWindow.Left = App.AppWindow.Left;
                                                pWindow.Top = App.AppWindow.Top;
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
                                            if (App.AppWindow.IsVisible)
                                            {
                                                pWindow.Owner = App.AppWindow;
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

                            if (!File.Exists(Path.Combine(wallpaper.GetWallpaperData().LivelyInfoFolderPath, "LivelyInfo.json")))
                            {
                                //user cancelled/fail!
                                wallpaper.Terminate();
                                RefreshDesktop();
                                await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                                {
                                    Program.LibraryVM.WallpaperDelete(wallpaper.GetWallpaperData());
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
                                    WallpaperChanged?.Invoke(null, null);
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
                            RefreshDesktop();
                            return;
                        default:
                            break;
                    }

                    //reload wp, fix if the webpage code is not subscribed to js window size changed event.
                    reloadRequired = wallpaper.GetWallpaperType() == WallpaperType.web ||
                        wallpaper.GetWallpaperType() == WallpaperType.webaudio ||
                        wallpaper.GetWallpaperType() == WallpaperType.url;

                    if (!ScreenHelper.IsMultiScreen())
                    {
                        TerminateAllWallpapers(false);
                        SetWallpaperPerScreen(wallpaper.GetHWND(), wallpaper.GetScreen());
                    }
                    else
                    {
                        switch (Program.SettingsVM.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                                CloseWallpaper(wallpaper.GetScreen(), false);
                                SetWallpaperPerScreen(wallpaper.GetHWND(), wallpaper.GetScreen());
                                break;
                            case WallpaperArrangement.span:
                                CloseAllWallpapers(false);
                                SetWallpaperSpanScreen(wallpaper.GetHWND());
                                break;
                            case WallpaperArrangement.duplicate:
                                CloseWallpaper(wallpaper.GetScreen(), false);
                                //Recursion..
                                SetWallpaperDuplicateScreen(wallpaper);
                                break;
                        }
                    }

                    if (reloadRequired)
                    {
                        wallpaper.SetPlaybackPos(0, PlaybackPosType.absolutePercent);
                    }

                    if (wallpaper.GetProcess() != null)
                    {
                        WatchdogProcess.Instance.Add(wallpaper.GetProcess().Id);
                    }

                    var thumbRequiredLockscreen = Program.SettingsVM.Settings.LockScreenAutoWallpaper && 
                        (!ScreenHelper.IsMultiScreen() || Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span || ScreenHelper.ScreenCompare(wallpaper.GetScreen(), ScreenHelper.GetPrimaryScreen(), DisplayIdentificationMode.deviceId));
                    var thumbRequiredAvgColor = (Program.SettingsVM.Settings.SystemTaskbarTheme == TaskbarTheme.wallpaper || Program.SettingsVM.Settings.SystemTaskbarTheme == TaskbarTheme.wallpaperFluent) &&
                        (!ScreenHelper.IsMultiScreen() || Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span || ScreenHelper.ScreenCompare(wallpaper.GetScreen(), ScreenHelper.GetPrimaryScreen(), DisplayIdentificationMode.deviceId));
                    //always capture screenshot if desktop wallpaper is checked, otherwise only capture if the wallpaper screen is primary/single-display system and user enabled lockscreen/taskbar color.
                    if (Program.SettingsVM.Settings.DesktopAutoWallpaper || thumbRequiredAvgColor || thumbRequiredLockscreen)
                    {
                        try
                        {
                            int maxIterations = 50;
                            //upto ~5sec wait for wallpaper to get ready..
                            for (int i = 1; i <= maxIterations; i++)
                            {
                                if (i == maxIterations)
                                    throw new Exception("Timed out..");

                                if (wallpaper.IsLoaded())
                                    break;

                                await Task.Delay(100);
                            }

                            //capture frame from wallpaper..
                            var imgPath = Path.Combine(Program.AppDataDir, "temp", Path.GetRandomFileName() + ".jpg");
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
                                    var color = await Task.Run(() => Helpers.TransparentTaskbar.GetAverageColor(imgPath));
                                    Helpers.TransparentTaskbar.Instance.SetAccentColor(color);
                                }
                                catch (Exception ie1)
                                {
                                    Logger.Error("Failed to set taskbar accent: " + ie1.Message);
                                }
                            }

                            //set desktop picture wallpaper..
                            if (Program.SettingsVM.Settings.DesktopAutoWallpaper)
                            {
                                if (ScreenHelper.IsMultiScreen())
                                {
                                    //Has transition animation..
                                    var desktop = (Helpers.IDesktopWallpaper)new Helpers.DesktopWallpaperClass();
                                    Helpers.DesktopWallpaperPosition scaler = Helpers.DesktopWallpaperPosition.Fill;
                                    switch (Program.SettingsVM.Settings.WallpaperScaling)
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
                                    desktop.SetPosition(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span ? Helpers.DesktopWallpaperPosition.Span : scaler);
                                    desktop.SetWallpaper(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span ? null : wallpaper.GetScreen().DeviceId, imgPath);
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

                    Wallpapers.Add(wallpaper);
                    WallpaperChanged?.Invoke(null, null);
                }
                else
                {
                    //failed to show wp window..
                    Logger.Error("Failed launching wallpaper: " + e.Msg + "\n" + e.Error?.ToString());
                    wallpaper.Terminate();
                    WallpaperChanged?.Invoke(null, null);
                    if (App.AppWindow.IsVisible)
                    {
                        MessageBox.Show(Properties.Resources.LivelyExceptionGeneral, Properties.Resources.TitleAppName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (!File.Exists(Path.Combine(wallpaper.GetWallpaperData().LivelyInfoFolderPath, "LivelyInfo.json")))
                    {
                        await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            Program.LibraryVM.WallpaperDelete(wallpaper.GetWallpaperData());
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed processing wallpaper: " + ex.ToString());
                wallpaper?.Terminate();
                WallpaperChanged?.Invoke(null, null);
            }
            finally
            {
                semaphoreSlimWallpaperInitLock.Release();
            }
        }

        /// <summary>
        /// In the event of explorer crash, re-create workerw and re-apply wallpaper.
        /// </summary>
        public static void ResetWorkerW()
        {
            Logger.Info("Restarting workerw and restoring wallpapers..");
            _isInitialized = false;
            processMonitor?.Dispose();
            if (Wallpapers.Count > 0)
            {
                var originalWallpapers = Wallpapers.ToList();
                TerminateAllWallpapers();
                foreach (var item in originalWallpapers)
                {
                    SetWallpaper(item.GetWallpaperData(), item.GetScreen());
                    if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                        break;
                }
            }
        }

        public static IntPtr GetWorkerW()
        {
            return workerw;
        }

        #region wallpaper add

        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private static void SetWallpaperPerScreen(IntPtr handle, LivelyScreen targetDisplay)
        {
            NativeMethods.RECT prct = new NativeMethods.RECT();
            Logger.Info($"Sending wallpaper(Screen): {targetDisplay.DeviceName} | {targetDisplay.Bounds}");
            //Position the wp fullscreen to corresponding display.
            if (!NativeMethods.SetWindowPos(handle, 1, targetDisplay.Bounds.X, targetDisplay.Bounds.Y, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos(2) fail AddWallpaper(),");
            }

            NativeMethods.MapWindowPoints(handle, workerw, ref prct, 2);
            SetParentWorkerW(handle);
            //Position the wp window relative to the new parent window(workerw).
            if (!NativeMethods.SetWindowPos(handle, 1, prct.Left, prct.Top, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos(3) fail addwallpaper(),");
            }

            //SetFocusMainApp();
            RefreshDesktop();
        }

        /// <summary>
        /// Spans wp across all screens.
        /// </summary>
        private static void SetWallpaperSpanScreen(IntPtr handle)
        {
            //get spawned workerw rectangle data.
            NativeMethods.GetWindowRect(workerw, out NativeMethods.RECT prct);
            SetParentWorkerW(handle);

            //fill wp into the whole workerw area.
            Logger.Info($"Sending wallpaper(Span): ({prct.Left}, {prct.Top}, {prct.Right - prct.Left}, {prct.Bottom - prct.Top}).");
            if (!NativeMethods.SetWindowPos(handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos fail SpanWallpaper(),");
            }

            //SetFocusMainApp();
            RefreshDesktop();
        }

        /// <summary>
        /// Recursively calls SetWallpaper() till the wp is applied to all screens.
        /// </summary>
        /// <param name="wallpaper">wallpaper to apply.</param>
        private static void SetWallpaperDuplicateScreen(IWallpaper wallpaper)
        {
            SetWallpaperPerScreen(wallpaper.GetHWND(), wallpaper.GetScreen());

            var remainingScreens = ScreenHelper.GetScreen().ToList();
            var currDuplicates = Wallpapers.FindAll(x => x.GetWallpaperData() == wallpaper.GetWallpaperData());
            remainingScreens.RemoveAll(x => ScreenHelper.ScreenCompare(wallpaper.GetScreen(), x, DisplayIdentificationMode.deviceId) ||
                currDuplicates.FindIndex(y => ScreenHelper.ScreenCompare(y.GetScreen(), x, DisplayIdentificationMode.deviceId)) != -1);
            if (remainingScreens.Count != 0)
            {
                Logger.Info("Sending/Queuing wallpaper(Duplicate): " + remainingScreens[0].DeviceName);
                SetWallpaper(wallpaper.GetWallpaperData(), remainingScreens[0]);
            }
            else
            {
                Logger.Info("Synchronizing wallpaper (duplicate.)");
                var mpvFix = (wallpaper.GetWallpaperType() == WallpaperType.video || wallpaper.GetWallpaperType() == WallpaperType.videostream) && 
                    Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.mpv;
                Wallpapers.ForEach(x =>
                {
                    if (mpvFix)
                    {
                        //{mpv player}
                        //todo: make a general IWallpaper interface method for track change.
                        //disable audio track of everything except the latest `wallpaper` (not added to Wallpaper list yet..)
                        Logger.Info($"Disabling audio track on screen {x.GetScreen().DeviceName} (duplicate.)");
                        x.SendMessage("{\"command\":[\"set_property\",\"aid\",\"no\"]}\n");
                    }
                    x.SetPlaybackPos(0, PlaybackPosType.absolutePercent);
                });
                if (mpvFix)
                {
                    //in theory this is not needed since its the latest - it should stay sync with the rest..
                    wallpaper.SetPlaybackPos(0, PlaybackPosType.absolutePercent);
                }
            }
        }

        #endregion //wallpaper add

        static readonly object _layoutWriteLock = new object();
        private static void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            lock (_layoutWriteLock)
            {
                SaveWallpaperLayout();
            }
        }

        private static void SaveWallpaperLayout()
        {
            var layout = new List<WallpaperLayoutModel>();
            Wallpapers.ForEach(wallpaper =>
            {
                layout.Add(new WallpaperLayoutModel(
                        wallpaper.GetScreen(),
                        wallpaper.GetWallpaperData().LivelyInfoFolderPath));
            });
            if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.per)
            {
                layout.AddRange(wallpapersDisconnected);
            }
            /*
            layout.AddRange(wallpapersDisconnected.Except(wallpapersDisconnected.FindAll(
                layout => Wallpapers.FirstOrDefault(wp => ScreenHelper.ScreenCompare(layout.LivelyScreen, wp.GetScreen(), DisplayIdentificationMode.deviceId)) != null)));
            */

            try
            {
                Helpers.JsonStorage<List<WallpaperLayoutModel>>.StoreData(Path.Combine(Program.AppDataDir, "WallpaperLayout.json"), layout);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        public static void ResetScreenData()
        {
            wallpapersDisconnected.Clear();
        }

        static readonly SemaphoreSlim semaphoreSlimDisplaySettingsChangedLock = new SemaphoreSlim(1, 1);
        private static async void DisplaySettingsChanged_Hwnd(object sender, EventArgs e)
        {
            await semaphoreSlimDisplaySettingsChangedLock.WaitAsync();
            try
            {
                Logger.Info("Display settings changed, screen(s):");
                ScreenHelper.GetScreen().ForEach(x => Logger.Info(x.DeviceName + " " + x.Bounds));
                Helpers.ScreensaverService.Instance.Stop();
                RefreshWallpaper();
                await RestoreDisconnectedWallpapers();
            }
            finally
            {
                semaphoreSlimDisplaySettingsChangedLock.Release();
            }
        }

        private static void RefreshWallpaper()
        {
            try
            {
                //Wallpapers still running on disconnected screens.
                var allScreens = ScreenHelper.GetScreen();
                var orphanWallpapers = Wallpapers.FindAll(
                    wallpaper => allScreens.Find(
                        screen => ScreenHelper.ScreenCompare(wallpaper.GetScreen(), screen, DisplayIdentificationMode.deviceId)) == null);

                //Updating user selected screen to primary if disconnected.
                Program.SettingsVM.Settings.SelectedDisplay = 
                    allScreens.Find(x => ScreenHelper.ScreenCompare(Program.SettingsVM.Settings.SelectedDisplay, x, DisplayIdentificationMode.deviceId)) ?? 
                    ScreenHelper.GetPrimaryScreen();
                Program.SettingsVM.UpdateConfigFile();

                switch (Program.SettingsVM.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        //No screens running wallpaper needs to be removed.
                        if (orphanWallpapers.Count != 0)
                        {
                            orphanWallpapers.ForEach(x =>
                            {
                                Logger.Info($"Disconnected Screen: {x.GetScreen().DeviceName} {x.GetScreen().Bounds}");
                                x.Close();
                            });

                            var newOrphans = orphanWallpapers.FindAll(
                                oldOrphan => wallpapersDisconnected.Find(
                                    newOrphan => ScreenHelper.ScreenCompare(newOrphan.LivelyScreen, oldOrphan.GetScreen(), DisplayIdentificationMode.deviceId)) == null);
                            foreach (var item in newOrphans)
                            {
                                wallpapersDisconnected.Add(new WallpaperLayoutModel(item.GetScreen(), item.GetWallpaperData().LivelyInfoFolderPath));
                            }
                            Wallpapers.RemoveAll(x => orphanWallpapers.Contains(x));
                        }
                        break;
                    case WallpaperArrangement.duplicate:
                        if (orphanWallpapers.Count != 0)
                        {
                            orphanWallpapers.ForEach(x =>
                            {
                                Logger.Info($"Disconnected Screen: {x.GetScreen().DeviceName} {x.GetScreen().Bounds}");
                                x.Close();
                            });
                            Wallpapers.RemoveAll(x => orphanWallpapers.Contains(x));
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

        private static void UpdateWallpaperRect()
        {
            try
            {
                processMonitor?.Stop();
                if (ScreenHelper.IsMultiScreen() && Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                {
                    if (Wallpapers.Count != 0)
                    {
                        Wallpapers[0].Play();
                        var screenArea = ScreenHelper.GetVirtualScreenBounds();
                        Logger.Info($"Updating wallpaper rect(Span): ({screenArea.Width}, {screenArea.Height}).");
                        //For play/pause, setting the new metadata.
                        Wallpapers[0].SetScreen(ScreenHelper.GetPrimaryScreen());
                        NativeMethods.SetWindowPos(Wallpapers[0].GetHWND(), 1, 0, 0, screenArea.Width, screenArea.Height, 0x0010);
                    }
                }
                else
                {
                    int i;
                    foreach (var screen in ScreenHelper.GetScreen().ToList())
                    {
                        if ((i = Wallpapers.FindIndex(x => ScreenHelper.ScreenCompare(screen, x.GetScreen(), DisplayIdentificationMode.deviceId))) != -1)
                        {
                            Wallpapers[i].Play();
                            Logger.Info($"Updating wallpaper rect(Screen): {Wallpapers[i].GetScreen().Bounds} -> {screen.Bounds}.");
                            //For play/pause, setting the new metadata.
                            Wallpapers[i].SetScreen(screen);

                            var screenArea = ScreenHelper.GetVirtualScreenBounds();
                            if (!NativeMethods.SetWindowPos(Wallpapers[i].GetHWND(),
                                                            1,
                                                            (screen.Bounds.X - screenArea.Location.X),
                                                            (screen.Bounds.Y - screenArea.Location.Y),
                                                            (screen.Bounds.Width),
                                                            (screen.Bounds.Height),
                                                            0x0010))
                            {
                                NLogger.LogWin32Error("setwindowpos(3) fail UpdateWallpaperRect()=>");
                            }
                        }
                    }
                }
                RefreshDesktop();
            }
            finally
            {
                processMonitor?.Start();
            }
        }

        private static async Task RestoreDisconnectedWallpapers()
        {
            try
            {
                switch (Program.SettingsVM.Settings.WallpaperArrangement)
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
                                wp => ScreenHelper.ScreenCompare(wp.GetScreen(), screen, DisplayIdentificationMode.deviceId)) == null);
                            if (newScreen != null)
                            {
                                //SetWallpaperDuplicateScreen uses recursion, so only one call is required for multiple screens.
                                SetWallpaper(Wallpapers[0].GetWallpaperData(), newScreen);
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

        public static async Task RestoreWallpaperFromLayout(string wallpaperLayoutPath)
        {
            try
            {
                var wallpaperLayout = Helpers.JsonStorage<List<WallpaperLayoutModel>>.LoadData(wallpaperLayoutPath);
                if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span ||
                    Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                {
                    if (wallpaperLayout.Count != 0)
                    {
                        await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                        {
                            var libraryItem = Program.LibraryVM.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(wallpaperLayout[0].LivelyInfoPath));
                            if (libraryItem != null)
                            {
                                SetupDesktop.SetWallpaper(libraryItem, ScreenHelper.GetPrimaryScreen());
                            }
                        }));
                    }
                }
                else if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.per)
                {
                    await RestoreWallpaper(wallpaperLayout);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to restore wallpaper: " + e.ToString());
            }
        }

        private static async Task RestoreWallpaper(List<WallpaperLayoutModel> wallpaperLayout)
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                foreach (var layout in wallpaperLayout)
                {
                    var libraryItem = Program.LibraryVM.LibraryItems.FirstOrDefault(x => x.LivelyInfoFolderPath.Equals(layout.LivelyInfoPath));
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
                            wallpapersDisconnected.Add(new WallpaperLayoutModel(layout.LivelyScreen, layout.LivelyInfoPath));
                        }
                    }
                    else
                    {
                        Logger.Info($"Restoring wallpaper {libraryItem.Title} | {libraryItem.LivelyInfoFolderPath}");
                        SetupDesktop.SetWallpaper(libraryItem, screen);
                        wallpapersDisconnected.Remove(layout);
                    }
                }
            }));
        }

        public static void ShutDown()
        {
            ScreenHelper.DisplayUpdated -= DisplaySettingsChanged_Hwnd;
            WallpaperChanged -= SetupDesktop_WallpaperChanged;
            if (_isInitialized)
            {
                try
                {
                    processMonitor?.Dispose();
                    TerminateAllWallpapers(false);
                    RefreshDesktop();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to shutdown core: " + e.ToString());
                }
            }
        }

        #endregion //core

        #region wallpaper operations

        public static void CloseAllWallpapers()
        {
            CloseAllWallpapers(true);
        }

        private static void CloseAllWallpapers(bool fireEvent)
        {
            if (Wallpapers.Count > 0)
            {
                Wallpapers.ForEach(x => x.Close());
                Wallpapers.Clear();
                WatchdogProcess.Instance.Clear();

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(null, null);
                }
            }
        }

        public static void CloseWallpaper(LivelyScreen display)
        {
            CloseWallpaper(display, true);
        }

        private static void CloseWallpaper(LivelyScreen display, bool fireEvent)
        {
            var tmp = Wallpapers.FindAll(x => ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.deviceId));
            if (tmp.Count > 0)
            {
                tmp.ForEach(x =>
                {
                    if (x.GetProcess() != null)
                    {
                        WatchdogProcess.Instance.Remove(x.GetProcess().Id);
                    }
                    x.Close();
                });
                Wallpapers.RemoveAll(x => tmp.Contains(x));

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(null, null);
                }
            }
        }

        public static void TerminateWallpaper(WallpaperType type)
        {
            var tmp = Wallpapers.FindAll(x => x.GetWallpaperType() == type);
            if (tmp.Count > 0)
            {
                tmp.ForEach(x =>
                {
                    if (x.GetProcess() != null)
                    {
                        WatchdogProcess.Instance.Remove(x.GetProcess().Id);
                    }
                    x.Terminate();
                });
                Wallpapers.RemoveAll(x => tmp.Contains(x));
                WallpaperChanged?.Invoke(null, null);
            }
        }

        public static void TerminateWallpaper(LivelyScreen display)
        {
            TerminateWallpaper(display, true);
        }

        private static void TerminateWallpaper(LivelyScreen display, bool fireEvent)
        {
            var tmp = Wallpapers.FindAll(x => ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.deviceId));
            if (tmp.Count > 0)
            {
                tmp.ForEach(x =>
                {
                    if (x.GetProcess() != null)
                    {
                        WatchdogProcess.Instance.Remove(x.GetProcess().Id);
                    }
                    x.Terminate();
                });
                Wallpapers.RemoveAll(x => tmp.Contains(x));

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(null, null);
                }
            }
        }

        public static void TerminateWallpaper(LibraryModel wp)
        {
            TerminateWallpaper(wp, true);
        }

        private static void TerminateWallpaper(LibraryModel wp, bool fireEvent)
        {
            var tmp = Wallpapers.FindAll(x => x.GetWallpaperData() == wp);
            if (tmp.Count > 0)
            {
                tmp.ForEach(x =>
                {
                    if (x.GetProcess() != null)
                    {
                        WatchdogProcess.Instance.Remove(x.GetProcess().Id);
                    }
                    x.Terminate();
                });
                Wallpapers.RemoveAll(x => tmp.Contains(x));

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(null, null);
                }
            }
        }

        public static void TerminateAllWallpapers()
        {
            TerminateAllWallpapers(true);
        }

        private static void TerminateAllWallpapers(bool fireEvent)
        {
            if (Wallpapers.Count > 0)
            {
                Wallpapers.ForEach(x => x.Terminate());
                Wallpapers.Clear();
                WatchdogProcess.Instance.Clear();

                if (fireEvent)
                {
                    WallpaperChanged?.Invoke(null, null);
                }
            }
        }

        public static void SendMessageWallpaper(IpcMessage msg)
        {
            Wallpapers.ForEach(x =>
            {
                x.SendMessage(msg);
            });
        }

        public static void SendMessageWallpaper(LivelyScreen display, LibraryModel wp, IpcMessage msg)
        {
            Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.deviceId) && wp == x.GetWallpaperData())
                    x.SendMessage(msg);
            });
        }

        public static void SeekWallpaper(LibraryModel wp, float seek, PlaybackPosType type)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetWallpaperData() == wp)
                {
                    x.SetPlaybackPos(seek, type);
                }
            });
        }

        public static void SeekWallpaper(LivelyScreen display, float seek, PlaybackPosType type)
        {
            Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.deviceId))
                    x.SetPlaybackPos(seek, type);
            });
        }

        #endregion //wallpaper operations

        #region helper functons

        /// <summary>
        /// Force redraw desktop - clears wallpaper persisting on screen even after close.
        /// </summary>
        public static void RefreshDesktop()
        {
            //todo: I'm just telling windows to change wallpaper with a null value of zero size, find proper way to do this.
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, null, NativeMethods.SPIF_UPDATEINIFILE);
        }

        /// <summary>
        /// Adds the wp as child of spawned desktop-workerw window.
        /// </summary>
        /// <param name="windowHandle">handle of window</param>
        private static void SetParentWorkerW(IntPtr windowHandle)
        {
            //Legacy, Windows 7
            if (System.Environment.OSVersion.Version.Major == 6 && System.Environment.OSVersion.Version.Minor == 1)
            {
                if (!workerw.Equals(progman)) //this should fix the win7 wp disappearing issue.
                    NativeMethods.ShowWindow(workerw, (uint)0);

                IntPtr ret = NativeMethods.SetParent(windowHandle, progman);
                if (ret.Equals(IntPtr.Zero))
                {
                    NLogger.LogWin32Error("failed to set parent(win7),");
                }
                //workerw is assumed as progman in win7, this is untested with all fn's: addwallpaper(), wp pause, resize events.. (I don't have win7 system with me).
                workerw = progman;
            }
            else
            {
                IntPtr ret = NativeMethods.SetParent(windowHandle, workerw);
                if (ret.Equals(IntPtr.Zero))
                {
                    NLogger.LogWin32Error("failed to set parent,");
                }
            }
        }

        private static RawInputDX rawInputHook;
        public static RawInputDX RawInputHook
        {
            get => rawInputHook ??= new RawInputDX();
        }

        /// <summary>
        /// Forward input from desktop to wallpapers.<para>
        /// Uses rawinput, call from UI thread.</para>
        /// </summary>
        /// <param name="mode">mouse, keyboard + mouse, off</param>
        public static void SetupInputHooks()
        {
            RawInputHook.Show();
        }

        #endregion //helper functions
    }
}
