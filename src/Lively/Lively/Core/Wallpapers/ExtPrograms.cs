using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Shell;
using Lively.Core.Suspend;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    public class ExtPrograms : IWallpaper
    {
        public UInt32 SuspendCnt { get; set; }

        public bool IsLoaded => Handle != IntPtr.Zero;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => Handle;

        public Process Proc { get; }

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public bool IsExited { get; private set; }

        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task<IntPtr> processWaitTask;
        private readonly int timeOut;

        /// <summary>
        /// Launch Program(.exe) Unity, godot.. as wallpaper.
        /// </summary>
        /// <param name="path">Path to program exe</param>
        /// <param name="model">Wallpaper data</param>
        /// <param name="display">Screen metadata</param>
        /// <param name="timeOut">Time to wait for program to be ready(in milliseconds)</param>
        public ExtPrograms(string path, LibraryModel model, DisplayMonitor display, int timeOut = 20000)
        {
            // Unity flags
            //-popupwindow removes from taskbar
            //-fullscreen disable fullscreen mode if set during compilation (lively is handling resizing window instead).
            //Alternative flags:
            //Unity attaches to workerw by itself; Problem: Process window handle is returning zero.
            //"-parentHWND " + workerw.ToString();// + " -popupwindow" + " -;
            //cmdArgs = "-popupwindow -screen-fullscreen 0";
            string cmdArgs = model.LivelyInfo.Arguments;

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                WorkingDirectory = System.IO.Path.GetDirectoryName(path),
                Arguments = cmdArgs,
            };

            Process _process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = start,
            };

            this.Proc = _process;
            this.Model = model;
            this.Screen = display;
            this.timeOut = timeOut;
            SuspendCnt = 0;
        }

        public async void Close()
        {
            ctsProcessWait.TaskWaitCancel();
            while(!processWaitTask.IsTaskWaitCompleted())
                await Task.Delay(1);

            //Not reliable, app may refuse to close(open dialogue window.. etc)
            //Proc.CloseMainWindow();
            Terminate();
        }

        public void Pause()
        {
            if (Proc != null)
            {
                //method 0, issue: does not work with every pgm
                //NativeMethods.DebugActiveProcess((uint)Proc.Id);

                //method 1, issue: resume taking time ?!
                //NativeMethods.NtSuspendProcess(Proc.Handle);

                //method 2, issue: deadlock/thread issue
                /*
                try
                {
                    ProcessSuspend.SuspendAllThreads(this);
                    //thread buggy noise otherwise?!
                    VolumeMixer.SetApplicationMute(Proc.Id, true);
                }
                catch { }
                */
            }
        }

        public void Play()
        {
            if (Proc != null)
            {
                //method 0, issue: does not work with every pgm
                //NativeMethods.DebugActiveProcessStop((uint)Proc.Id);

                //method 1, issue: resume taking time ?!
                //NativeMethods.NtResumeProcess(Proc.Handle);

                //method 2, issue: deadlock/thread issue
                /*
                try
                {
                    ProcessSuspend.ResumeAllThreads(this);
                    //thread buggy noise otherwise?!
                    VolumeMixer.SetApplicationMute(Proc.Id, false);
                }
                catch { }
                */
            }
        }

        public void Stop()
        {
            Pause();
        }

        public async Task ShowAsync()
        {
            if (Proc is null)
                return;

            try
            {
                Proc.Exited += Proc_Exited;
                Proc.Start();
                processWaitTask = Proc.WaitForProcessOrGameWindow(Category, timeOut, ctsProcessWait.Token, true);
                this.Handle = await processWaitTask;
                if (Handle.Equals(IntPtr.Zero))
                {
                    throw new InvalidOperationException(Properties.Resources.LivelyExceptionGeneral);
                }
                else
                {
                    //Program ready!
                    //TaskView crash fix
                    WindowUtil.BorderlessWinStyle(Handle);
                    WindowUtil.RemoveWindowFromTaskbar(Handle);
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
            try
            {
                VolumeMixer.SetApplicationVolume(Proc.Id, volume);
            }
            catch { }
        }

        public void SetMute(bool mute)
        {
            //todo
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //todo
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
