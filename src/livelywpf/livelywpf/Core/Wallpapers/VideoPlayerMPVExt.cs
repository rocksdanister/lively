using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace livelywpf.Core
{
    /// <summary>
    /// libMPV videoplayer (External plugin.)
    /// </summary>
    public class VideoPlayerMPVExt : IWallpaper
    {
        public VideoPlayerMPVExt(string path, LibraryModel model, LivelyScreen display, 
            StreamQualitySuggestion streamQuality = StreamQualitySuggestion.Highest)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                //Arguments = "\"" + path + "\"",
                Arguments = "--path " + "\"" + path + "\"" + " --stream " + (int)streamQuality,
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "libMPVPlayer.exe"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer")
            };

            Process videoPlayerProc = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true
            };
            //webProcess.OutputDataReceived += WebProcess_OutputDataReceived;

            this.Proc = videoPlayerProc;
            this.Model = model;
            this.Display = display;
        }
        IntPtr HWND { get; set; }
        Process Proc { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        /// <summary>
        /// copy of LivelyProperties.json file used to modify for current running screen.
        /// </summary>
        //string LivelyPropertyCopy { get; set; }
        private bool Initialized { get; set; }

        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        public void Close()
        {
            try
            {
                Proc.Refresh();
                Proc.StandardInput.WriteLine("lively:terminate");
                Proc.OutputDataReceived -= Proc_OutputDataReceived;
            }
            catch
            {
                Terminate();
            }
        }

        public IntPtr GetHWND()
        {
            return HWND;
        }

        public Process GetProcess()
        {
            return Proc;
        }

        public LivelyScreen GetScreen()
        {
            return Display;
        }

        public LibraryModel GetWallpaperData()
        {
            return Model;
        }

        public WallpaperType GetWallpaperType()
        {
            return Model.LivelyInfo.Type;
        }

        public void Pause()
        {
            SendMessage("lively:vid-pause");
        }

        public void Play()
        {
            SendMessage("lively:vid-play");
        }

        public void SetHWND(IntPtr hwnd)
        {
            this.HWND = hwnd;
        }

        public void Stop()
        {
            
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
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e, Msg = null });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            if (!Initialized)
            {
                //Exited with no error and without even firing OutputDataReceived; probably some external factor.
                WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = null, Msg = "Pgm exited early!" });
            }
            Proc.Close();
            SetupDesktop.RefreshDesktop();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            bool status = true;
            Exception error = null;
            string msg = null;
            try
            {
                IntPtr handle = new IntPtr();
                //Retrieves the windowhandle of cefsubprocess, cefsharp is launching cef as a separate proces..
                //If you add the full pgm as child of workerw then there are problems (prob related sharing input queue)
                //Instead hiding the pgm window & adding cefrender window instead.
                msg = "libMPVPlayer Handle:" + e.Data;
                if (e.Data.Contains("HWND"))
                {
                    handle = new IntPtr(Convert.ToInt32(e.Data.Substring(4), 10));
                    if (IntPtr.Equals(handle, IntPtr.Zero))//unlikely.
                    {
                        status = false;
                    }
                    SetHWND(handle);
                }
            }
            catch (Exception ex)
            {
                status = false;
                error = ex;
            }
            Initialized = true;
            WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = status, Error = error, Msg = msg });
        }

        public void SendMessage(string msg)
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

        public string GetLivelyPropertyCopyPath()
        {
            return null;
        }

        public void SetScreen(LivelyScreen display)
        {
            this.Display = display;
        }

        public void Terminate()
        {
            try
            {
                Proc.Kill();
                Proc.Close();
            }
            catch { }
            SetupDesktop.RefreshDesktop();
        }

        public void Resume()
        {

        }

        public void SetVolume(int volume)
        {
            SendMessage("lively:vid-volume " + volume);
        }
    }
}
