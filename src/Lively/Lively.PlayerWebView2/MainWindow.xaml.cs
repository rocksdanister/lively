using CommandLine;
using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.PlayerWebView2.Services;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Lively.PlayerWebView2
{
    //ref: https://docs.microsoft.com/en-us/microsoft-edge/webview2/gettingstarted/wpf
    public partial class MainWindow : Window
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Debug only")]
        private StartArgs startArgs;
        private bool isPaused = false;

        private bool initializedServices = false; //delay API init till loaded page
        private IAudioVisualizerService visualizerService;
        private IHardwareUsageService hardwareUsageService;
        private INowPlayingService nowPlayingService;

        public MainWindow(string[] args)
        {
            InitializeComponent();
#if DEBUG
            startArgs = new StartArgs
            {
                // .html fullpath
                Url = @"",
                //online or local(file)
                Type = "local",
                // LivelyProperties.json path if any
                Properties = @"",
                SysInfo = false,
                NowPlaying = false,
                AudioVisualizer = false,
            };

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ShowActivated = true;
            this.ResizeMode = ResizeMode.CanResize;
#endif

#if DEBUG != true
            Parser.Default.ParseArguments<StartArgs>(args)
            .WithParsed((x) => startArgs = x)
            .WithNotParsed(HandleParseError);
#endif
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            App.WriteToParent(new LivelyMessageConsole()
            {
                Category = ConsoleMessageType.error,
                Message = $"Error parsing cmdline args: {errs.First()}",
            });
            Application.Current.Shutdown();
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            try
            {
                await InitializeWebView();
                App.WriteToParent(new LivelyMessageHwnd()
                {
                    Hwnd = webView.Handle.ToInt32()
                });
            }
            finally
            {
#if DEBUG != true
                _ = StdInListener();
#endif
            }
        }

        public async Task InitializeWebView()
        {
            //Ref: https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/user-data-folder
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disk-cache-size=1"); //workaround: avoid cache
            var env = await CoreWebView2Environment.CreateAsync(null, Constants.CommonPaths.TempWebView2Dir, options);
            await webView.EnsureCoreWebView2Async(env);

            webView.CoreWebView2.ProcessFailed += (s, e) =>
            {
                App.WriteToParent(new LivelyMessageConsole()
                {
                    Category = ConsoleMessageType.error,
                    Message = $"Process fail: {e.Reason}",
                });
            };

            webView.NavigationCompleted += WebView_NavigationCompleted;

            webView.CoreWebView2.NewWindowRequested += (s, e) => 
            {
                if (e.IsUserInitiated) //avoid popups
                {
                    e.Handled = true;
                    LinkHandler.OpenBrowser(e.Uri);
                }
            };

            if (startArgs.Type.Equals("online", StringComparison.OrdinalIgnoreCase))
            {
                string tmp = null;
                if (StreamUtil.TryParseShadertoy(startArgs.Url, ref tmp))
                {
                    webView.CoreWebView2.NavigateToString(tmp);
                }
                else if (StreamUtil.TryParseYouTubeVideoIdFromUrl(startArgs.Url, ref tmp))
                {
                    //fullscreen yt embed player with looping enabled.
                    webView.CoreWebView2.Navigate("https://www.youtube.com/embed/" + tmp +
                        "?version=3&rel=0&autoplay=1&loop=1&controls=0&playlist=" + tmp);
                }
                else
                {
                    webView.CoreWebView2.Navigate(startArgs.Url);
                }
            }
            else
            {
                //webView.CoreWebView2.SetVirtualHostNameToFolderMapping(Path.GetFileName(startArgs.Url), Path.GetDirectoryName(startArgs.Url), CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.Navigate(startArgs.Url);
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            await RestoreLivelyProperties(startArgs.Properties);
            App.WriteToParent(new LivelyMessageWallpaperLoaded() { Success = true });

            if (!initializedServices)
            {
                initializedServices = true;
                if (startArgs.NowPlaying)
                {
                    nowPlayingService = new NpsmNowPlayingService();
                    nowPlayingService.NowPlayingTrackChanged += (s, e) => {
                        try
                        {
                            if (isPaused)
                                return;

                            this.Dispatcher.Invoke(() =>
                            {
                                //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                                _ = ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(e, Formatting.Indented));
                            });
                        }
                        catch (Exception ex)
                        {
                            App.WriteToParent(new LivelyMessageConsole()
                            {
                                Category = ConsoleMessageType.log,
                                Message = $"Error sending track:{ex.Message}",
                            });

                        }
                    };
                    nowPlayingService.Start();
                }


                if (startArgs.SysInfo)
                {
                    hardwareUsageService = new PerfCounterUsageService();
                    hardwareUsageService.HWMonitor += (s, e) => {
                        try
                        {
                            if (isPaused)
                                return;

                            //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                            this.Dispatcher.Invoke(() =>
                            {
                                _ = ExecuteScriptFunctionAsync("livelySystemInformation", JsonConvert.SerializeObject(e, Formatting.Indented));
                            });
                        }
                        catch { }
                    };
                    hardwareUsageService.Start();
                }

                if (startArgs.AudioVisualizer)
                {
                    visualizerService = new AudioVisualizerService();
                    visualizerService.AudioDataAvailable += (s, e) => {
                        try
                        {
                            if (isPaused)
                                return;

                            //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                            this.Dispatcher.Invoke(() =>
                            {
                                _ = ExecuteScriptFunctionAsync("livelyAudioListener", e);
                            });
                        }
                        catch { }
                    };
                    visualizerService.Start();
                }
            }
        }

        private class WallpaperPlaybackState
        {
            public bool IsPaused { get; set; }
        }

        public async Task StdInListener()
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        var msg = await Console.In.ReadLineAsync();
                        if (string.IsNullOrEmpty(msg))
                        {
                            //When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        else
                        {
                            try
                            {
                                var close = false;
                                var obj = JsonConvert.DeserializeObject<IpcMessage>(msg, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                                this.Dispatcher.Invoke(() =>
                                {
                                    switch (obj.Type)
                                    {
                                        case MessageType.cmd_reload:
                                            webView?.Reload();
                                            break;
                                        case MessageType.cmd_suspend:
                                            if (startArgs.PauseEvent && !isPaused)
                                            {
                                                //TODO: check if js context ready
                                                _ = ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged",
                                                    JsonConvert.SerializeObject(new WallpaperPlaybackState() { IsPaused = true }),
                                                    Formatting.Indented);
                                            }
                                            isPaused = true;
                                            break;
                                        case MessageType.cmd_resume:
                                            if (isPaused)
                                            {
                                                if (startArgs.PauseEvent)
                                                {
                                                    //TODO: check if js context ready
                                                    _ = ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged",
                                                        JsonConvert.SerializeObject(new WallpaperPlaybackState() { IsPaused = false }),
                                                        Formatting.Indented);
                                                }
                                                
                                                if (startArgs.NowPlaying)
                                                {

                                                    //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                                                    _ = ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(nowPlayingService?.CurrentTrack, Formatting.Indented));
                                                }
                                            }
                                            isPaused = false;
                                            break;
                                        case MessageType.lp_slider:
                                            var sl = (LivelySlider)obj;
                                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", sl.Name, sl.Value);
                                            break;
                                        case MessageType.lp_textbox:
                                            var tb = (LivelyTextBox)obj;
                                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", tb.Name, tb.Value);
                                            break;
                                        case MessageType.lp_dropdown:
                                            var dd = (LivelyDropdown)obj;
                                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", dd.Name, dd.Value);
                                            break;
                                        case MessageType.lp_cpicker:
                                            var cp = (LivelyColorPicker)obj;
                                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", cp.Name, cp.Value);
                                            break;
                                        case MessageType.lp_chekbox:
                                            var cb = (LivelyCheckbox)obj;
                                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", cb.Name, cb.Value);
                                            break;
                                        case MessageType.lp_fdropdown:
                                            var fd = (LivelyFolderDropdown)obj;
                                            var filePath = Path.Combine(Path.GetDirectoryName(startArgs.Url), fd.Value);
                                            if (File.Exists(filePath))
                                            {
                                                _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                                                fd.Name,
                                                fd.Value);
                                            }
                                            else
                                            {
                                                _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                                                fd.Name,
                                                null); //or custom msg
                                            }
                                            break;
                                        case MessageType.lp_button:
                                            var btn = (LivelyButton)obj;
                                            if (btn.IsDefault)
                                            {
                                                _ = RestoreLivelyProperties(startArgs.Properties);
                                            }
                                            else
                                            {
                                                _ = ExecuteScriptFunctionAsync("livelyPropertyListener", btn.Name, true);
                                            }
                                            break;
                                        case MessageType.lsp_perfcntr:
                                            if (!isPaused)
                                            {
                                                _ = ExecuteScriptFunctionAsync("livelySystemInformation", JsonConvert.SerializeObject(((LivelySystemInformation)obj).Info, Formatting.Indented));
                                            }
                                            break;
                                        case MessageType.lsp_nowplaying:
                                            if (!isPaused)
                                            {
                                                _ = ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(((LivelySystemNowPlaying)obj).Info, Formatting.Indented));
                                            }
                                            break;
                                        case MessageType.cmd_close:
                                            close = true;
                                            break;
                                    }
                                });

                                if (close)
                                {
                                    break;
                                }
                            }
                            catch (Exception ie)
                            {
                                App.WriteToParent(new LivelyMessageConsole()
                                {
                                    Category = ConsoleMessageType.error,
                                    Message = $"Ipc action error: {ie.Message}"
                                });
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                App.WriteToParent(new LivelyMessageConsole()
                {
                    Category = ConsoleMessageType.error,
                    Message = $"Ipc stdin error: {e.Message}",
                });
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            }
        }

        private async Task RestoreLivelyProperties(string path)
        {
            try
            {
                if (path == null)
                    return;

                foreach (var item in JsonUtil.ReadJObject(path))
                {
                    string uiElementType = item.Value["type"].ToString();
                    if (!uiElementType.Equals("button", StringComparison.OrdinalIgnoreCase) && !uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                    {
                        if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase) ||
                            uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                        {
                            await ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (int)item.Value["value"]);
                        }
                        else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                        {
                            var filePath = Path.Combine(Path.GetDirectoryName(startArgs.Url), item.Value["folder"].ToString(), item.Value["value"].ToString());
                            if (File.Exists(filePath))
                            {
                                await ExecuteScriptFunctionAsync("livelyPropertyListener",
                                item.Key,
                                Path.Combine(item.Value["folder"].ToString(), item.Value["value"].ToString()));
                            }
                            else
                            {
                                await ExecuteScriptFunctionAsync("livelyPropertyListener",
                                item.Key,
                                null); //or custom msg
                            }
                        }
                        else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                        {
                            await ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (bool)item.Value["value"]);
                        }
                        else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase) || uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                        {
                            await ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (string)item.Value["value"]);
                        }
                    }
                }
            }
            catch { /* TODO */ }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG != true
            IntPtr handle = new WindowInteropHelper(this).Handle;
            //ShowInTaskbar = false : causing issue with windows10 Taskview.
            WindowOperations.RemoveWindowFromTaskbar(handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
#endif
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            visualizerService?.Dispose();
            hardwareUsageService?.Stop();
            nowPlayingService?.Stop();
            webView?.Dispose();
        }

