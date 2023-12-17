﻿using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Helpers.Shell;
using Lively.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using Lively.Common.Extensions;

namespace Lively.Core.Wallpapers
{
    public class WebWebView2 : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly TaskCompletionSource<Exception> tcsProcessWait = new();
        private int cefD3DRenderingSubProcessPid;
        private static int globalCount;
        private readonly int uniqueId;
        private bool isInitialized;

        public bool IsLoaded { get; private set; } = false;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle { get; private set; }

        public Process Proc { get; }

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath { get; }

        public bool IsExited { get; private set; }

        public WebWebView2(string path,
            LibraryModel model,
            DisplayMonitor display,
            string livelyPropertyPath)
        {
            LivelyPropertyCopyPath = livelyPropertyPath;

            StringBuilder cmdArgs = new StringBuilder();
            cmdArgs.Append(" --url " + "\"" + path + "\"");
            cmdArgs.Append(" --display " + "\"" + display.DeviceId + "\"");
            cmdArgs.Append(" --property " + "\"" + LivelyPropertyCopyPath + "\"");
            cmdArgs.Append(" --volume " + 100);
            cmdArgs.Append(" --geometry " + display.Bounds.Width + "x" + display.Bounds.Height);
            //--audio false Issue: https://github.com/commandlineparser/commandline/issues/702
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.webaudio ? " --audio true" : " ");
            cmdArgs.Append(!string.IsNullOrWhiteSpace(model.LivelyInfo.Arguments) ? " " + model.LivelyInfo.Arguments : " ");
            //cmdArgs.Append(!string.IsNullOrWhiteSpace(debugPort) ? " --debug " + debugPort : " ");
            cmdArgs.Append(model.LivelyInfo.Type == WallpaperType.url || model.LivelyInfo.Type == WallpaperType.videostream ? " --type online" : " --type local");
            //cmdArgs.Append(diskCache && model.LivelyInfo.Type == WallpaperType.url ? " --cache " + "\"" + Path.Combine(Constants.CommonPaths.TempCefDir, "cache", display.DeviceNumber) + "\"" : " ");
#if DEBUG
            //cmdArgs.Append(" --verbose-log true");
#endif

            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = cmdArgs.ToString(),
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "wv2", "Lively.PlayerWebView2.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "wv2")
            };

            Process webProcess = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true
            };

            this.Proc = webProcess;
            this.Model = model;
            this.Screen = display;

            //for logging purpose
            uniqueId = globalCount++;
        }

        public void Close()
        {
            SendMessage(new LivelyCloseCmd());
        }

        public void Pause()
        {
            if (cefD3DRenderingSubProcessPid != 0)
            {
                //Cef spawns multiple subprocess but "Intermediate D3D Window" seems to do the trick..
                //The "System Idle Process" is given process ID 0, Kernel is 1.
                _ = NativeMethods.DebugActiveProcess((uint)cefD3DRenderingSubProcessPid);
                SendMessage(new LivelySuspendCmd()); //"{\"Type\":7}"
            }
        }

        public void Play()
        {
            if (cefD3DRenderingSubProcessPid != 0)
            {
                _ = NativeMethods.DebugActiveProcessStop((uint)cefD3DRenderingSubProcessPid);
                SendMessage(new LivelyResumeCmd()); //"{\"Type\":8}"
            }
        }

        private void SendMessage(string msg)
        {
            try
            {
                Proc?.StandardInput.WriteLine(msg);
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
            //todo
        }

        public void SetMute(bool mute)
        {
            //todo
        }

        public async Task ShowAsync()
        {
            if (Proc is null)
                return;

            try
            {
                Proc.Exited += Proc_Exited;
                Proc.OutputDataReceived += Proc_OutputDataReceived;
                Proc.Start();
                Proc.BeginOutputReadLine();

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
            if (!isInitialized)
            {
                //Exited with no error and without even firing OutputDataReceived; probably some external factor.
                tcsProcessWait.TrySetResult(new InvalidOperationException(Properties.Resources.LivelyExceptionGeneral));
            }
            Proc.OutputDataReceived -= Proc_OutputDataReceived;
            Proc?.Dispose();
            DesktopUtil.RefreshDesktop();
            IsExited = true;
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.Info($"Wv2{uniqueId}: {e.Data}");
                if (!isInitialized || !IsLoaded)
                {
                    IpcMessage obj;
                    try
                    {
                        obj = JsonConvert.DeserializeObject<IpcMessage>(e.Data, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Ipcmessage parse error: {ex.Message}");
                        return;
                    }

                    if (obj.Type == MessageType.msg_hwnd)
                    {
                        Exception error = null;
                        try
                        {
                            //CefBrowserWindow
                            var handle = new IntPtr(((LivelyMessageHwnd)obj).Hwnd);
                            //WindowsForms10.Window.8.app.0.141b42a_r9_ad1
                            var chrome_WidgetWin_0 = NativeMethods.FindWindowEx(handle, IntPtr.Zero, "Chrome_WidgetWin_0", null);
                            if (!chrome_WidgetWin_0.Equals(IntPtr.Zero))
                            {
                                this.InputHandle = NativeMethods.FindWindowEx(chrome_WidgetWin_0, IntPtr.Zero, "Chrome_WidgetWin_1", null);
                            }
                            Handle = Proc.GetProcessWindow(true);

                            if (IntPtr.Equals(Handle, IntPtr.Zero) || IntPtr.Equals(InputHandle, IntPtr.Zero))
                            {
                                throw new Exception("Browser input/window handle NULL.");
                            }

                            //TaskView crash fix..
                            //WindowOperations.RemoveWindowFromTaskbar(Handle);
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
                    else if (obj.Type == MessageType.msg_wploaded)
                    {
                        //Takes time for rendering window to spawn.. this should be enough.
                        _ = NativeMethods.GetWindowThreadProcessId(NativeMethods.FindWindowEx(InputHandle, IntPtr.Zero, "Intermediate D3D Window", null), out cefD3DRenderingSubProcessPid);
                        IsLoaded = true;
                    }
                }
            }
        }

        public void Stop()
        {
            Pause();
        }

        public void Terminate()
        {
            try
            {
                Proc.Kill();
            }
            catch { }
            DesktopUtil.RefreshDesktop();
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
            //TODO
            //await player?.CaptureScreenshot(Path.GetExtension(filePath) != ".jpg" ? filePath + ".jpg" : filePath, ScreenshotFormat.jpeg);
        }
    }
}
