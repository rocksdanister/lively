using Lively.Common;
using Lively.Common.Exceptions;
using Lively.Common.Extensions;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.JsonConverters;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    public class WebCefSharpProcess : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly TaskCompletionSource<Exception> tcsProcessWait = new();
        private readonly TaskCompletionSource contentReadyTcs = new();
        private bool IsContentReady => contentReadyTcs.Task.IsCompleted;
        private readonly Process process;
        private int cefD3DRenderingSubProcessPid;//, cefAudioSubProcessPid;
        private bool isInitialized;
        private static int globalCount;
        private readonly int uniqueId;

        public event EventHandler Exited;
        public event EventHandler Loaded;

        public bool IsLoaded { get; private set; } = false;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle { get; private set; }

        public int? Pid { get; private set; } = null;

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath { get; }

        public bool IsExited { get; private set; }

        public WebCefSharpProcess(string path,
            LibraryModel model,
            DisplayMonitor display,
            string livelyPropertyPath,
            string debugPort,
            bool diskCache,
            AppTheme theme,
            int volume,
            string audioVisualizerId = null)
        {
            //Streams can also use browser.
            var isWeb = model.LivelyInfo.Type == WallpaperType.url || model.LivelyInfo.Type == WallpaperType.web || model.LivelyInfo.Type == WallpaperType.webaudio;
            LivelyPropertyCopyPath = isWeb ? livelyPropertyPath : null;

            var cmdArgs = new StringBuilder();
            cmdArgs.Append(" --wallpaper-url " + "\"" + path + "\"");
            cmdArgs.Append(" --wallpaper-color-scheme " + theme + " ");
            cmdArgs.Append(" --wallpaper-display " + "\"" + display.DeviceId + "\"");
            cmdArgs.Append(" --wallpaper-property " + "\"" + LivelyPropertyCopyPath + "\"");
            cmdArgs.Append(" --wallpaper-volume " + volume);
            cmdArgs.Append(" --wallpaper-geometry " + display.Bounds.Width + "x" + display.Bounds.Height);
            //--audio false Issue: https://github.com/commandlineparser/commandline/issues/702
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.webaudio ? " --wallpaper-audio true" : " ");
            cmdArgs.Append(!string.IsNullOrEmpty(audioVisualizerId) ? " --wallpaper-audio-id " + audioVisualizerId : " ");
            cmdArgs.Append(!string.IsNullOrWhiteSpace(debugPort) ? " --wallpaper-debug " + debugPort : " ");
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.url || model.LivelyInfo.Type == WallpaperType.videostream ? " --wallpaper-type online" : " --wallpaper-type local");
            cmdArgs.Append(diskCache && model.LivelyInfo.Type == WallpaperType.url ? " --wallpaper-cache " + "\"" + Path.Combine(Constants.CommonPaths.TempCefDir, "Lively.PlayerCefSharp", display.Index.ToString()) + "\"" : " ");
            if (TryParseUserCommandArgs(model.LivelyInfo.Arguments, out string parsedArgs))
                cmdArgs.Append(" " + parsedArgs);

            this.process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    Arguments = cmdArgs.ToString(),
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.CefSharpPath),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    StandardInputEncoding = Encoding.UTF8,
                    //StandardOutputEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.CefSharpDir)
                },
            };
            this.Model = model;
            this.Screen = display;

            //for logging purpose
            uniqueId = globalCount++;
        }
        
        public void Pause()
        {
            // The "System Idle Process" is given process ID 0, Kernel is 1.
            if (!IsContentReady || cefD3DRenderingSubProcessPid == 0)
                return;

            // Cef spawns multiple subprocess but "Intermediate D3D Window" seems to do the trick..
            _ = NativeMethods.DebugActiveProcess((uint)cefD3DRenderingSubProcessPid);
            SendMessage(new LivelySuspendCmd()); //"{\"Type\":7}"
        }

        public void Play()
        {
            if (!IsContentReady || cefD3DRenderingSubProcessPid == 0)
                return;

            _ = NativeMethods.DebugActiveProcessStop((uint)cefD3DRenderingSubProcessPid);
            SendMessage(new LivelyResumeCmd()); //"{\"Type\":8}"
        }

        public async Task ShowAsync()
        {
            if (process is null)
                return;

            try
            {
                process.Exited += Proc_Exited;
                process.OutputDataReceived += Proc_OutputDataReceived;
                process.Start();
                Pid = process.Id;
                process.BeginOutputReadLine();

                await tcsProcessWait.Task;
                if (tcsProcessWait.Task.Result is not null)
                    throw tcsProcessWait.Task.Result;
            }
            catch (Exception)
            {
                Terminate();

                throw;
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Logger.Info($"Cef{uniqueId}: Process exited with exit code: {process?.ExitCode}");
            if (!isInitialized)
            {
                // 87 = ERROR_INVALID_PARAMETER
                // Ref: <https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499->
                if (process is not null && process.ExitCode == 87)
                    tcsProcessWait.TrySetResult(new WallpaperPluginException("Error initializing. Unknown options are passed."));
                else
                    tcsProcessWait.TrySetResult(new InvalidOperationException(Properties.Resources.LivelyExceptionGeneral));
            }
            process.OutputDataReceived -= Proc_OutputDataReceived;
            process?.Dispose();
            IsExited = true;
            Exited?.Invoke(this, EventArgs.Empty);
        }

        private async void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!string.IsNullOrEmpty(e.Data))
            {
                IpcMessage obj = null;
                try
                {
                    obj = JsonConvert.DeserializeObject<IpcMessage>(e.Data, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                }
                catch (Exception ex)
                {
                    Logger.Error($"Cef{uniqueId}: Ipc parse error: {e.Data}.\n\nException: {ex.Message}");
                }

                if (obj is null)
                    return;

                // Log message
                switch (obj.Type)
                {
                    case MessageType.msg_console:
                        var msg = obj as LivelyMessageConsole;
                        switch (msg.Category)
                        {
                            case ConsoleMessageType.log:
                                Logger.Info($"Cef{uniqueId}: {msg.Message}");
                                break;
                            case ConsoleMessageType.error:
                                Logger.Error($"Cef{uniqueId}: {msg.Message}");
                                break;
                            case ConsoleMessageType.console:
                                Logger.Info($"Cef{uniqueId}: {msg.Message}");
                                break;
                        }
                        break;
                    default:
                        Logger.Info($"Cef{uniqueId}: {e.Data}");
                        break;
                }

                // Process message
                switch (obj.Type)
                {
                    case MessageType.msg_hwnd:
                        if (!isInitialized)
                        {
                            Exception error = null;
                            try
                            {
                                //CefBrowserWindow
                                var handle = new IntPtr(((LivelyMessageHwnd)obj).Hwnd);
                                //WindowsForms10.Window.8.app.0.141b42a_r9_ad1
                                InputHandle = NativeMethods.FindWindowEx(handle, IntPtr.Zero, "Chrome_WidgetWin_1", null);
                                Handle = process.GetProcessWindow(true);//FindWindowByProcessId(Proc.Id);

                                if (IntPtr.Equals(InputHandle, IntPtr.Zero) || IntPtr.Equals(Handle, IntPtr.Zero))
                                {
                                    throw new Exception("Browser input/window handle NULL.");
                                }

                                // We are doing this player side.
                                // WindowUtil.RemoveWindowFromTaskbar(Handle);
                            }
                            catch (Exception ie)
                            {
                                error = ie;
                            }
                            finally
                            {
                                isInitialized = true;
                                tcsProcessWait.TrySetResult(error);
                            }
                        }
                        break;
                    case MessageType.msg_wploaded:
                        if (!IsLoaded)
                        {
                            // Takes time for rendering window to spawn, CefSharp ChromeBrowser.LoadingStateChanged.
                            _ = NativeMethods.GetWindowThreadProcessId(NativeMethods.FindWindowEx(InputHandle, IntPtr.Zero, "Intermediate D3D Window", null), out cefD3DRenderingSubProcessPid);
                            IsLoaded = true;
                            Loaded?.Invoke(this, EventArgs.Empty);

                            // Wait before pausing or other internal fn since some pages can have transition.
                            await Task.Delay(1000);
                            if (!IsExited)
                                contentReadyTcs.TrySetResult();
                            else
                                contentReadyTcs.TrySetException(new InvalidOperationException("Process exited."));
                        }
                        break;
                }
            }
        }

        private void SendMessage(string msg)
        {
            if (IsExited)
                return;

            try
            {
                // Setting process StandardInputEncoding to UTF8.
                process?.StandardInput.WriteLine(msg);
                // Or convert message to UTF8.
                //byte[] bytes = Encoding.UTF8.GetBytes(msg);
                //Proc.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                Logger.Error($"Stdin write fail: {e.Message}");
            }
        }

        public void SendMessage(IpcMessage obj)
        {
            SendMessage(JsonConvert.SerializeObject(obj));
        }

        public void Terminate()
        {
            if (IsExited)
                return;

            try
            {
                process.Kill();
            }
            catch { }
        }

        public void Close()
        {
            if (IsExited)
                return;

            //Issue: Cef.shutdown() crashing when multiple instance is closed simulataneously.
            Terminate();
            /*
            try
            {
                Proc.Refresh();
                Proc.StandardInput.WriteLine("lively:terminate");
                //TODO: Make it Async function.
                if (!Proc.WaitForExit(4000))
                {
                    Terminate();
                }
            }
            catch
            {
                Terminate();
            }
            */
        }

        public void SetVolume(int volume)
        {
            SendMessage(new LivelyVolumeCmd() { Volume = volume });
        }

        public void SetMute(bool mute)
        {
            //todo
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
            await WaitForContentReadyAsync(TimeSpan.FromSeconds(5));

            var tcs = new TaskCompletionSource();
            void LocalOutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    tcs.TrySetException(new InvalidOperationException("Process exited unexpectedly."));
                }
                else
                {
                    var obj = JsonConvert.DeserializeObject<IpcMessage>(e.Data, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                    if (obj.Type == MessageType.msg_screenshot)
                    {
                        var msg = (LivelyMessageScreenshot)obj;
                        if (msg.FileName == Path.GetFileName(filePath))
                        {
                            process.OutputDataReceived -= LocalOutputDataReceived;
                            if (msg.Success)
                                tcs.TrySetResult();
                            else
                                tcs.TrySetException(new InvalidOperationException($"Failed to take screenshot."));
                        }
                    }
                }
            }
            process.OutputDataReceived += LocalOutputDataReceived;

            Logger.Info($"Cef{uniqueId}: Taking screenshot: {filePath}");
            SendMessage(new LivelyScreenshotCmd()
            {
                FilePath = Path.GetExtension(filePath) != ".jpg" ? filePath + ".jpg" : filePath,
                Format = ScreenshotFormat.jpeg,
                Delay = 0 //unused
            });

            // Timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using (cts.Token.Register(() =>
            {
                if (!IsExited)
                    process.OutputDataReceived -= LocalOutputDataReceived;

                tcs.TrySetException(new TimeoutException($"Screenshot timed out."));
            }))

            await tcs.Task;
        }

        public void Dispose()
        {
            // Process object is disposed in Exit event.
            Terminate();
        }

        private async Task WaitForContentReadyAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            await contentReadyTcs.Task.WaitAsync(cts.Token);
        }

        /// <summary>
        /// Backward compatibility, appends --wallpaper to arguments if required.
        /// </summary>
        private static bool TryParseUserCommandArgs(string args, out string result)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                result = null;
                return false;
            }

            var words = args.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].StartsWith("--"))
                {
                    words[i] = string.Concat("--wallpaper-", words[i].AsSpan(2));
                }
            }
            result = string.Join(" ", words);
            return true;
        }
    }
}