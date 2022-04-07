using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Helpers.Shell;
using Lively.Models;
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
    //Ref: 
    //https://github.com/rocksdanister/lively/discussions/342
    //https://wiki.videolan.org/documentation:modules/rc/
    public class VideoVlcPlayer : IWallpaper
    {
        public event EventHandler<WindowInitializedArgs> WindowInitialized;
        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task processWaitTask;
        private readonly int timeOut;

        public bool IsLoaded => Handle != IntPtr.Zero;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public ILibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc { get; }

        public IDisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public VideoVlcPlayer(string path, ILibraryModel model, IDisplayMonitor display, WallpaperScaler scaler = WallpaperScaler.fill, bool hwAccel = true)
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
            TaskProcessWaitCancel();
            while (!IsProcessWaitDone())
            {
                await Task.Delay(1);
            }

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

        public async void Show()
        {
            if (Proc != null)
            {
                try
                {
                    Proc.Exited += Proc_Exited;
                    Proc.Start();
                    processWaitTask = Task.Run((Func<IntPtr>)(() => this.Handle = WaitForProcesWindow().Result), ctsProcessWait.Token);
                    await processWaitTask;
                    if (Handle.Equals(IntPtr.Zero))
                    {
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                        {
                            Success = false,
                            Error = new Exception(Properties.Resources.LivelyExceptionGeneral),
                            Msg = "Process window handle is zero."
                        });
                    }
                    else
                    {
                        WindowOperations.BorderlessWinStyle(Handle);
                        WindowOperations.RemoveWindowFromTaskbar(Handle);
                        //Program ready!
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                        {
                            Success = true,
                            Error = null,
                            Msg = null
                        });
                        //todo: Restore livelyproperties.json settings here..
                    }
                }
                catch (OperationCanceledException e1)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                    {
                        Success = false,
                        Error = e1,
                        Msg = "Program wallpaper terminated early/user cancel."
                    });
                }
                catch (InvalidOperationException e2)
                {
                    //No GUI, program failed to enter idle state.
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                    {
                        Success = false,
                        Error = e2,
                        Msg = "Program wallpaper crashed/closed already!"
                    });
                }
                catch (Exception e3)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs()
                    {
                        Success = false,
                        Error = e3,
                        Msg = ":("
                    });
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Proc?.Dispose();
            DesktopUtil.RefreshDesktop();
        }

        #region process task

        /// <summary>
        /// Function to search for window of spawned program.
        /// </summary>
        private async Task<IntPtr> WaitForProcesWindow()
        {
            if (Proc == null)
            {
                return IntPtr.Zero;
            }

            Proc.Refresh();
            //waiting for program messageloop to be ready (GUI is not guaranteed to be ready.)
            while (Proc.WaitForInputIdle(-1) != true)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
            }

            IntPtr wHWND = IntPtr.Zero;
            //Find process window.
            for (int i = 0; i < timeOut && Proc.HasExited == false; i++)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
                if (!IntPtr.Equals((wHWND = GetProcessWindow(Proc, true)), IntPtr.Zero))
                    break;
                await Task.Delay(1);
            }
            return wHWND;
        }

        /// <summary>
        /// Retrieve window handle of process.
        /// </summary>
        /// <param name="proc">Process to search for.</param>
        /// <param name="win32Search">Use win32 method to find window.</param>
        /// <returns></returns>
        private IntPtr GetProcessWindow(Process proc, bool win32Search = false)
        {
            if (this.Proc == null)
                return IntPtr.Zero;

            if (win32Search)
            {
                return FindWindowByProcessId(proc.Id);
            }
            else
            {
                proc.Refresh();
                //Issue(.net core) MainWindowHandle zero: https://github.com/dotnet/runtime/issues/32690
                return proc.MainWindowHandle;
            }
        }

        private IntPtr FindWindowByProcessId(int pid)
        {
            IntPtr HWND = IntPtr.Zero;
            NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                _ = NativeMethods.GetWindowThreadProcessId(tophandle, out int cur_pid);
                if (cur_pid == pid)
                {
                    if (NativeMethods.IsWindowVisible(tophandle))
                    {
                        HWND = tophandle;
                        return false;
                    }
                }

                return true;
            }), IntPtr.Zero);

            return HWND;
        }

        /// <summary>
        /// Cancel waiting for pgm wp window to be ready.
        /// </summary>
        private void TaskProcessWaitCancel()
        {
            if (ctsProcessWait == null)
                return;

            ctsProcessWait.Cancel();
            ctsProcessWait.Dispose();
        }

        /// <summary>
        /// Check if started pgm ready(GUI window started).
        /// </summary>
        /// <returns>true: process ready/halted, false: process still starting.</returns>
        private bool IsProcessWaitDone()
        {
            var task = processWaitTask;
            if (task != null)
            {
                if ((task.IsCompleted == false
                || task.Status == TaskStatus.Running
                || task.Status == TaskStatus.WaitingToRun
                || task.Status == TaskStatus.WaitingForActivation))
                {
                    return false;
                }
                return true;
            }
            return true;
        }

        #endregion process task


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
