using Google.Protobuf.WellKnownTypes;
using Lively.Common;
using Lively.Common.Exceptions;
using Lively.Common.JsonConverters;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    public class VideoLibVlcPlayer : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly TaskCompletionSource<Exception> tcsProcessWait = new();
        private readonly TaskCompletionSource contentReadyTcs = new();
        private bool IsContentReady => contentReadyTcs.Task.IsCompleted;
        private readonly Process process;
        private static int globalCount;
        private readonly int uniqueId;
        private int currentVolume = 0;
        private bool isInitialized;
        private bool isMuted;

        public event EventHandler Exited;
        public event EventHandler Loaded;

        public bool IsLoaded { get; private set; } = false;

        public bool IsExited { get; private set; }

        public WallpaperType Category => Model.LivelyInfo.Type;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle { get; private set; }

        public int? Pid { get; private set; } = null;

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath { get; }

        public VideoLibVlcPlayer(string path,
            LibraryModel model,
            DisplayMonitor display,
            string livelyPropertyPath,
            AppTheme theme,
            int volume,
            bool isHwAccel = true)
        {
            LivelyPropertyCopyPath = livelyPropertyPath;

            StringBuilder cmdArgs = new();
            cmdArgs.Append(" --wallpaper-path " + "\"" + path + "\"");
            cmdArgs.Append(" --wallpaper-property " + "\"" + LivelyPropertyCopyPath + "\"");
            cmdArgs.Append(isHwAccel ? " --wallpaper-hardware-decoding true" :  " ");
            cmdArgs.Append(" --wallpaper-color-scheme " + theme + " ");
            cmdArgs.Append(" --wallpaper-geometry " + display.Bounds.Width + "x" + display.Bounds.Height);
            cmdArgs.Append(" --wallpaper-volume 0");

            this.process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    Arguments = cmdArgs.ToString(),
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.LibVlcPath),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    StandardInputEncoding = Encoding.UTF8,
                    //StandardOutputEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.LibVlcDir)
                },
            };
            this.Model = model;
            this.Screen = display;

            //for logging purpose
            uniqueId = globalCount++;
        }

        public void Close()
        {
            if (IsExited)
                return;

            SendMessage(new LivelyCloseCmd());
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

        public void Play()
        {
            SendMessage(new LivelyResumeCmd());
        }

        public void Pause()
        {
            SendMessage(new LivelySuspendCmd());
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
            Logger.Info($"libVlc{uniqueId}: Process exited with exit code: {process?.ExitCode}");
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
                    Logger.Error($"libVlc{uniqueId}: Ipc parse error: {e.Data}.\n\nException: {ex.Message}");
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
                                Logger.Info($"libVlc{uniqueId}: {msg.Message}");
                                break;
                            case ConsoleMessageType.error:
                                Logger.Error($"libVlc{uniqueId}: {msg.Message}");
                                break;
                            case ConsoleMessageType.console:
                                Logger.Info($"libVlc{uniqueId}: {msg.Message}");
                                break;
                        }
                        break;
                    default:
                        Logger.Info($"libVlc{uniqueId}: {e.Data}");
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
                                Handle = new IntPtr(((LivelyMessageHwnd)obj).Hwnd);
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

            Logger.Info($"libVlc{uniqueId}: Taking screenshot: {filePath}");
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

        public void SetVolume(int volume)
        {
            currentVolume = volume;

            if (!isMuted)
                SendMessage(new LivelyVolumeCmd() { Volume = volume });
        }

        public void SetMute(bool mute)
        {
            // We use mute as part of LivelyProperties, so workaround.
            isMuted = mute;
            if (isMuted)
                SendMessage(new LivelyVolumeCmd() { Volume = 0 });
            else
                SendMessage(new LivelyVolumeCmd { Volume = currentVolume });
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            if (pos == 0 && type != PlaybackPosType.relativePercent)
                SendMessage(new LivelyReloadCmd());
        }

        private async Task WaitForContentReadyAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            await contentReadyTcs.Task.WaitAsync(cts.Token);
        }

        public void Dispose()
        {
            // Process object is disposed in Exit event.
            Terminate();
        }
    }
}
