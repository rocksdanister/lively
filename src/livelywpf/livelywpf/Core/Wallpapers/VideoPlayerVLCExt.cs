using livelywpf.Core.API;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using livelywpf.Models;
using livelywpf.Helpers.Shell;

namespace livelywpf.Core.Wallpapers
{
    /// <summary>
    /// libVLC videoplayer (External plugin.)
    /// </summary>
    public class VideoPlayerVlcExt : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _initialized;

        public bool IsLoaded => Handle != IntPtr.Zero;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public ILibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc { get; }

        public ILivelyScreen Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public VideoPlayerVlcExt(string path, ILibraryModel model, ILivelyScreen display)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = "\"" + path + "\"",
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer", "libVLCPlayer.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer")
            };

            Process _process = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true
            };

            this.Proc = _process;
            this.Model = model;
            this.Screen = display;
        }

        public void Close()
        {
            try
            {
                Proc.Refresh();
                Proc.StandardInput.WriteLine("lively:terminate");
            }
            catch
            {
                Terminate();
            }
        }

        public void Pause()
        {
            SendMessage("lively:vid-pause");
        }

        public void Play()
        {
            SendMessage("lively:vid-play");
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
            if (!String.IsNullOrEmpty(e.Data))
            {
                if (e.Data.Contains("HWND"))
                {
                    bool status = true;
                    Exception error = null;
                    string msg = null;
                    try
                    {
                        msg = e.Data;
                        IntPtr handle = new IntPtr();
                        handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
                        if (IntPtr.Equals(handle, IntPtr.Zero))//unlikely.
                        {
                            status = false;
                        }
                        Handle = handle;
                    }
                    catch (Exception ex)
                    {
                        status = false;
                        error = ex;
                    }
                    finally
                    {
                        if (!_initialized)
                        {
                            WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
                        }
                        //First run sent msg will be window handle.
                        _initialized = true;
                    }
                }
                Logger.Info("libVLC(Ext):" + e.Data);
            }
        }

        public void Stop()
        {
            //throw new NotImplementedException();
        }

        private void SendMessage(string msg)
        {
            if (Proc != null)
            {
                try
                {
                    Proc.StandardInput.WriteLine(msg);
                }
                catch { }
            }
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Screen = display;
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

        public void SetVolume(int volume)
        {
            SendMessage("lively:vid-volume " + volume);
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
            throw new NotImplementedException();
        }

        public void SendMessage(IpcMessage obj)
        {
            //todo
        }
    }
}
