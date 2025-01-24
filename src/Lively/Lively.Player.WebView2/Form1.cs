using CommandLine;
using Lively.Common;
using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Storage;
using Lively.Common.JsonConverters;
using Lively.Common.Services;
using Lively.Models.Message;
using Lively.Player.WebView2.Extensions.WebView2;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebView = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Lively.Player.WebView2
{
    public partial class Form1 : Form
    {
        private WebView webView;
        private StartArgs startArgs;
        private bool isPaused = false;

        private bool initializedServices = false; //delay API init till loaded page
        private IAudioVisualizerService visualizerService;
        private IHardwareUsageService hardwareUsageService;
        private INowPlayingService nowPlayingService;

        private bool IsDebugging { get; } = BuildInfoUtil.IsDebugBuild();

        public Form1()
        {
            InitializeComponent();
            if (IsDebugging)
            {
                startArgs = new StartArgs
                {
                    // .html fullpath
                    Url = "https://google.com/",
                    //online or local(file)
                    Type = "online",
                    // LivelyProperties.json path if any
                    Properties = @"",
                    SysInfo = false,
                    NowPlaying = false,
                    AudioVisualizer = false,
                    PauseEvent = false
                };

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.Manual;
                this.Size = new Size(1920, 1080);
                this.ShowInTaskbar = true;
                this.MaximizeBox = true;
                this.MinimizeBox = true;
            }
            else
            {
                Parser.Default.ParseArguments<StartArgs>(Environment.GetCommandLineArgs())
                    .WithParsed((x) => startArgs = x)
                    .WithNotParsed(HandleParseError);

                this.WindowState = FormWindowState.Minimized;
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(-9999, 0);

                if (startArgs.Geometry != null)
                {
                    var msg = startArgs.Geometry.Split('x');
                    if (msg.Length >= 2 && int.TryParse(msg[0], out int width) && int.TryParse(msg[1], out int height))
                    {
                        this.Size = new Size(width, height);
                    }
                }
            }


            InitializeWebView2Async().Await(() => {
                _ = ListenToParent();
            }, 
            (err) => {
                WriteToParent(new LivelyMessageConsole()
                {
                    Category = ConsoleMessageType.error,
                    Message = $"InitializeWebView2 fail: {err.Message}"
                });
            });
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            WriteToParent(new LivelyMessageConsole()
            {
                Category = ConsoleMessageType.error,
                Message = $"Error parsing cmdline args: {errs.First()}",
            });

            if (Application.MessageLoop)
                Application.Exit();
            else
                Environment.Exit(1);
        }

        public async Task InitializeWebView2Async()
        {
            webView = new WebView();
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            // Ref: https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/user-data-folder
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disk-cache-size=1"); //workaround: avoid cache
            var userDataPath = Path.Combine(Constants.CommonPaths.TempWebView2Dir, Assembly.GetExecutingAssembly().GetName().Name);
            var env = await CoreWebView2Environment.CreateAsync(null, userDataPath, options);
            await webView.EnsureCoreWebView2Async(env);

            if (!IsDebugging)
            {
                // Don't allow contextmenu and devtools.
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            }

            webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

            switch (startArgs.Type)
            {
                case "online":
                    {
                        string tmp = null;
                        if (StreamUtil.TryParseShadertoy(startArgs.Url, ref tmp))
                            webView.CoreWebView2.NavigateToString(tmp);
                        else if (StreamUtil.TryParseYouTubeVideoIdFromUrl(startArgs.Url, ref tmp))
                            webView.CoreWebView2.Navigate($"https://www.youtube.com/embed/{tmp}?version=3&rel=0&autoplay=1&loop=1&controls=0&playlist={tmp}");
                        else
                            webView.CoreWebView2.Navigate(startArgs.Url);
                    }
                    break;
                case "local":
                    {
                        webView.NavigateToLocalPath(startArgs.Url);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            this.Controls.Add(webView);
            webView.Dock = DockStyle.Fill;
        }

        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            WriteToParent(new LivelyMessageConsole()
            {
                Category = ConsoleMessageType.error,
                Message = $"Process fail: {e.Reason}",
            });
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Avoid popups
            if (e.IsUserInitiated)
            {
                e.Handled = true;
                LinkUtil.OpenBrowser(e.Uri);
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            WriteToParent(new LivelyMessageHwnd()
            {
                Hwnd = webView.Handle.ToInt32()
            });
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                WriteToParent(new LivelyMessageConsole()
                {
                    Message = e.WebErrorStatus.ToString(),
                    Category = ConsoleMessageType.error
                });
                return;
            }

            await RestoreLivelyProperties(startArgs.Properties);
            WriteToParent(new LivelyMessageWallpaperLoaded() { Success = e.IsSuccess });

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

                            // CefSharp CanExecuteJavascriptInMainFrame equivalent in webview required?
                            this.Invoke((Action)(() =>
                            {
                                _ = webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(e, Formatting.Indented));
                            }));

                        }
                        catch (Exception ex)
                        {
                            WriteToParent(new LivelyMessageConsole()
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
                    hardwareUsageService = new HardwareUsageService();
                    hardwareUsageService.HWMonitor += (s, e) => {
                        try
                        {
                            if (isPaused)
                                return;

                            // CefSharp CanExecuteJavascriptInMainFrame equivalent in webview required?
                            this.Invoke((Action)(() =>
                            {
                                _ = webView.ExecuteScriptFunctionAsync("livelySystemInformation", JsonConvert.SerializeObject(e, Formatting.Indented));
                            }));
                        }
                        catch { }
                    };
                    hardwareUsageService.Start();
                }

                if (startArgs.AudioVisualizer)
                {
                    visualizerService = new NAudioVisualizerService();
                    visualizerService.AudioDataAvailable += (s, e) => {
                        try
                        {
                            if (isPaused)
                                return;

                            //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                            this.Invoke((Action)(() =>
                            {
                                _ = webView.ExecuteScriptFunctionAsync("livelyAudioListener", e);
                            }));
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

        public async Task ListenToParent()
        {
            if (IsDebugging)
                return;

            var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        // Since UTF8 is backward compatible, will work without this reader for non unicode characters.
                        string text = await reader.ReadLineAsync();
                        if (startArgs.VerboseLog)
                            Console.WriteLine(text);

                        if (string.IsNullOrEmpty(text))
                        {
                            // When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        else
                        {
                            try
                            {
                                var close = false;
                                var obj = JsonConvert.DeserializeObject<IpcMessage>(text, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                                this.Invoke((Action)(async () =>
                                {
                                    switch (obj.Type)
                                    {
                                        case MessageType.cmd_reload:
                                            // ConnectionAborted issue.
                                            //try
                                            //{
                                            //    webView?.Reload();
                                            //}
                                            //catch (Exception ie)
                                            //{
                                            //    WriteToParent(new LivelyMessageConsole()
                                            //    {
                                            //        Category = ConsoleMessageType.error,
                                            //        Message = $"Reload failed: {ie.Message}"
                                            //    });
                                            //}
                                            break;
                                        case MessageType.cmd_suspend:
                                            if (startArgs.PauseEvent && !isPaused)
                                            {
                                                //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                                                await webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged",
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
                                                    //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                                                    await webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged",
                                                        JsonConvert.SerializeObject(new WallpaperPlaybackState() { IsPaused = false }),
                                                        Formatting.Indented);
                                                }

                                                if (startArgs.NowPlaying)
                                                {

                                                    //TODO: CefSharp CanExecuteJavascriptInMainFrame equivalent in webview
                                                    await webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(nowPlayingService?.CurrentTrack, Formatting.Indented));
                                                }
                                            }
                                            isPaused = false;
                                            break;
                                        case MessageType.cmd_volume:
                                            var vc = (LivelyVolumeCmd)obj;
                                            webView.CoreWebView2.IsMuted = vc.Volume == 0;
                                            break;
                                        case MessageType.cmd_screenshot:
                                            var success = true;
                                            var scr = (LivelyScreenshotCmd)obj;
                                            try
                                            {
                                                await webView.CaptureScreenshot(scr.Format, scr.FilePath);
                                            }
                                            catch (Exception ie)
                                            {
                                                success = false;
                                                WriteToParent(new LivelyMessageConsole()
                                                {
                                                    Category = ConsoleMessageType.error,
                                                    Message = $"Screenshot capture fail: {ie.Message}"
                                                });
                                            }
                                            finally
                                            {
                                                WriteToParent(new LivelyMessageScreenshot()
                                                {
                                                    FileName = Path.GetFileName(scr.FilePath),
                                                    Success = success
                                                });
                                            }
                                            break;
                                        case MessageType.lp_slider:
                                            var sl = (LivelySlider)obj;
                                            await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", sl.Name, sl.Value);
                                            break;
                                        case MessageType.lp_textbox:
                                            var tb = (LivelyTextBox)obj;
                                            await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", tb.Name, tb.Value);
                                            break;
                                        case MessageType.lp_dropdown:
                                            var dd = (LivelyDropdown)obj;
                                            await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", dd.Name, dd.Value);
                                            break;
                                        case MessageType.lp_cpicker:
                                            var cp = (LivelyColorPicker)obj;
                                            await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", cp.Name, cp.Value);
                                            break;
                                        case MessageType.lp_chekbox:
                                            var cb = (LivelyCheckbox)obj;
                                            await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", cb.Name, cb.Value);
                                            break;
                                        case MessageType.lp_fdropdown:
                                            var fd = (LivelyFolderDropdown)obj;
                                            var filePath = fd.Value is null ? null : Path.Combine(Path.GetDirectoryName(startArgs.Url), fd.Value);
                                            await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", fd.Name, File.Exists(filePath) ? fd.Value : null);
                                            break;
                                        case MessageType.lp_button:
                                            var btn = (LivelyButton)obj;
                                            if (btn.IsDefault)
                                                await RestoreLivelyProperties(startArgs.Properties);
                                            else
                                                await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", btn.Name, true);
                                            break;
                                        case MessageType.lsp_perfcntr:
                                            await webView.ExecuteScriptFunctionAsync("livelySystemInformation", JsonConvert.SerializeObject(((LivelySystemInformation)obj).Info, Formatting.Indented));

                                            break;
                                        case MessageType.lsp_nowplaying:
                                            await webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(((LivelySystemNowPlaying)obj).Info, Formatting.Indented));
                                            break;
                                        case MessageType.cmd_close:
                                            close = true;
                                            break;
                                    }
                                }));

                                if (close)
                                    break;
                            }
                            catch (Exception ie)
                            {
                                WriteToParent(new LivelyMessageConsole()
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
                WriteToParent(new LivelyMessageConsole()
                {
                    Category = ConsoleMessageType.error,
                    Message = $"Ipc stdin error: {e.Message}",
                });
            }
            finally
            {
                this.Invoke((Action)Application.Exit);
            }
        }

        private async Task RestoreLivelyProperties(string propertyPath)
        {
            try
            {
                await LivelyPropertyUtil.LoadProperty(propertyPath, Path.GetDirectoryName(startArgs.Url), async (key, value) =>
                {
                    await webView.ExecuteScriptFunctionAsync("livelyPropertyListener", key, value);
                });
            }
            catch (Exception ex)
            {
                WriteToParent(new LivelyMessageConsole()
                {
                    Category = ConsoleMessageType.error,
                    Message = ex.Message
                });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            visualizerService?.Dispose();
            hardwareUsageService?.Stop();
            nowPlayingService?.Stop();
            webView?.Dispose();
        }

        public void WriteToParent(IpcMessage obj)
        {
            if (!IsDebugging)
                Console.WriteLine(JsonConvert.SerializeObject(obj));

            Debug.WriteLine(JsonConvert.SerializeObject(obj));
        }
    }
}
