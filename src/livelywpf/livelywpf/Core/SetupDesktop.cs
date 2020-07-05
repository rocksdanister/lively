using livelywpf.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static List<IWallpaper> pendingWallpapers = new List<IWallpaper>();

        public static async void SetWallpaper(LibraryModel wp, Screen targetDisplay)
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

            if( wp.LivelyInfo.Type == WallpaperType.web || wp.LivelyInfo.Type == WallpaperType.webaudio || wp.LivelyInfo.Type == WallpaperType.url)
            {
                //todo: Process Exited event.
                pendingWallpapers.Add(new WebProcess(wp.FilePath, wp, targetDisplay));
                //Receiving cefsharp window handle through ipc.
                pendingWallpapers[^1].GetProcess().OutputDataReceived += WebProcess_OutputDataReceived;
                pendingWallpapers[^1].Show();
            }
            else if(wp.LivelyInfo.Type == WallpaperType.video)
            {
                /*
                pendingWallpapers.Add(new VideoPlayerWPF(wp.FilePath, wp, targetDisplay));
                pendingWallpapers[^1].Show();
                AddWallpaper(pendingWallpapers[^1]);
                */
                pendingWallpapers.Add(new VideoPlayerMPV(wp.FilePath, wp, targetDisplay));
                pendingWallpapers[^1].Show();
                AddWallpaper(pendingWallpapers[^1]);
            }
            else if(wp.LivelyInfo.Type == WallpaperType.gif)
            {
                pendingWallpapers.Add(new GIFPlayerUWP(wp.FilePath, wp, targetDisplay));
                pendingWallpapers[^1].Show();
                AddWallpaper(pendingWallpapers[^1]);
            }
        }

        /// <summary>
        /// Calculates the position of window w.r.t parent workerw handle & sets it as child window to it.
        /// </summary>
        /// <param name="handle">window handle of process to add as wallpaper</param>
        /// <param name="display">displaystring of display to sent wp to.</param>
        private static async void AddWallpaper(IWallpaper wp)
        {
            //close to early, otherwise 2 fullscreen wp running sametime is heavy.
            var currWallpaper = Wallpapers.Find(x => x.GetScreen() == wp.GetScreen());
            if (currWallpaper != null)
            {
                CloseWallpaper(currWallpaper.GetScreen());
            }
            Wallpapers.Add(wp);
            pendingWallpapers.Remove(wp);

            NativeMethods.RECT prct = new NativeMethods.RECT();
            NativeMethods.POINT topLeft;
            //StaticPinvoke.POINT bottomRight;
            IntPtr handle = wp.GetHWND();
            Screen targetDisplay = wp.GetScreen();
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

        private static void WebProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                IntPtr handle = new IntPtr();
                //Retrieves the windowhandle of cefsubprocess, cefsharp is launching cef as a separate proces..
                //If you add the full pgm as child of workerw then there are problems (prob related sharing input queue)
                //Instead hiding the pgm window & adding cefrender window instead.
                Logger.Info("Cefsharp Handle:- " + e.Data);
                if (e.Data.Contains("HWND"))
                {
                    var webBrowser = pendingWallpapers.Find(x => x.GetProcess() == sender);

                    handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
                    //note-handle: WindowsForms10.Window.8.app.0.141b42a_r9_ad1

                    //hidin other windows, no longer required since I'm doing it in cefsharp pgm itself.
                    NativeMethods.ShowWindow(webBrowser.GetProcess().MainWindowHandle, 0);

                    //WARNING:- If you put the whole cefsharp window, workerw crashes and refuses to start again on next startup!!, this is a workaround.
                    handle = NativeMethods.FindWindowEx(handle, IntPtr.Zero, "Chrome_WidgetWin_0", null);
                    //cefRenderWidget = StaticPinvoke.FindWindowEx(handle, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", null);
                    //cefIntermediate = StaticPinvoke.FindWindowEx(handle, IntPtr.Zero, "Intermediate D3D Window", null);

                    if (IntPtr.Equals(handle, IntPtr.Zero))//unlikely.
                    {
                        webBrowser.Close();
                        pendingWallpapers.Remove(webBrowser);
                        Logger.Error("cef-error: Error getting webhandle, terminating!.");
                        return;
                    }

                    webBrowser.SetHWND(handle);
                    AddWallpaper(webBrowser);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("cef-error: " + ex.ToString());
            }
        }

        public static void CloseAllWallpapers()
        {
            Wallpapers.ForEach(x => x.Close());
            Wallpapers.Clear();
        }

        public static void CloseWallpaper(WallpaperType type)
        {
            Wallpapers.ForEach(x => 
            { 
                if (x.GetWallpaperType() == type) 
                    x.Close();             
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperType() == type);
        }

        public static void CloseWallpaper(Screen display)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetScreen() == display)
                    x.Close();
            });
            Wallpapers.RemoveAll(x => x.GetScreen() == display);
        }

        public static void CloseWallpaper(LibraryModel wp)
        {
            Wallpapers.ForEach(x =>
            {
                if (x.GetWallpaperData() == wp)
                    x.Close();
            });
            Wallpapers.RemoveAll(x => x.GetWallpaperData() == wp);
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
    }
}
