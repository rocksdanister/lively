using Lively.Common;
using Lively.Common.JsonConverters;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    public class VideoWmfProcess : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly TaskCompletionSource<Exception> tcsProcessWait = new();
        private bool isInitialized;
        private readonly Process process;
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

        public VideoWmfProcess(string path,
            LibraryModel model,
            DisplayMonitor display,
            int volume,
            WallpaperScaler scaler = WallpaperScaler.fill)
        {
            StringBuilder cmdArgs = new();
            cmdArgs.Append(" --path " + "\"" + path + "\"");
            cmdArgs.Append(" --volume " + volume);
            cmdArgs.Append(" --stretch " + (int)scaler);

            this.process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    Arguments = cmdArgs.ToString(),
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.WmfPath),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.WmfDir)
                },
            };
            this.Model = model;
            this.Screen = display;

            //for logging purpose
            uniqueId = globalCount++;
        }

        public void Pause()
        {
            SendMessage(new LivelySuspendCmd());
        }

        public void Play()
        {
            SendMessage(new LivelyResumeCmd());
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
            if (!isInitialized)
            {
                //Exited with no error and without even firing OutputDataReceived; probably some external factor.
                tcsProcessWait.TrySetResult(new InvalidOperationException(Properties.Resources.LivelyExceptionGeneral));
            }
            process.OutputDataReceived -= Proc_OutputDataReceived;
            process?.Dispose();
            IsExited = true;
            Exited?.Invoke(this, EventArgs.Empty);
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.Info($"Wmf{uniqueId}: {e.Data}");
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
                    else if (obj.Type == MessageType.msg_wploaded)
                    {
                        IsLoaded = ((LivelyMessageWallpaperLoaded)obj).Success;
                        Loaded?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void SendMessage(string msg)
        {
            if (IsExited)
                return;

            try
            {
                process?.StandardInput.WriteLine(msg);
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

            Terminate();
            //SendMessage(new LivelyCloseCmd());
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
            //todo
        }

        public async Task ScreenCapture(string filePath)
        {
            filePath = Path.GetExtension(filePath) != ".jpg" ? filePath + ".jpg" : filePath;
            using var bmp = CaptureScreen.CaptureWindow(Handle);
            bmp.Save(filePath, ImageFormat.Jpeg);
        }

        public void Dispose()
        {
            // Process object is disposed in Exit event.
            Terminate();
        }
    }
}
