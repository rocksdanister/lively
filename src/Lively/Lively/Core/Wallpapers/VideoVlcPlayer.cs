using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Shell;
using Lively.Common.Message;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    //Ref: 
    //https://github.com/rocksdanister/lively/discussions/342
    //https://wiki.videolan.org/documentation:modules/rc/
    public class VideoVlcPlayer : IWallpaper
    {
        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task<IntPtr> processWaitTask;
        private readonly int timeOut;

        public bool IsLoaded => Handle != IntPtr.Zero;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc { get; }

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public bool IsExited { get; private set; }

        public VideoVlcPlayer(string path, LibraryModel model, DisplayMonitor display, WallpaperScaler scaler = WallpaperScaler.fill, bool hwAccel = true)
        {
            var scalerArg = scaler switch
            {
                WallpaperScaler.none => "--no-autoscale ",
                WallpaperScaler.fill => "--aspect-ratio=" + display.Bounds.Width + ":" + display.Bounds.Height,
                WallpaperScaler.uniform => "--autoscale",
                WallpaperScaler.uniformFill => "--crop=" + display.Bounds.Width + ":" + display.Bounds.Height,
                _ => "--autoscale",
            };

            StringBuilder cmdArgs = new StringBuilder();
            //--no-video-title.
            cmdArgs.Append("--no-osd ");
            //video stretch algorithm.
            cmdArgs.Append(scalerArg + " ");
            //hide menus and controls.
            cmdArgs.Append("--qt-minimal-view ");
            //do not create system-tray icon.
            cmdArgs.Append("--no-qt-system-tray ");
            //prevent player window resizing to video size.
            cmdArgs.Append("--no-qt-video-autoresize ");
            //allow screensaver.
            cmdArgs.Append("--no-disable-screensaver ");
            //open window at (-9999,0), not working without: --no-embedded-video
            cmdArgs.Append("--video-x=-9999 --video-y=0 ");
            //gpu decode preference.
            cmdArgs.Append(hwAccel ? "--avcodec-hw=any " : "--avcodec-hw=none ");
            //media file path.
            cmdArgs.Append("\"" + path + "\"");

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vlc", "vlc.exe"),
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vlc"),
                Arguments = cmdArgs.ToString(),
            };

            Process _process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = start,
            };

            this.Proc = _process;
            this.Model = model;
            this.Screen = display;
            this.timeOut = 20000;
        }

        public async void Close()
        {
            ctsProcessWait.TaskWaitCancel();
            while (!processWaitTask.IsTaskWaitCompleted())
                await Task.Delay(1);

            //Not reliable, app may refuse to close(open dialogue window.. etc)
            //Proc.CloseMainWindow();
            Terminate();
        }

        public WallpaperType GetWallpaperType()
        {
            return Model.LivelyInfo.Type;
        }

        public void Pause()
        {
            //todo
        }

        public void Play()
        {
            //todo
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //todo
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
                Proc.Start();
                processWaitTask = Proc.WaitForProcesWindow(timeOut, ctsProcessWait.Token, true);
                this.Handle = await processWaitTask;
                await processWaitTask;
                if (Handle.Equals(IntPtr.Zero)) {
                    throw new InvalidOperationException(Properties.Resources.LivelyExceptionGeneral);
                }
                else
                {
                    //Program ready!
                    //TaskView crash fix
                    WindowUtil.BorderlessWinStyle(Handle);
                    WindowUtil.RemoveWindowFromTaskbar(Handle);
                    //todo: Restore livelyproperties.json settings here..
                }
            }
            catch (Exception)
            {
                Terminate();

                throw;
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Proc?.Dispose();
            DesktopUtil.RefreshDesktop();
            IsExited = true;
        }

        public void Stop()
        {
            //todo
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

        public Task ScreenCapture(string filePath)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(IpcMessage obj)
        {
            //todo
        }
    }
}
