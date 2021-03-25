using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;

namespace livelywpf.Core
{
    class WebEdge : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        IntPtr HWND { get; set; }
        WebView2Element Player { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        string LivelyPropertyCopy { get; set; }

        public WebEdge(string path, LibraryModel model, LivelyScreen display)
        {
            LivelyPropertyCopy = null;
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
                        var wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, screenNumber);
                        Directory.CreateDirectory(wpdataFolder);

                        LivelyPropertyCopy = Path.Combine(wpdataFolder, "LivelyProperties.json");
                        if (!File.Exists(LivelyPropertyCopy))
                            File.Copy(model.LivelyPropertyPath, LivelyPropertyCopy);

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

            Player = new WebView2Element(path, model.LivelyInfo.Type, LivelyPropertyCopy);
            this.Model = model;
            this.Display = display;
        }

        public void Close()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                Player.Close();
            }));
        }

        public IntPtr GetHWND()
        {
            return HWND;
        }

        public string GetLivelyPropertyCopyPath()
        {
            return LivelyPropertyCopy;
        }

        public Process GetProcess()
        {
            return null;
        }

        public LivelyScreen GetScreen()
        {
            return Display;
        }

        public LibraryModel GetWallpaperData()
        {
            return Model;
        }

        public WallpaperType GetWallpaperType()
        {
            return Model.LivelyInfo.Type;
        }

        public void Pause()
        {

        }

        public void Play()
        {

        }

        public void SendMessage(string msg)
        {
            if(Player != null)
            {
                Player.SendMessage(msg);
            }
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public void SetVolume(int volume)
        {

        }

        public void Show()
        {
            if (Player != null)
            {
                Player.Closed += Player_Closed;
                Player.Show();
                HWND = new WindowInteropHelper(Player).Handle;
                //Logger.Debug("WEBVIEW2: old HWND=>" + HWND);
                //Player.webView.CoreWebView2Ready += WebView_CoreWebView2Ready;
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null });
            }
        }

        private void WebView_CoreWebView2Ready(object sender, EventArgs e)
        {
            Logger.Debug("WEBVIEW2: new HWND=>" + Player.webView.Handle);   
            //SetHWND(Player.webView.Handle);
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

        public void SetPlaybackPos(float pos)
        {
            //todo: reload if 0
        }
    }
}
