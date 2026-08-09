using CommandLine;
using Lively.Common;
using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.JsonConverters;
using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.Models.Message;
using Lively.Player.WebView2.Extensions.WebView2;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private bool isVideoStream = false;
        private int cefD3DRenderingSubProcessId;

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
                    Type = WebPageType.online,
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

                var darkColor = Color.FromArgb(30, 30, 30);
                var lightColor = Color.FromArgb(240, 240, 240);
                this.BackColor = startArgs.Theme switch
                {
                    AppTheme.Auto => ThemeUtil.GetWindowsTheme() == AppTheme.Dark ? darkColor : lightColor,
                    AppTheme.Light => lightColor,
                    AppTheme.Dark => darkColor,
                    _ => darkColor,
                };
            }

            InitializeWebView2Async().Await(() => {
                _ = ListenToParent();
            }, 
            (err) =>
            {
                err.SendError(SendToParent, "Failed to initialize WebView2");
                // Exit or display custom error page.
                Environment.Exit(1);
            });
        }

        // Hide from taskview and taskbar.
        // ShowInTaskbar = true does not create TOOLWINDOW.
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            if (errs != null)
                string.Join(Environment.NewLine, errs).SendError(SendToParent, "Error parsing launch arguments");

            // ERROR_INVALID_PARAMETER
            // Ref: <https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499->
            Environment.Exit(87);
        }

        public async Task InitializeWebView2Async()
        {
            webView = new WebView() {
                DefaultBackgroundColor = Color.Transparent
            };
            webView.NavigationCompleted += WebView_NavigationCompleted;

            var webViewStartArgs =
                // Allow media autoplay even if not muted.
                "--autoplay-policy=no-user-gesture-required " +
                // Disable SMTC.
                "--disable-features=HardwareMediaKeyHandling " +
                // Enable Vulkan GPU acceleration & ANGLE backend
                "--use-angle=vulkan " +
                "--enable-features=Vulkan,VulkanFromANGLE,DefaultANGLEVulkan " +
                "--enable-gpu-rasterization " +
                "--enable-zero-copy " +
                "--ignore-gpu-blocklist " +
                // Constrain JavaScript heap RAM & process count for web wallpapers
                "--js-flags=\"--max-old-space-size=128\" " +
                "--renderer-process-limit=1 ";
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions(webViewStartArgs);
            // WebView2 does not have in-memory mode, ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/3637
            // Custom user data folder, ref: https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/user-data-folder
            $"Setting UserData path: {startArgs.UserDataPath}".SendLog(SendToParent);
            var env = await CoreWebView2Environment.CreateAsync(null, startArgs.UserDataPath, options);
            await webView.EnsureCoreWebView2Async(env);

            // Defaults
            webView.CoreWebView2.IsMuted = startArgs.Volume == 0;
            webView.CoreWebView2.Profile.PreferredColorScheme = startArgs.Theme.GetPreferredColorScheme();
            if (startArgs.Scale != null)
            {
                $"Attempt to set scale: {startArgs.Scale.Value}".SendLog(SendToParent);
                try
                {
                    // RasterizationScale is better choice than ZoomFactor since it is not affected by system text size but not exposed in .NET, ref:
                    // https://github.com/MicrosoftEdge/WebView2Feedback/issues/4775
                    // https://github.com/MicrosoftEdge/WebView2Feedback/issues/3839
                    // https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2controller.rasterizationscale
                    var field = typeof(WebView).GetField("_coreWebView2Controller", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field?.GetValue(webView) is CoreWebView2Controller controller)
                    {
                        controller.ShouldDetectMonitorScaleChanges = false;
                        // Bug: Initial value is always 1, ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/5329
                        $"Current scale: {controller.RasterizationScale}".SendLog(SendToParent);
                        controller.RasterizationScale = startArgs.Scale.Value;
                        $"Updated scale: {controller.RasterizationScale}".SendLog(SendToParent);
                    }
                    else
                    {
                        "CoreWebView2Controller not found".SendError(SendToParent);
                    }
                }
                catch (Exception ex)
                {
                    ex.SendError(SendToParent, "Failed to set scale");
                }
            }

            if (!IsDebugging)
            {
                // Don't allow contextmenu and devtools.
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            }

            if (!string.IsNullOrWhiteSpace(startArgs.DebugPort) && int.TryParse(startArgs.DebugPort, out int _))
            {
                // In WebView2 --remote-debugging-port=XXXX is not opening up a direct connection, we will just ignore port and open DevTools instead.
                webView.CoreWebView2.OpenDevToolsWindow();
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            }

            webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            webView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;

            switch (startArgs.Type)
            {
                case WebPageType.online:
                    {
                        var (contentType, id) = WebContentUtil.GetContentType(startArgs.Url);
                        switch (contentType)
                        {
                            case WebContentType.shadertoy:
                                {
                                    $"Opening shadertoy shader: {id}".SendLog(SendToParent);
                                    webView.CoreWebView2.Navigate($"https://www.shadertoy.com/embed/{id}?gui=false&t=10&paused=false&muted=true");
                                }
                                break;
                            case WebContentType.youtube:
                                {
                                    isVideoStream = true;
                                    $"Opening yt stream: {id}".SendLog(SendToParent);
                                    // Error 153, ref: https://github.com/rocksdanister/lively/issues/2991
                                    webView.CoreWebView2.AddWebResourceRequestedFilter("*youtube.com/*", CoreWebView2WebResourceContext.All);
                                    webView.CoreWebView2.WebResourceRequested += (s, e) =>
                                    {
                                        var headers = e.Request.Headers;
                                        if (!headers.Contains("Referer"))
                                            headers.SetHeader("Referer", "https://localhost/");
                                    };
                                    webView.CoreWebView2.Navigate($"https://www.youtube.com/embed/{id}?version=3&rel=0&autoplay=1&loop=1&controls=0&playlist={id}");
                                }
                                break;
                            case WebContentType.generic:
                                {
                                    $"Opening address: {startArgs.Url}".SendLog(SendToParent);
                                    webView.CoreWebView2.Navigate(startArgs.Url);
                                }
                                break;
                            case WebContentType.none:
                                {
                                    "Failed to parse url".SendError(SendToParent);
                                    throw new ArgumentException(startArgs.Url);
                                }
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    break;
                case WebPageType.local:
                    {
                        $"Opening local project: {startArgs.Url}".SendLog(SendToParent);
                        webView.NavigateToLocalPath(startArgs.Url);
                    }
                    break;
            }
            this.Controls.Add(webView);
            webView.Dock = DockStyle.Fill;

            // Make sure to do this after WebView control is attached to form to avoid issues.
            SendToParent(new LivelyMessageHwnd() {
                Hwnd = webView.Handle.ToInt32()
            });
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            this.Invoke((MethodInvoker)(() => this.Text = webView.CoreWebView2.DocumentTitle));
        }

        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            // Expected behavior: DebugActiveProcess(CEF_D3DRenderingSubProcess)
            // Ref: https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2processfailedkind
            if (isPaused && e.Reason == CoreWebView2ProcessFailedReason.Unresponsive)
                return;

            e.Reason.ToString().SendError(SendToParent, "CoreWebView2 process failed");
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Avoid popups.
            if (e.IsUserInitiated)
            {
                e.Handled = true;
                // Open new tab/window  hyperlink (target="_blank") in default browser.
                LinkUtil.OpenBrowser(e.Uri);
            }
        }

        private void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Cancel user requested downloads.
            e.Cancel = true;
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Restore default.
            webView.DefaultBackgroundColor = Color.White;

            if (!e.IsSuccess)
            {
                e.WebErrorStatus.ToString().SendError(SendToParent, "WebView navigation failed");
                return;
            }

            if (!webView.TryGetCefD3DRenderingSubProcessId(out cefD3DRenderingSubProcessId))
                "Failed to retrieve GetCefD3DRenderingSubProcessId".SendError(SendToParent);

            switch (startArgs.Type)
            {
                case WebPageType.online:
                    switch (WebContentUtil.GetContentType(startArgs.Url).ContentType)
                    {
                        case WebContentType.shadertoy:
                            await webView.TryHideShaderToyGui();
                            break;
                    }
                    break;
                case WebPageType.local:
                    await RestoreLivelyProperties(startArgs.Properties);
                    break;
            }
            SendToParent(new LivelyMessageWallpaperLoaded() { Success = e.IsSuccess });

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
                            ex.SendError(SendToParent, "Error sending track information");

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
                    visualizerService.Start(startArgs.AudioVisualizerDeviceId);
                }
            }
        }

        /// <summary>
        /// Resumes the suspended CEF Direct3D rendering subprocess by detaching the debugger.
        /// Must be called on the same thread that previously attached, since debugger state is thread-specific.
        /// </summary>
        private void ResumeRenderingSubProcess()
        {
            if (cefD3DRenderingSubProcessId == 0)
                return;

            _ = NativeMethods.DebugActiveProcessStop((uint)cefD3DRenderingSubProcessId);
        }

        /// <summary>
        /// Suspends the CEF Direct3D rendering subprocess by attaching a debugger.
        /// Must be called on the same thread to ensure proper detachment later.
        /// </summary>
        private void SuspendRenderingSubProcess()
        {
            // The "System Idle Process" is given process ID 0, Kernel is 1.
            if (cefD3DRenderingSubProcessId == 0)
                return;

            // DebugSetProcessKillOnExit by default is TRUE.
            // Ref: https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-debugsetprocesskillonexit
            _ = NativeMethods.DebugActiveProcess((uint)cefD3DRenderingSubProcessId);
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
                                var obj = JsonConvert.DeserializeObject<IpcMessage>(text, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                                if (obj.Type == MessageType.cmd_close)
                                    break;

                                // Ensure all processing is on the same thread.
                                // Some WebView2 calls require UI thread.
                                this.Invoke((Action)(async () =>
                                {
                                    try
                                    {
                                        await ProcessMessage(obj);
                                    }
                                    catch (Exception ie1)
                                    {
                                        ie1.SendError(SendToParent);
                                    }
                                }));
                            }
                            catch (Exception ie2)
                            {
                                ie2.SendError(SendToParent);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                ex.SendError(SendToParent);
            }
            finally
            {
                reader?.Dispose();
                this.Invoke((Action)Application.Exit);
            }
        }

        private async Task ProcessMessage(IpcMessage obj)
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
                    await HandleSuspend();
                    break;
                case MessageType.cmd_resume:
                    await HandleResume();
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
                        ie.SendError(SendToParent, "Failed to capture screenshot");
                    }
                    finally
                    {
                        SendToParent(new LivelyMessageScreenshot()
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
                    // Handle on caller
                    break;
            }
        }

        private async Task HandleSuspend()
        {
            SuspendRenderingSubProcess();
            if (!isPaused)
            {
                if (startArgs.PauseWebMedia || isVideoStream)
                    await webView.TryPauseMedia();

                if (startArgs.PauseEvent)
                    await webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged",
                        JsonConvert.SerializeObject(new WallpaperPlaybackState() { IsPaused = true }),
                        Formatting.Indented);
            }
            isPaused = true;
            MemoryUtil.OptimizeMemory();
        }

        private async Task HandleResume()
        {
            ResumeRenderingSubProcess();
            if (isPaused)
            {
                if (startArgs.PauseWebMedia || isVideoStream)
                    await webView.TryPlayMedia();

                if (startArgs.PauseEvent)
                    await webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged",
                            JsonConvert.SerializeObject(new WallpaperPlaybackState() { IsPaused = false }),
                            Formatting.Indented);

                if (startArgs.NowPlaying)
                    await webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(nowPlayingService?.CurrentTrack, Formatting.Indented));
            }
            isPaused = false;
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
                ex.SendError(SendToParent);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            visualizerService?.Dispose();
            hardwareUsageService?.Stop();
            nowPlayingService?.Stop();
            webView?.Dispose();
        }

        private void SendToParent(IpcMessage obj)
        {
            if (!IsDebugging)
                Console.WriteLine(JsonConvert.SerializeObject(obj));

            Debug.WriteLine(JsonConvert.SerializeObject(obj));
        }

        private sealed class WallpaperPlaybackState
        {
            public bool IsPaused { get; set; }
        }
    }
}
