using livelywpf.Core.API;
using livelywpf.Helpers.Pinvoke;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using livelywpf.Models;
using livelywpf.Views.Wallpapers;
using livelywpf.Helpers.Shell;

namespace livelywpf.Core.Wallpapers
{
    class WebEdge : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        private int cefD3DRenderingSubProcessPid;
        //private IntPtr chrome_WidgetWin_0;
        private readonly Webview2View player;

        public bool IsLoaded { get; private set; } = false;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public ILibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle { get; private set; }

        public Process Proc => null;

        public ILivelyScreen Screen { get; set; }

        public string LivelyPropertyCopyPath { get; }

        public WebEdge(string path, ILibraryModel model, ILivelyScreen display, string livelyPropertyPath)
        {
            LivelyPropertyCopyPath = livelyPropertyPath;

            player = new Webview2View(path, model.LivelyInfo.Type, LivelyPropertyCopyPath);
            this.Model = model;
            this.Screen = display;
        }

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                player.Close();
            }));
        }

        public void Pause()
        {
            //minimize browser.
            NativeMethods.ShowWindow(InputHandle, (uint)NativeMethods.SHOWWINDOW.SW_SHOWMINNOACTIVE);
            /*
            if (cefD3DRenderingSubProcessPid != 0)
            {
                //Cef spawns multiple subprocess but "Intermediate D3D Window" seems to do the trick..
                //The "System Idle Process" is given process ID 0, Kernel is 1.
                _ = NativeMethods.DebugActiveProcess((uint)cefD3DRenderingSubProcessPid);
            }
            */
        }

        public void Play()
        {
            //show minimized browser.
            NativeMethods.ShowWindow(InputHandle, (uint)NativeMethods.SHOWWINDOW.SW_SHOWNOACTIVATE);
            /*
            if (cefD3DRenderingSubProcessPid != 0)
            {
                _ = NativeMethods.DebugActiveProcessStop((uint)cefD3DRenderingSubProcessPid);
            }
            */
        }

        private void SendMessage(string msg)
        {
            player?.MessageProcess(msg);
        }

        public void SetVolume(int volume)
        {
            //todo
        }

        public void SetMute(bool mute)
        {
            //todo
        }

        public async void Show()
        {
            if (player != null)
            {
                player.LivelyPropertiesInitialized += Player_LivelyPropertiesInitialized;
                player.Closed += Player_Closed;
                player.Show();
                //visible window..
                this.Handle = new WindowInteropHelper(player).Handle;

                bool status = true;
                Exception error = null;
                string message = null;
                try
                {
                    var tmpHwnd = await player.InitializeWebView();
                    //input window..
                    var chrome_WidgetWin_0 = NativeMethods.FindWindowEx(tmpHwnd, IntPtr.Zero, "Chrome_WidgetWin_0", null);
                    if (!chrome_WidgetWin_0.Equals(IntPtr.Zero))
                    {
                        this.InputHandle = NativeMethods.FindWindowEx(chrome_WidgetWin_0, IntPtr.Zero, "Chrome_WidgetWin_1", null);
                    }

                    if (this.InputHandle.Equals(IntPtr.Zero))
                    {
                        throw new Exception("Webview input handle not found.");
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    status = false;
                    message = "WebView initialization fail.";
                }
                finally
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = message });
                }
            }
        }

        private void Player_LivelyPropertiesInitialized(object sender, EventArgs e)
        {
            //Takes time for rendering window to spawn.. this should be enough.
            _ = NativeMethods.GetWindowThreadProcessId(NativeMethods.FindWindowEx(InputHandle, IntPtr.Zero, "Intermediate D3D Window", null), out cefD3DRenderingSubProcessPid);
            IsLoaded = true;
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            DesktopUtil.RefreshDesktop();
        }

        public void Stop()
        {

        }

        public void Terminate()
        {
            Close();
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            if (pos == 0 && type != PlaybackPosType.relativePercent)
            {
                SendMessage(new LivelyReloadCmd());
            }
        }

        public async Task ScreenCapture(string filePath)
        {
            await player?.CaptureScreenshot(Path.GetExtension(filePath) != ".jpg" ? filePath + ".jpg" : filePath, ScreenshotFormat.jpeg);
        }

        public void SendMessage(IpcMessage obj)
        {
            player?.MessageProcess(obj);
        }
    }
}
