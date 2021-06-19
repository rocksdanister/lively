using livelywpf.Core.API;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf.Core
{
    class WebEdge : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        private IntPtr hwnd, hwndInput;
        private readonly WebView2Element player;
        private readonly LibraryModel model;
        private LivelyScreen display;
        private readonly string livelyPropertyCopyPath;

        public WebEdge(string path, LibraryModel model, LivelyScreen display)
        {
            livelyPropertyCopyPath = null;
            if (model.LivelyPropertyPath != null)
            {
                //customisable wallpaper, livelyproperty.json is present.
                var dataFolder = Path.Combine(Program.WallpaperDir, "SaveData", "wpdata");
                try
                {
                    //extract last digits of the Screen class DeviceName, eg: \\.\DISPLAY4 -> 4
                    var screenNumber = display.DeviceNumber;
                    if (screenNumber != null)
                    {
                        //Create a directory with the wp foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                        //Further modifications are done to the copy file.
                        string wpdataFolder = null;
                        switch (Program.SettingsVM.Settings.WallpaperArrangement)
                        {
                            case WallpaperArrangement.per:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, screenNumber);
                                break;
                            case WallpaperArrangement.span:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "span");
                                break;
                            case WallpaperArrangement.duplicate:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "duplicate");
                                break;
                        }
                        Directory.CreateDirectory(wpdataFolder);
                        //copy the original file if not found..
                        livelyPropertyCopyPath = Path.Combine(wpdataFolder, "LivelyProperties.json");
                        if (!File.Exists(livelyPropertyCopyPath))
                        {
                            File.Copy(model.LivelyPropertyPath, livelyPropertyCopyPath);
                        }
                    }
                    else
                    {
                        //todo: fallback, use the original file (restore feature disabled.)
                    }
                }
                catch
                {
                    //todo: fallback, use the original file (restore feature disabled.)
                }
            }

            player = new WebView2Element(path, model.LivelyInfo.Type, livelyPropertyCopyPath);
            this.model = model;
            this.display = display;
        }

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                player.Close();
            }));
        }

        public IntPtr GetHWND()
        {
            return hwnd;
        }

        public IntPtr GetHWNDInput()
        {
            return hwndInput;
        }

        public string GetLivelyPropertyCopyPath()
        {
            return livelyPropertyCopyPath;
        }

        public Process GetProcess()
        {
            return null;
        }

        public LivelyScreen GetScreen()
        {
            return display;
        }

        public LibraryModel GetWallpaperData()
        {
            return model;
        }

        public WallpaperType GetWallpaperType()
        {
            return model.LivelyInfo.Type;
        }

        public void Pause()
        {
            //minimize browser.
            NativeMethods.ShowWindow(GetHWND(), (uint)NativeMethods.SHOWWINDOW.SW_SHOWMINNOACTIVE);
        }

        public void Play()
        {
            //show minimized browser.
            NativeMethods.ShowWindow(GetHWND(), (uint)NativeMethods.SHOWWINDOW.SW_SHOWNOACTIVATE);
        }

        public void SendMessage(string msg)
        {
            player?.MessageProcess(msg);
        }

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public void SetVolume(int volume)
        {

        }

        public async void Show()
        {
            if (player != null)
            {
                player.Closed += Player_Closed;
                player.Show();
                //visible window..
                this.hwnd = new WindowInteropHelper(player).Handle;

                try
                {
                    var tmpHwnd = await player.InitializeWebView();
                    //input window..
                    var parentHwnd = NativeMethods.FindWindowEx(tmpHwnd, IntPtr.Zero, "Chrome_WidgetWin_0", null);
                    if (!parentHwnd.Equals(IntPtr.Zero))
                    {
                        this.hwndInput = NativeMethods.FindWindowEx(parentHwnd, IntPtr.Zero, "Chrome_WidgetWin_1", null);
                    }

                    if (this.hwndInput.Equals(IntPtr.Zero))
                    {
                        throw new Exception("Input handle null.");
                    }
                    //everything ready..
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
                }
                catch (Exception e)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = "Failed to init webview." });
                }
            }
        }

        private void Player_Closed(object sender, EventArgs e)
        {
            SetupDesktop.RefreshDesktop();
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

        public Task ScreenCapture(string filePath)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(IpcMessage obj)
        {
            player?.MessageProcess(obj);
        }
    }
}
