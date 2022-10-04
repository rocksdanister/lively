using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using Lively.Models;
using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers.Shell;
using Lively.Helpers;

namespace Lively.Core.Wallpapers
{
    public class VideoWmfProcess : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _initialized;
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        private static int globalCount;
        private readonly int uniqueId;

        public bool IsLoaded { get; private set; } = false;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public ILibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle { get; private set; }

        public Process Proc { get; }

        public IDisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath { get; }

        public VideoWmfProcess(string path,
            ILibraryModel model,
            IDisplayMonitor display,
            int volume,
            WallpaperScaler scaler = WallpaperScaler.fill)
        {
            StringBuilder cmdArgs = new StringBuilder();
            cmdArgs.Append(" --path " + "\"" + path + "\"");
            cmdArgs.Append(" --volume " + volume);
            cmdArgs.Append(" --stretch " + (int)scaler);
#if DEBUG
            cmdArgs.Append(" --verbose-log true");
#endif

            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = cmdArgs.ToString(),
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "wmf", "Lively.PlayerWmf.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "wmf")
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

        public void Pause()
        {
            SendMessage(new LivelySuspendCmd());
        }

        public void Play()
        {
            SendMessage(new LivelyResumeCmd());
        }

        public void Show()
        {
            if (Proc != null)
            {
                try
                {
                    Proc.Exited += Proc_Exited;
                    Proc.OutputDataReceived += Proc_OutputDataReceived;
                    Proc.Start();
                    Proc.BeginOutputReadLine();
                }
                catch (Exception e)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = "Failed to start process." });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                //Exited with no error and without even firing OutputDataReceived; probably some external factor.
                WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                {
                    Success = false,
                    Error = new Exception(Properties.Resources.LivelyExceptionGeneral),
                    Msg = "Process exited before giving HWND."
                });
            }
            Proc.OutputDataReceived -= Proc_OutputDataReceived;
            Proc?.Dispose();
            DesktopUtil.RefreshDesktop();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //When the redirected stream is closed, a null line is sent to the event handler.
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.Info($"Wmf{uniqueId}: {e.Data}");
                if (!_initialized || !IsLoaded)
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
                        bool status = true;
                        Exception error = null;
                        string msg = null;
                        try
                        {
                            msg = e.Data;
                            Handle = new IntPtr(((LivelyMessageHwnd)obj).Hwnd);
                        }
                        catch (Exception ie)
                        {
                            status = false;
                            error = ie;
                        }
                        finally
                        {
                            _initialized = true;
                            WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
                        }
                    }
                    else if (obj.Type == MessageType.msg_wploaded)
                    {
                        IsLoaded = ((LivelyMessageWallpaperLoaded)obj).Success;
                    }
                }
            }
        }

        public void Stop()
        {
            //throw new NotImplementedException();
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

        public void Terminate()
        {
            try
            {
                Proc.Kill();
            }
            catch { }
            DesktopUtil.RefreshDesktop();
        }

        public void Close()
        {
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
    }
}