#region helpers

        //credit: https://stackoverflow.com/questions/62835549/equivalent-of-webbrowser-invokescriptstring-object-in-webview2
        private async Task<string> ExecuteScriptFunctionAsync(string functionName, params object[] parameters)
        {
            var script = new StringBuilder();
            script.Append(functionName);
            script.Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                script.Append(JsonConvert.SerializeObject(parameters[i]));
                if (i < parameters.Length - 1)
                {
                    script.Append(", ");
                }
            }
            script.Append(");");
            return await webView?.ExecuteScriptAsync(script.ToString());
        }

        //ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/529
        public async Task CaptureScreenshot(string filePath, ScreenshotFormat format)
        {
            var base64String = await CaptureScreenshot(format);
            var imageBytes = Convert.FromBase64String(base64String);
            switch (format)
            {
                case ScreenshotFormat.jpeg:
                case ScreenshotFormat.png:
                case ScreenshotFormat.webp:
                    {
                        // Write to disk
                        File.WriteAllBytes(filePath, imageBytes);
                    }
                    break;
                case ScreenshotFormat.bmp:
                    {
                        // Convert byte[] to Image
                        using MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                        using Image image = Image.FromStream(ms, true);
                        image.Save(filePath, ImageFormat.Bmp);
                    }
                    break;
            }
        }

        private async Task<string> CaptureScreenshot(ScreenshotFormat format)
        {
            var param = format switch
            {
                ScreenshotFormat.jpeg => "{\"format\":\"jpeg\"}",
                ScreenshotFormat.webp => "{\"format\":\"webp\"}",
                ScreenshotFormat.png => "{}", // Default
                ScreenshotFormat.bmp => "{}", // Not supported by cef
                _ => "{}",
            };
            string r3 = await webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", param);
            JObject o3 = JObject.Parse(r3);
            JToken data = o3["data"];
            return data.ToString();
        }

        private Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }

        private BitmapImage Base64ToBitmapImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            BitmapImage bi = new BitmapImage();
            using MemoryStream ms = new MemoryStream(imageBytes);
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            //bi.DecodePixelWidth = 1280;
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }

#endregion //helpers
    }
}
