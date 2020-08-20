using livelywpf.Core;
using livelywpf.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf
{
    public static class SetupDesktop
    {
        #region init

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static List<LivelyScreen> screenList = ScreenHelper.GetScreen();
        static IntPtr progman, workerw;
        private static bool _isInitialized = false;
        private static Playback processMonitor;
        public static List<IWallpaper> Wallpapers = new List<IWallpaper>();
        public static event EventHandler WallpaperChanged;

        #endregion //init

        #region core

        public static void SetWallpaper(LibraryModel wp, LivelyScreen targetDisplay)
        {
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
                    Logger.Error("Failed to setup wallpaper, WorkerW handle null!");
                    System.Windows.MessageBox.Show(Properties.Resources.LivelyExceptionWorkerWSetupFail, Properties.Resources.TextError);
                    return;
                }
                else
                {
                    _isInitialized = true;
                    processMonitor = new Playback();
                    processMonitor.Start();
                    SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
                    WallpaperChanged += SetupDesktop_WallpaperChanged;
                    Logger.Info("Core Initialized");
                }
            }

            if(wp.LivelyInfo.Type == WallpaperType.web 
            || wp.LivelyInfo.Type == WallpaperType.webaudio 
            || wp.LivelyInfo.Type == WallpaperType.url)
            {
                wp.ItemStartup = true;
                var item = new WebProcess(wp.FilePath, wp, targetDisplay);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            if (wp.LivelyInfo.Type == WallpaperType.app
            || wp.LivelyInfo.Type == WallpaperType.godot
            || wp.LivelyInfo.Type == WallpaperType.unity)
            {
                System.Windows.MessageBox.Show("not supported currently");
                return;

                wp.ItemStartup = true;
                var item = new ExtPrograms(wp.FilePath, wp, targetDisplay);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if(wp.LivelyInfo.Type == WallpaperType.video)
            {
                if(Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.libmpv)
                {
                    var item = new VideoPlayerMPV(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    item.Show();
                }
                else if(Program.SettingsVM.Settings.VideoPlayer == LivelyMediaPlayer.libvlc)
                {
                    var item = new VideoPlayerVLC(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    item.Show();
                }
                else
                {
                    var item = new VideoPlayerWPF(wp.FilePath, wp, targetDisplay);
                    item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                    item.Show();
                }
            }
            else if(wp.LivelyInfo.Type == WallpaperType.videostream)
            {
                var item = new VideoPlayerVLC(wp.FilePath, wp, targetDisplay);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if(wp.LivelyInfo.Type == WallpaperType.gif)
            {
                var item = new GIFPlayerUWP(wp.FilePath, wp, targetDisplay);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
        }

        private static async void SetupDesktop_WallpaperInitialized(object sender, WindowInitializedArgs e)
        {
            var wallpaper = (IWallpaper)sender;
            wallpaper.WindowInitialized -= SetupDesktop_WallpaperInitialized;
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                wallpaper.GetWallpaperData().ItemStartup = false;
            }));
            if (e.Success)
            {
                //preview and create gif and thumbnail for user dropped file.
                if (wallpaper.GetWallpaperData().DataType == LibraryTileType.processing)
                {
                    //quitting running wallpaper before gif capture for low-end systemss.
                    if (Program.SettingsVM.Settings.LivelyGUIRendering == LivelyGUIState.lite)
                    {
                        switch (Program.SettingsVM.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                                CloseWallpaperWithoutEvent(wallpaper.GetScreen());
                                break;
                            case WallpaperArrangement.span:
                                CloseAllWallpapersWithoutEvent();
                                break;
                            case WallpaperArrangement.duplicate:
                                CloseAllWallpapersWithoutEvent();
                                break;
                        }
                    }

                    await ShowPreviewDialogSTAThread(wallpaper);
                    if (!File.Exists(
                    Path.Combine(wallpaper.GetWallpaperData().LivelyInfoFolderPath, "LivelyInfo.json")))
                    {
                        //user cancelled/fail!
                        wallpaper.Close();
                        RefreshDesktop();
                        await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            Program.LibraryVM.WallpaperDelete(wallpaper.GetWallpaperData());
                        }));
                        return; //exit
                    }
                }
                else if (wallpaper.GetWallpaperData().DataType == LibraryTileType.videoConvert)
                {
                    //converting existing library item into video wallpaper using libVLC.
                    await ShowVLCScreenCaptureDialogSTAThread(wallpaper);
                    wallpaper.Close();
                    RefreshDesktop();
                    return; //exit
                }

                switch(Program.SettingsVM.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        CloseWallpaperWithoutEvent(wallpaper.GetScreen());
                        SetWallpaperPerScreen(wallpaper.GetHWND(), wallpaper.GetScreen());
                        break;
                    case WallpaperArrangement.span:
                        CloseAllWallpapersWithoutEvent();
                        SetWallpaperSpanScreen(wallpaper.GetHWND());
                        break;
                    case WallpaperArrangement.duplicate:
                        CloseAllWallpapersWithoutEvent();
                        SetWallpaperDuplicateScreen(wallpaper.GetHWND());
                        break;
                } 
                Wallpapers.Add(wallpaper);
                WallpaperChanged?.Invoke(null, null);
                //SaveWallpaperLayout();
            }
            else
            {
                Logger.Error(e.Error.ToString());
                wallpaper.Close();
            }
        }

        private static void SetupDesktop_WallpaperChanged(object sender, EventArgs e)
        {
            SaveWallpaperLayout();
        }

        private static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            try
            {
                if (ScreenHelper.ScreenCount() != screenList.Count)
                {
                    Logger.Info("System parameters changed: Screen Connected/Disconnected");
                    List<IWallpaper> orphans = new List<IWallpaper>();
                    Wallpapers.ForEach(x =>
                    {
                        bool found = false;
                        foreach (var item in ScreenHelper.GetScreen())
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
                        if (orphans.FindIndex(x => ScreenHelper.ScreenCompare(Program.SettingsVM.Settings.SelectedDisplay, x.GetScreen(), DisplayIdentificationMode.screenLayout)) != -1)
                        {
                            Program.SettingsVM.Settings.SelectedDisplay = ScreenHelper.GetPrimaryScreen();
                            Program.SettingsVM.UpdateConfigFile();
                        }

                        orphans.ForEach(x =>
                        {
                            Logger.Info("System parameters changed: Disconnected Screen -> " + x.GetScreen().DeviceName + " " + x.GetScreen().Bounds);
                            x.Close();
                        });
                        Wallpapers.RemoveAll(x => orphans.Contains(x));
                        orphans.Clear();
                    }
                    WallpaperChanged?.Invoke(null, null);
                }
                else
                {
                    Logger.Info("System parameters changed: Screen Properties");
                    UpdateWallpaperRect();
                }
                screenList.Clear();
                screenList.AddRange(ScreenHelper.GetScreen());
            }
            catch(Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private static void UpdateWallpaperRect()
        {
            //Known issue: If resolution is changed along with dpi is also changed, then this method will produce unreliable results.
            //Possible cause: the window being used as reference is not updated to the new dpi in time?
            int i;
            NativeMethods.RECT prct = new NativeMethods.RECT();
            foreach (var item in ScreenHelper.GetScreen())
            {
                //If number of connected devices is unchanged -> devicename is also unchanged?
                if((i = Wallpapers.FindIndex(x=> ScreenHelper.ScreenCompare(item, x.GetScreen(), DisplayIdentificationMode.screenClass))) != -1)
                {
                    Logger.Info("System parameters changed: Screen Param old/new -> " + Wallpapers[i].GetScreen().Bounds + "/" + item.Bounds);
                    //Using this window as reference for MapWindowPoints.                  
                    Window w = new Window() { Width = 10, Height = 10};
                    w.Show();
                    NativeMethods.SetWindowPos(new WindowInteropHelper(w).Handle, 1, item.Bounds.Left, item.Bounds.Top, 0, 0,
                            (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE | (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE);
                    NativeMethods.MapWindowPoints(new WindowInteropHelper(w).Handle, workerw, ref prct, 2);
                    w.Close();

                    //Updating position and data of wallpaper.
                    NativeMethods.SetWindowPos(Wallpapers[i].GetHWND(), 1, prct.Left, prct.Top, (item.Bounds.Width), (item.Bounds.Height), 0 | 0x0010);
                    Wallpapers[i].SetScreen(item);
                }
            }
            RefreshDesktop();
        }

        private static void SaveWallpaperLayout()
        {
            List<WallpaperLayoutModel> layout = new List<WallpaperLayoutModel>();
            foreach (var item in Wallpapers)
            {
                layout.Add(new WallpaperLayoutModel(
                item.GetScreen(),
                item.GetWallpaperData().LivelyInfoFolderPath));
            }

            WallpaperLayoutJSON.SaveWallpaperLayout(
                  layout,
                  Path.Combine(Program.AppDataDir, "WallpaperLayout.json"));
        }

        public static void ShutDown()
        {
            if(_isInitialized)
            {
                WallpaperChanged -= SetupDesktop_WallpaperChanged;
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
                processMonitor.Stop();
                //CloseAllWallpapersWithoutEvent();
                TerminateAllWallpapersWithoutEvent();
                RefreshDesktop();
            }
        }

        #endregion //core

        #region wallpaper add

        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private static void SetWallpaperPerScreen(IntPtr handle, LivelyScreen targetDisplay)
        {

            NativeMethods.RECT prct = new NativeMethods.RECT();
            NativeMethods.POINT topLeft;
            //StaticPinvoke.POINT bottomRight;
            Logger.Info("Sending WP -> " + targetDisplay);

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
            SetFocus(true);
            RefreshDesktop();

            //logging.
            NativeMethods.GetWindowRect(handle, out prct);
            Logger.Info("Relative Coordinates of WP -> " + prct.Left + " " + prct.Right + " " + targetDisplay.Bounds.Width + " " + targetDisplay.Bounds.Height);
            topLeft.X = prct.Left;
            topLeft.Y = prct.Top;
            NativeMethods.ScreenToClient(workerw, ref topLeft);
            Logger.Info("Coordinate wrt to screen ->" + topLeft.X + " " + topLeft.Y + " " + targetDisplay.Bounds.Width + " " + targetDisplay.Bounds.Height);
        }

        /// <summary>
        /// Spans wp across all screens.
        /// </summary>
        private static void SetWallpaperSpanScreen(IntPtr handle)
        {
            NativeMethods.RECT prct = new NativeMethods.RECT();
            //get spawned workerw rectangle data.
            NativeMethods.GetWindowRect(workerw, out prct);
            SetParentWorkerW(handle);

            //fill wp into the whole workerw area.
            if (!NativeMethods.SetWindowPos(handle, 1, 0, 0, prct.Right - prct.Left, prct.Bottom - prct.Top, 0 | 0x0010))
            {
                NLogger.LogWin32Error("setwindowpos fail SpanWallpaper(),");
            }
            SetFocus(true);
            RefreshDesktop();
        }

        private static void SetWallpaperDuplicateScreen(IntPtr handle)
        {
            throw new NotImplementedException();
        }

        #endregion //wallpaper add

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

        #region wallpaper close

        public static void CloseAllWallpapers()
        {
            Wallpapers.ForEach(x => x.Close());
            Wallpapers.Clear();
            WallpaperChanged?.Invoke(null, null);
        }

        public static void CloseWallpaper(WallpaperType type)
        {
            Wallpapers.ForEach(x => 
            { 
                if (x.GetWallpaperType() == type) 
                    x.Close();             
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperType() == type);
            WallpaperChanged?.Invoke(null, null);
        }

        public static void CloseWallpaper(LivelyScreen display)
        {
            Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                    x.Close();
            });
            Wallpapers.RemoveAll(x => ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout));
            WallpaperChanged?.Invoke(null, null);
        }

        public static void CloseWallpaper(LibraryModel wp)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetWallpaperData() == wp)
                    x.Close();
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperData() == wp);
            WallpaperChanged?.Invoke(null, null);
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
                    x.SendMessage(msg);
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

        private static void CloseWallpaperWithoutEvent(LivelyScreen display)
        {
            Wallpapers.ForEach(x =>
            {
                if (ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout))
                    x.Close();
            });
            Wallpapers.RemoveAll(x => ScreenHelper.ScreenCompare(x.GetScreen(), display, DisplayIdentificationMode.screenLayout));
        }

        private static void CloseAllWallpapersWithoutEvent()
        {
            Wallpapers.ForEach(x => x.Close());
            Wallpapers.Clear();
        }

        public static void TerminateAllWallpapers()
        {
            Wallpapers.ForEach(x => x.Terminate());
            Wallpapers.Clear();
        }

        private static void TerminateAllWallpapersWithoutEvent()
        {
            Wallpapers.ForEach(x => x.Terminate());
            Wallpapers.Clear();
        }

        #endregion //wallpaper close

        #region helper functons

        /// <summary>
        /// Focus fix, otherwise when new applicaitons launch fullscreen wont giveup window handle once SetParent() is called.
        /// </summary>
        private static void SetFocus(bool focusLively = true)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                //IntPtr progman = NativeMethods.FindWindow("Progman", null);
                NativeMethods.SetForegroundWindow(progman); //change focus from the started window//application.
                NativeMethods.SetFocus(progman);

                IntPtr livelyWindow = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                if (!livelyWindow.Equals(IntPtr.Zero) && NativeMethods.IsWindowVisible(livelyWindow) && focusLively)  //todo:- not working for cefsharp wp launch, why?
                {
                    NativeMethods.SetForegroundWindow(livelyWindow);
                    NativeMethods.SetFocus(livelyWindow);
                }
            }));
        }

        /// <summary>
        /// Force redraw desktop - clears wallpaper persisting on screen even after close.
        /// </summary>
        public static void RefreshDesktop()
        {
            //todo:- right now I'm just telling windows to change wallpaper with a null value of zero size, there has to be a PROPER way to do this.
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, null, NativeMethods.SPIF_UPDATEINIFILE);
        }

        /// <summary>
        /// Adds the wp as child of spawned desktop-workerw window.
        /// </summary>
        /// <param name="windowHandle">handle of window</param>
        private static void SetParentWorkerW(IntPtr windowHandle)
        {
            if (System.Environment.OSVersion.Version.Major == 6 && System.Environment.OSVersion.Version.Minor == 1) //windows 7
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
