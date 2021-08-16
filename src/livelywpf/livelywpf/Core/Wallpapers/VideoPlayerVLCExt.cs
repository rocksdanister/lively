using livelywpf.Core.API;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace livelywpf.Core
{
    /// <summary>
    /// libVLC videoplayer (External plugin.)
    /// </summary>
    public class VideoPlayerVLCExt : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IntPtr hwnd;
        private readonly Process _process;
        private readonly LibraryModel model;
        private LivelyScreen display;
        private bool _initialized;
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public VideoPlayerVLCExt(string path, LibraryModel model, LivelyScreen display)
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

            this._process = _process;
            this.model = model;
            this.display = display;
        }

        public void Close()
        {
            try
            {
                _process.Refresh();
                _process.StandardInput.WriteLine("lively:terminate");
            }
            catch
            {
                Terminate();
            }
        }

        public IntPtr GetHWND()
        {
            return hwnd;
        }

        public IntPtr GetHWNDInput()
        {
            return IntPtr.Zero;
        }

        public Process GetProcess()
        {
            return _process;
        }

        public LivelyScreen GetScreen()
        {
            return display;
        }

        public LibraryModel GetWallpaperData()
        {
            return model;
        }

        public WallpaperType GetWallpaperType()
        {
            return model.LivelyInfo.Type;
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
            if (_process != null)
            {
                try
                {
                    _process.Exited += Proc_Exited;
                    _process.OutputDataReceived += Proc_OutputDataReceived;
                    _process.Start();
                    _process.BeginOutputReadLine();
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
            _process.OutputDataReceived -= Proc_OutputDataReceived;
            _process?.Dispose();
            SetupDesktop.RefreshDesktop();
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
                        hwnd = handle;
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

        public void SendMessage(string msg)
        {
            if (_process != null)
            {
                try
                {
                    _process.StandardInput.WriteLine(msg);
                }
                catch { }
            }
        }

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.display = display;
        }

        public void Terminate()
        {
            try
            {
                _process.Kill();
            }
            catch { }
            SetupDesktop.RefreshDesktop();
        }

        public void SetVolume(int volume)
        {
            SendMessage("lively:vid-volume " + volume);
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

        public bool IsLoaded()
        {
            return GetHWND() != IntPtr.Zero;
        }
    }
}
