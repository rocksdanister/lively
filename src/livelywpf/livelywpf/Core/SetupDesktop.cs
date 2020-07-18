using livelywpf.Core;
using livelywpf.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf
{
    public static class SetupDesktop
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static IntPtr progman, workerw;
        private static bool _isInitialized = false;

        public static List<IWallpaper> Wallpapers = new List<IWallpaper>();
        //public static event EventHandler<IWallpaper> WallpaperLoaded;

        public static void SetWallpaper(LibraryModel wp, Screen targetDisplay)
        {
            if (SystemInformation.HighContrast)
            {
                Logger.Error("Failed to setup workers, high contrast mode!");
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
                    Logger.Error("Failed to setup, workerw handle null!");
                    //todo: set the settings through code using SystemParametersInfo() - complication: microsoft uses registry to update the radio button UI in the Performance dialog, 
                    //which DOES not reflect actual applied settings! o_O..will have to edit registry too.
                    return;
                }
                else
                {
                    _isInitialized = true;
                    Playback.Initialize();
                }
            }

            if( wp.LivelyInfo.Type == WallpaperType.web 
            || wp.LivelyInfo.Type == WallpaperType.webaudio 
            || wp.LivelyInfo.Type == WallpaperType.url)
            {
                //todo process exit event.
                var item = new WebProcess(wp.FilePath, wp, targetDisplay);
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
            if(e.Success)
            {
                //preview and create gif and thumbnail for user dropped file.
                if (wallpaper.GetWallpaperData().DataType == LibraryTileType.processing)
                {
                    await ShowPreviewDialogSTAThread(wallpaper);
                    if(!File.Exists(
                    Path.Combine(wallpaper.GetWallpaperData().LivelyInfoFolderPath,"LivelyInfo.json")))
                    {
                        //user cancelled/fail!
                        wallpaper.Close();
                        RefreshDesktop();
                        await System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                        {
                            Program.LibraryVM.WallpaperDelete(wallpaper.GetWallpaperData());
                        }));
                        return;
                    }
                }
                else if(wallpaper.GetWallpaperData().DataType == LibraryTileType.videoConvert)
                {
                    await ShowVLCScreenCaptureDialogSTAThread(wallpaper);
                    wallpaper.Close();
                    RefreshDesktop();
                    return;
                }

                //close previous wp, otherwise 2 fullscreen wp running sametime is heavy.
                var prevWallpaper = Wallpapers.Find(x => x.GetScreen() == wallpaper.GetScreen());
                if (prevWallpaper != null)
                {
                    CloseWallpaper(prevWallpaper.GetWallpaperData());
                }

                AddWallpaper(wallpaper.GetHWND(), wallpaper.GetScreen());
                Wallpapers.Add(wallpaper);
            }
            else
            {
                Logger.Error(e.Error.ToString());
                wallpaper.Close();
            }
        }

        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private static void AddWallpaper(IntPtr handle, Screen targetDisplay)
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

            //WallpaperLoaded?.Invoke(null, wp);
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

        public static void CloseAllWallpapers()
        {
            Wallpapers.ForEach(x => x.Close());
            Wallpapers.Clear();
            RefreshDesktop();
        }

        public static void CloseWallpaper(WallpaperType type)
        {
            Wallpapers.ForEach(x => 
            { 
                if (x.GetWallpaperType() == type) 
                    x.Close();             
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperType() == type);
            RefreshDesktop();
        }

        public static void CloseWallpaper(Screen display)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetScreen() == display)
                    x.Close();
            });
            Wallpapers.RemoveAll(x => x.GetScreen() == display);
            RefreshDesktop();
        }

        public static void CloseWallpaper(LibraryModel wp)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetWallpaperData() == wp)
                    x.Close();
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperData() == wp);
            RefreshDesktop();
        }

        public static void SendMessageWallpaper(LibraryModel wp, string msg)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetWallpaperData() == wp)
                    x.GetProcess().StandardInput.WriteLine(msg);
            });
        }

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
                    InputForwardWindow.Closing -= DesktopInputForward_Closing;
                    InputForwardWindow.Close();
                }
            }
            else
            {
                if (InputForwardWindow == null)
                {
                    InputForwardWindow = new RawInputDX(mode);
                    InputForwardWindow.Closing += DesktopInputForward_Closing;
                    InputForwardWindow.Show();
                }
                else
                {
                    if(mode != InputForwardWindow.InputMode)
                    {
                        InputForwardWindow.Closing -= DesktopInputForward_Closing;
                        InputForwardWindow.Close();

                        WallpaperInputForward(mode);
                    }
                }
            }
        }
        private static void DesktopInputForward_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            InputForwardWindow = null;
        }
    }
}
