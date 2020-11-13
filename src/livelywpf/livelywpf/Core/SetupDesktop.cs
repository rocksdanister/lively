using livelywpf.Core;
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
        private static readonly List<LivelyScreen> screenList = ScreenHelper.GetScreen();
        private static IntPtr progman, workerw;
        private static bool _isInitialized = false;
        private static Playback processMonitor;
        private static readonly List<IWallpaper> wallpapersPending = new List<IWallpaper>();
        public static event EventHandler WallpaperChanged;
        public static List<IWallpaper> Wallpapers { get; } = new List<IWallpaper>();

        static SetupDesktop()
        {
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        #endregion //init

        #region core

        public static IntPtr GetWorkerW()
        {
            return workerw;
        }

        public static void SetWallpaper(LibraryModel wp, LivelyScreen targetDisplay)
        {
            Logger.Info("Core: Setting Wallpaper=>" + wp.Title + " " + wp.FilePath);
            if (SystemParameters.HighContrast)
            {
                Logger.Error("Failed to setup workers, high contrast mode!");
                MessageBox.Show(Properties.Resources.LivelyExceptionHighContrastMode, Properties.Resources.TextError);
                return;
            }
            else if (!_isInitialized)
            {
                // Fetch the Progman window
                progman = NativeMethods.FindWindow("Progman", null);

                IntPtr result = IntPtr.Zero;

                // Send 0x052C to Progman. This message directs Progman to spawn a 
                // WorkerW behind the desktop icons. If it is already there, nothing 
                // happens.
                NativeMethods.SendMessageTimeout(progman,
                                       0x052C,
                                       new IntPtr(0),
                                       IntPtr.Zero,
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

                if (IntPtr.Equals(workerw, IntPtr.Zero) || workerw == null)
                {
                    //todo: set the settings through code using SystemParametersInfo() or something?
                    Logger.Error("Core: Failed to setup wallpaper, WorkerW handle null!");
                    System.Windows.MessageBox.Show(Properties.Resources.LivelyExceptionWorkerWSetupFail, Properties.Resources.TextError);
                    WallpaperChanged?.Invoke(null, null);
                    return;
                }
                else
                {
                    Logger.Info("Core Initialized");
                    _isInitialized = true;
                    processMonitor = new Playback();
                    processMonitor.Start();
                    WallpaperChanged += SetupDesktop_WallpaperChanged;
                    if(!Program.IsMSIX)
                    {
                        StartLivelySubProcess();
                    }
                }
            }

            if(ScreenHelper.ScreenExists(targetDisplay, DisplayIdentificationMode.screenLayout) &&
                wallpapersPending.FindIndex(x => ScreenHelper.ScreenCompare(x.GetScreen(), targetDisplay, DisplayIdentificationMode.screenLayout)) != -1)
            {
                Logger.Info("Core: Skipping, wallpaper already queued/screen not found=>" + targetDisplay.DeviceName);
                return;
            }

            if (wp.LivelyInfo.Type == WallpaperType.web 
                || wp.LivelyInfo.Type == WallpaperType.webaudio 
                || wp.LivelyInfo.Type == WallpaperType.url)
            {

                if (Program.SettingsVM.Settings.WebBrowser == LivelyWebBrowser.cef)
                {
                    wp.ItemStartup = true;
                    var item = new WebProcess(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
                else if (Program.SettingsVM.Settings.WebBrowser == LivelyWebBrowser.webview2)
                {
                    wp.ItemStartup = true;
                    var item = new WebEdge(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
            }
            else if (wp.LivelyInfo.Type == WallpaperType.app
                || wp.LivelyInfo.Type == WallpaperType.godot
                || wp.LivelyInfo.Type == WallpaperType.unity)
            {

                if (Program.IsMSIX)
                {
                    Logger.Info("Core: Skipping program wallpaper on MSIX package.");
                    return;
                }

                wp.ItemStartup = true;
                var item = new ExtPrograms(wp.FilePath, wp, targetDisplay, 
                    Program.SettingsVM.Settings.WallpaperWaitTime);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                wallpapersPending.Add(item);
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.video)
            {
                //How many videoplayers? Yes.
                if (Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.libmpv)
                {
                    wp.ItemStartup = true;
                    var item = new VideoPlayerMPV(wp.FilePath, wp, 
                        targetDisplay, Program.SettingsVM.Settings.WallpaperScaling);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
                else if (Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.libmpvExt)
                {
                    wp.ItemStartup = true;
                    var item = new VideoPlayerMPVExt(wp.FilePath, wp, targetDisplay, 
                        Program.SettingsVM.Settings.WallpaperScaling);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
                else if (Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.libvlc)
                {
                    wp.ItemStartup = true;
                    var item = new VideoPlayerVLC(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
                else if (Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.libvlcExt)
                {
                    wp.ItemStartup = true;
                    var item = new VideoPlayerVLCExt(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
                else if (Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.wmf)
                {
                    var item = new VideoPlayerWPF(wp.FilePath, wp, 
                        targetDisplay, Program.SettingsVM.Settings.WallpaperScaling);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
            }
            else if (wp.LivelyInfo.Type == WallpaperType.videostream)
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "lib", "youtube-dl.exe")))
                {
                    wp.ItemStartup = true;
                    var item = new VideoPlayerMPVExt(wp.FilePath, wp, targetDisplay,
                        Program.SettingsVM.Settings.WallpaperScaling, Program.SettingsVM.Settings.StreamQuality);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
                else
                {
                    Logger.Info("Core: yt-dl not found, using cef browser instead.");
                    //note: wallpaper type will be videostream, don't forget..
                    wp.ItemStartup = true;
                    var item = new WebProcess(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    wallpapersPending.Add(item);
                    item.Show();
                }
            }
            else if (wp.LivelyInfo.Type == WallpaperType.gif || wp.LivelyInfo.Type == WallpaperType.picture)
            {
                var item = new GIFPlayerUWP(wp.FilePath, wp, 
                    targetDisplay, Program.SettingsVM.Settings.WallpaperScaling);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                wallpapersPending.Add(item);
                item.Show();
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
                    if (wallpaper.GetProcess() != null)
                    {
                        SendMsgLivelySubProcess("lively:add-pgm " + wallpaper.GetProcess().Id);
                    }

                    //preview and create gif and thumbnail for user dropped file.
                    if (wallpaper.GetWallpaperData().DataType == LibraryTileType.processing)
                    {
                        //quitting running wallpaper before gif capture for low-end systemss.
                        if (Program.SettingsVM.Settings.LivelyGUIRendering == LivelyGUIState.lite)
                        {
                            switch (Program.SettingsVM.Settings.WallpaperArrangement)
                            {
                                case WallpaperArrangement.per:
                                    CloseWallpaper(wallpaper.GetScreen(), false);
                                    break;
                                case WallpaperArrangement.span:
                                    CloseAllWallpapers(false);
                                    break;
                                case WallpaperArrangement.duplicate:
                                    CloseAllWallpapers(false);
                                    break;
                            }
                        }

                        await ShowPreviewDialogSTAThread(wallpaper);
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

                        reloadRequired = wallpaper.GetWallpaperType() == WallpaperType.web ||
                            wallpaper.GetWallpaperType() == WallpaperType.webaudio ||
                            wallpaper.GetWallpaperType() == WallpaperType.url;
                    }
                    else if (wallpaper.GetWallpaperData().DataType == LibraryTileType.videoConvert)
                    {
                        //converting existing library item into video wallpaper using libVLC.
                        await ShowVLCScreenCaptureDialogSTAThread(wallpaper);
                        wallpaper.Close();
                        RefreshDesktop();
                        return; //exit
                    }

                    if(!ScreenHelper.IsMultiScreen())
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
                                await SetWallpaperDuplicateScreen(wallpaper);
                                break;
                        }
                    }
                    //Reload webpage, fix if the webpage code is not subscribed to window size changed event.
                    if(reloadRequired)
                    {
                        wallpaper.SendMessage("lively:reload");
                    }

                    Wallpapers.Add(wallpaper);
                    WallpaperChanged?.Invoke(null, null);
                }
                else
                {
                    if(e.Error != null)
                    {
                        Logger.Error("Core: Failed to launch wallpaper=>" + e.Msg + "\n" + e.Error.ToString());
                    }
                    else
                    {
                        Logger.Error("Core: Failed to launch wallpaper=>(No Exception thrown)" + e.Msg);
                    }
                    wallpaper.Terminate();
                    WallpaperChanged?.Invoke(null, null);
                    MessageBox.Show(Properties.Resources.LivelyExceptionGeneral, Properties.Resources.TextError);
                }
            }
            catch(Exception ex)
            {
                Logger.Error("Core: Failed processing wallpaper=>" + ex.ToString());
                if(wallpaper != null)
                {
                    wallpaper.Terminate();
                }
                WallpaperChanged?.Invoke(null, null);
            }
            finally
            {
                semaphoreSlimWallpaperInitLock.Release();
            }
        }

        #region wallpaper add

        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private static void SetWallpaperPerScreen(IntPtr handle, LivelyScreen targetDisplay, bool focus = true)
        {
            NativeMethods.RECT prct = new NativeMethods.RECT();
            NativeMethods.POINT topLeft;
            //StaticPinvoke.POINT bottomRight;
            Logger.Info("Sending wallpaper(Per Screen)=>" + targetDisplay.DeviceName + " " + targetDisplay.Bounds);

            if (!NativeMethods.SetWindowPos(handle, 1, targetDisplay.Bounds.X, targetDisplay.Bounds.Y, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0 | 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos(2) fail AddWallpaper(),");
            }

            //ScreentoClient is no longer used, this supports windows mirrored mode also, calculate new relative position of window w.r.t parent.
            NativeMethods.MapWindowPoints(handle, workerw, ref prct, 2);

            SetParentWorkerW(handle);
            //Position the wp window relative to the new parent window(workerw).
            if (!NativeMethods.SetWindowPos(handle, 1, prct.Left, prct.Top, (targetDisplay.Bounds.Width), (targetDisplay.Bounds.Height), 0 | 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos(3) fail addwallpaper(),");
            }

            if(focus)
            {
                SetFocus(true);
            }
            RefreshDesktop();

            //logging.
            NativeMethods.GetWindowRect(handle, out prct);
            Logger.Debug("Relative Coordinates of WP=>" + prct.Left + " " + prct.Right + " " + targetDisplay.Bounds.Width + " " + targetDisplay.Bounds.Height);
            topLeft.X = prct.Left;
            topLeft.Y = prct.Top;
            NativeMethods.ScreenToClient(workerw, ref topLeft);
            Logger.Debug("Coordinate wrt to screen=>" + topLeft.X + " " + topLeft.Y + " " + targetDisplay.Bounds.Width + " " + targetDisplay.Bounds.Height);
        }

        /// <summary>
        /// Spans wp across all screens.
        /// </summary>
        private static void SetWallpaperSpanScreen(IntPtr handle, bool focus = true)
        {
            NativeMethods.RECT prct = new NativeMethods.RECT();
            //get spawned workerw rectangle data.
            NativeMethods.GetWindowRect(workerw, out prct);
            SetParentWorkerW(handle);

            //fill wp into the whole workerw area.
            Logger.Info("Sending wallpaper(Span)=>" + prct.Left + " " + prct.Top + " " + (prct.Right - prct.Left) + " " + (prct.Bottom - prct.Top));
            if (!NativeMethods.SetWindowPos(handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0 | 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos fail SpanWallpaper(),");
            }

            if (focus)
            {
                SetFocus(true);
            }
            RefreshDesktop();
        }

        private static async Task SetWallpaperDuplicateScreen(IWallpaper wallpaper)
        {
            SetWallpaperPerScreen(wallpaper.GetHWND(), wallpaper.GetScreen());

            var remainingScreens = ScreenHelper.GetScreen();
            var currDuplicates = Wallpapers.FindAll(x => x.GetWallpaperData() == wallpaper.GetWallpaperData());
            remainingScreens.RemoveAll(x => ScreenHelper.ScreenCompare(wallpaper.GetScreen(), x, DisplayIdentificationMode.screenLayout) ||
                currDuplicates.FindIndex(y => ScreenHelper.ScreenCompare(y.GetScreen(), x, DisplayIdentificationMode.screenLayout)) != -1);
            if (remainingScreens.Count != 0)
            {
                Logger.Info("Sending/Queuing wallpaper(Duplicate)=>" + remainingScreens[0].DeviceName + " " + remainingScreens[0].Bounds);
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    SetWallpaper(wallpaper.GetWallpaperData(), remainingScreens[0]);
                }));
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
            List<WallpaperLayoutModel> layout = new List<WallpaperLayoutModel>();
            foreach (var item in Wallpapers)
            {
                layout.Add(new WallpaperLayoutModel(
                item.GetScreen(),
                item.GetWallpaperData().LivelyInfoFolderPath));

                if(Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.duplicate ||
                   Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span )
                {
                    break;
                }
            }

            WallpaperLayoutJSON.SaveWallpaperLayout(
                  layout,
                  Path.Combine(Program.AppDataDir, "WallpaperLayout.json"));
        }

        private static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            try
            {
                int index;
                var allScreens = ScreenHelper.GetScreen();
                if (ScreenHelper.ScreenCount() != screenList.Count)
                {
                    Logger.Info("System parameters changed: Screen Connected/Disconnected");
                    List<IWallpaper> orphans = new List<IWallpaper>();
                    Wallpapers.ForEach(x =>
                    {
                        bool found = false;
                        foreach (var item in allScreens)
                        {
                            if (ScreenHelper.ScreenCompare(x.GetScreen(), item, DisplayIdentificationMode.screenLayout))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            orphans.Add(x);
                        }
                    });

                    if (orphans.Count != 0)
                    {
                        if ((index = allScreens.FindIndex(x => ScreenHelper.ScreenCompare(
                            Program.SettingsVM.Settings.SelectedDisplay, x, DisplayIdentificationMode.screenLayout))) != -1)
                        {
                            Program.SettingsVM.Settings.SelectedDisplay = allScreens[index];
                        }
                        else
                        {
                            Program.SettingsVM.Settings.SelectedDisplay = ScreenHelper.GetPrimaryScreen();
                        }
                        Program.SettingsVM.UpdateConfigFile();

                        if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                        {
                            UpdateWallpaperRect();
                        }
                        else
                        {
                            orphans.ForEach(x =>
                            {
                                Logger.Info("System parameters changed: Disconnected Screen -> " + x.GetScreen().DeviceName + " " + x.GetScreen().Bounds);
                                x.Close();
                            });
                            Wallpapers.RemoveAll(x => orphans.Contains(x));
                        }
                        orphans.Clear();
                    }
                    WallpaperChanged?.Invoke(null, null);
                }
                else
                {
                    Logger.Info("System parameters changed: Screen Properties");
                    if ((index = allScreens.FindIndex(x => ScreenHelper.ScreenCompare(
                        Program.SettingsVM.Settings.SelectedDisplay, x, DisplayIdentificationMode.screenLayout))) != -1)
                    {
                        Program.SettingsVM.Settings.SelectedDisplay = allScreens[index];
                    }
                    else
                    {
                        Program.SettingsVM.Settings.SelectedDisplay = ScreenHelper.GetPrimaryScreen();
                    }
                    Program.SettingsVM.UpdateConfigFile();
                    UpdateWallpaperRect();
                }
                screenList.Clear();
                screenList.AddRange(allScreens);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private static void UpdateWallpaperRect()
        {
            if (ScreenHelper.IsMultiScreen() && Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
            {
                if(Wallpapers.Count != 0)
                {
                    var width = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                    var height = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
                    Logger.Info("System parameters changed: Screen Param(Span)=>" + width + " " + height);
                    //NativeMethods.SetWindowPos(Wallpapers[0].GetHWND(), 1, 0, 0, width, height, 0 | 0x0010);   
                    Wallpapers[0].SetScreen(ScreenHelper.GetPrimaryScreen());
                }
            }
            else
            {
                int i;
                foreach (var item in ScreenHelper.GetScreen())
                {
                    if ((i = Wallpapers.FindIndex(x => ScreenHelper.ScreenCompare(item, x.GetScreen(), DisplayIdentificationMode.screenClass))) != -1)
                    {
                        Logger.Info("System parameters changed: Screen Param old/new -> " + Wallpapers[i].GetScreen().Bounds + "/" + item.Bounds);
                        /*
                        NativeMethods.RECT prct = new NativeMethods.RECT();
                        NativeMethods.MapWindowPoints(Wallpapers[i].GetHWND(), workerw, ref prct, 2);
                        if (!NativeMethods.SetWindowPos(Wallpapers[i].GetHWND(), 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010))
                        {
                            NLogger.LogWin32Error("setwindowpos(3) fail UpdateWallpaperRect()=>");
                        }       
                        */
                        Wallpapers[i].SetScreen(item);                 
                    }
                }
            }
            RefreshDesktop();
        }

        private static Process livelySubProcess;
        private static void StartLivelySubProcess()
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo()
                {
                    Arguments = Process.GetCurrentProcess().Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "subproc", "livelySubProcess.exe"),
                    RedirectStandardInput = true,
                    //RedirectStandardOutput = true,
                    UseShellExecute = false,
                };

                livelySubProcess = new Process
                {
                    StartInfo = start,
                };
                livelySubProcess.Start();
            }
            catch (Exception e)
            {
                Logger.Error("subProcess start fail:" + e.Message);
            }
        }

        private static void SendMsgLivelySubProcess(string text)
        {
            if(livelySubProcess != null)
            {
                try
                {
                    livelySubProcess.StandardInput.WriteLine(text);
                }
                catch { }
            }
        }
        public static void ShutDown()
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            if (_isInitialized)
            {
                WallpaperChanged -= SetupDesktop_WallpaperChanged;
                processMonitor.Stop();
                //CloseAllWallpapersWithoutEvent();
                TerminateAllWallpapers(false);
                RefreshDesktop();
            }
        }

        #endregion //core

        #region threads

        public static Task ShowPreviewDialogSTAThread(IWallpaper wp)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        var previewWindow = new LibraryPreviewView(wp);
                        if (App.AppWindow != null)
                        {
                            previewWindow.Owner = App.AppWindow;
                            previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        }
                        previewWindow.ShowDialog();
                    }));
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                    Logger.Error(e.ToString());
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static Task ShowVLCScreenCaptureDialogSTAThread(IWallpaper wp)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        var vlcCaptureWindow = new VLCWallpaperRecordWindow(wp);
                        if (App.AppWindow != null)
                        {
                            vlcCaptureWindow.Owner = App.AppWindow;
                            vlcCaptureWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        }
                        vlcCaptureWindow.ShowDialog();
                    }));
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                    Logger.Error(e.ToString());
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        #endregion threads

        #region wallpaper operations

        private static void CloseAllWallpapers(bool fireEvent)
        {
            Wallpapers.ForEach(x => x.Close());
            Wallpapers.Clear();
            SendMsgLivelySubProcess("lively:clear");
            if (fireEvent)
            {
                WallpaperChanged?.Invoke(null, null);
            }
        }

        public static void CloseAllWallpapers()
        {
            CloseAllWallpapers(true);
        }

        private static void CloseWallpaper(LivelyScreen display, bool fireEvent)
        {
            Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                {
                    if (x.GetProcess() != null)
                    {
                        SendMsgLivelySubProcess("lively:rmv-pgm " + x.GetProcess().Id);
                    }
                    x.Close();
                }
            });
            Wallpapers.RemoveAll(x => ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout));
            if (fireEvent)
            {
                WallpaperChanged?.Invoke(null, null);
            }
        }

        public static void CloseWallpaper(LivelyScreen display)
        {
            CloseWallpaper(display, true);
        }

        public static void TerminateWallpaper(WallpaperType type)
        {
            Wallpapers.ForEach(x => 
            { 
                if (x.GetWallpaperType() == type)
                {
                    if (x.GetProcess() != null)
                    {
                        SendMsgLivelySubProcess("lively:rmv-pgm " + x.GetProcess().Id);
                    }
                    x.Terminate();
                }         
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperType() == type);
            WallpaperChanged?.Invoke(null, null);
        }

        private static void TerminateWallpaper(LivelyScreen display, bool fireEvent)
        {
            Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                {
                    if (x.GetProcess() != null)
                    {
                        SendMsgLivelySubProcess("lively:rmv-pgm " + x.GetProcess().Id);
                    }
                    x.Terminate();
                }
            });
            Wallpapers.RemoveAll(x => ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout));
            if (fireEvent)
            {
                WallpaperChanged?.Invoke(null, null);
            }
        }

        public static void TerminateWallpaper(LivelyScreen display)
        {
            TerminateWallpaper(display, true);
        }

        public static void TerminateWallpaper(LibraryModel wp)
        {
            TerminateWallpaper(wp, true);
        }

        private static void TerminateWallpaper(LibraryModel wp, bool fireEvent)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetWallpaperData() == wp)
                {
                    if (x.GetProcess() != null)
                    {
                        SendMsgLivelySubProcess("lively:rmv-pgm " + x.GetProcess().Id);
                    }
                    x.Terminate();
                }
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperData() == wp);
            if (fireEvent)
            {
                WallpaperChanged?.Invoke(null, null);
            }
        }

        private static void TerminateAllWallpapers(bool fireEvent)
        {
            Wallpapers.ForEach(x => x.Terminate());
            Wallpapers.Clear();
            SendMsgLivelySubProcess("lively:clear");
            if (fireEvent)
            {
                WallpaperChanged?.Invoke(null, null);
            }
        }

        public static void TerminateAllWallpapers()
        {
            TerminateAllWallpapers(true);
        }

        /// <summary>
        /// Note: If more than one instance of same wallpaper running, will send message to both.
        /// </summary>
        /// <param name="wp"></param>
        /// <param name="msg"></param>
        public static void SendMessageWallpaper(LibraryModel wp, string msg)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetWallpaperData() == wp)
                {
                    x.SendMessage(msg);
                }
            });
        }

        public static void SendMessageWallpaper(LivelyScreen display, string msg)
        {
            Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                    x.SendMessage(msg);
            });
        }

        #endregion //wallpaper operations

        #region helper functons

        /// <summary>
        /// Focus fix, otherwise when new applicaitons launch fullscreen wont giveup window handle once SetParent() is called.
        /// </summary>
        private static void SetFocus(bool focusLively = true)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                //change focus from the started window//application.
                NativeMethods.SetForegroundWindow(progman);
                NativeMethods.SetFocus(progman);

                if(App.AppWindow != null)
                {
                    App.AppWindow.Activate();
                }
            }));
        }

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

        private static RawInputDX InputForwardWindow = null;
        /// <summary>
        /// Forward input from desktop to wallpapers.
        /// </summary>
        /// <param name="mode">mouse, keyboard + mouse, off</param>
        public static void WallpaperInputForward(InputForwardMode mode)
        {
            if(mode == InputForwardMode.off)
            {
                if (InputForwardWindow != null)
                {
                    InputForwardWindow.Close();
                    InputForwardWindow = null;
                }
            }
            else
            {
                if (InputForwardWindow == null)
                {
                    InputForwardWindow = new RawInputDX(mode);
                    InputForwardWindow.Show();
                }
                else
                {
                    if(mode != InputForwardWindow.InputMode)
                    {
                        InputForwardWindow.Close();
                        InputForwardWindow = null;
                        WallpaperInputForward(mode);
                    }
                }
            }
        }

        #endregion //helper functions
    }
}
