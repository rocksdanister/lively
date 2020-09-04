using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace livelywpf.Core
{
    /// <summary>
    /// Issue(.net core) window handle zero: https://github.com/dotnet/runtime/issues/32690
    /// </summary>
    public class ExtPrograms : IWallpaper
    {
        public ExtPrograms(string path, LibraryModel model, LivelyScreen display)
        {
            string cmdArgs;
            if (model.LivelyInfo.Type == WallpaperType.unity)
            {
                //-popupwindow removes from taskbar
                //-fullscreen disable fullscreen mode if set during compilation (lively is handling resizing window instead).
                //Alternative flags:
                //Unity attaches to workerw by itself; Problem: Process window handle is returning zero.
                //"-parentHWND " + workerw.ToString();// + " -popupwindow" + " -;
                cmdArgs = "-popupwindow -screen-fullscreen 0";
            }
            else
            {
                cmdArgs = model.LivelyInfo.Arguments;
            }

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                WorkingDirectory = System.IO.Path.GetDirectoryName(path),
                Arguments = cmdArgs,
            };

            Process proc = new Process()
            {
                StartInfo = start,
            };

            this.Proc = proc;
            this.Model = model;
            this.Display = display;
            SuspendCnt = 0;
        }
        IntPtr HWND { get; set; }
        Process Proc { get; set; }
        LibraryModel Model { get; set; }
        LivelyScreen Display { get; set; }
        public UInt32 SuspendCnt { get; set; }
        public event EventHandler<WindowInitializedArgs> WindowInitialized;

        CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task processWaitTask;

        public async void Close()
        {
            TaskProcessWaitCancel();
            while(!IsProcessWaitDone())
            {
                await Task.Delay(1);
            }

            try
            {
                //Not reliable, app may refuse to close(dialogue window visible etc)
                //Proc.CloseMainWindow();
                Proc.Refresh();
                Proc.Kill();
                Proc.Close();
            }
            catch { }
        }

        public IntPtr GetHWND()
        {
            return HWND;
        }

        public Process GetProcess()
        {
            return Proc;
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
            try
            {
                ProcessSuspend.SuspendAllThreads(this);
                //thread buggy noise otherwise?!
                VolumeMixer.SetApplicationMute(Proc.Id, true);
            }
            catch { }
        }

        public void Play()
        {
            try
            {
                ProcessSuspend.ResumeAllThreads(this);
                //thread buggy noise otherwise?!
                VolumeMixer.SetApplicationMute(Proc.Id, false);
            }
            catch { }
        }

        public void SetHWND(IntPtr hwnd)
        {
            HWND = hwnd;
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public LivelyScreen GetScreen()
        {
            return Display;
        }

        public async void Show()
        {
            if (Proc != null)
            {
                try
                {
                    Proc.Exited += Proc_Exited;
                    Proc.Start();
                    processWaitTask = Task.Run(() => HWND = WaitForProcesWindow().Result, ctsProcessWait.Token);
                    await processWaitTask;
                    if(HWND.Equals(IntPtr.Zero))
                    {
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = null, Msg = "Pgm handle is zero!" });
                    }
                    else
                    {
                        WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = true, Error = null, Msg = null });
                    }
                }
                catch(OperationCanceledException e1)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e1, Msg = "Pgm terminated early/user cancel!" });
                    Close();
                }
                catch(InvalidOperationException e2)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e2, Msg = "Pgm crashed/closed already!" });
                    Close();
                }
                catch (Exception e3)
                {
                    WindowInitialized?.Invoke(this, new WindowInitializedArgs() { Success = false, Error = e3, Msg = null });
                    Close();
                }
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Proc.Close();
            SetupDesktop.RefreshDesktop();
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

            IntPtr configW = IntPtr.Zero;
            int i = 0;
            try
            {
                Proc.Refresh();
                //waiting for msgloop to be ready, gui not guaranteed to be ready!.
                while (Proc.WaitForInputIdle(-1) != true) 
                {
                    ctsProcessWait.Token.ThrowIfCancellationRequested();
                }
            }
            catch (InvalidOperationException)
            {
                //no gui, failed to enter idle state.
                throw new OperationCanceledException();
            }

            if (GetWallpaperType() == WallpaperType.godot)
            {
                while (i < Program.SettingsVM.Settings.WallpaperWaitTime && Proc.HasExited == false)
                {
                    i++;
                    configW = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Engine", null);
                    if (!IntPtr.Equals(configW, IntPtr.Zero))
                        break;
                    await Task.Delay(1);
                }
                return configW;
            }
            else if (GetWallpaperType() == WallpaperType.unity)
            {
                i = 0;
                //Player settings dialog of Unity, simulating play button click or search workerw if paramter given in argument.
                while (i < Program.SettingsVM.Settings.WallpaperWaitTime && Proc.HasExited == false)
                {
                    ctsProcessWait.Token.ThrowIfCancellationRequested();
                    i++;
                    if (!IntPtr.Equals(Proc.MainWindowHandle, IntPtr.Zero))
                        break;
                    await Task.Delay(1);
                }
                configW = NativeMethods.FindWindowEx(Proc.MainWindowHandle, IntPtr.Zero, "Button", "Play!");
                if (!IntPtr.Equals(configW, IntPtr.Zero))
                {
                    //simulate Play! button click. (Unity config window)
                    NativeMethods.SendMessage(configW, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                }

                await Task.Delay(1);
            }
            Proc.Refresh(); //update window-handle of unity config

            i = 0;
            //there does not seem to be a "proper" way to check whether mainwindow is ready.
            while (i < Program.SettingsVM.Settings.WallpaperWaitTime && Proc.HasExited == false)
            {
                ctsProcessWait.Token.ThrowIfCancellationRequested();
                i++;
                if (!IntPtr.Equals(Proc.MainWindowHandle, IntPtr.Zero))
                {
                    //moving the window out of screen.
                    //StaticPinvoke.SetWindowPos(proc.MainWindowHandle, 1, -20000, 0, 0, 0, 0x0010 | 0x0001); 
                    break;
                }
                await Task.Delay(1);
            }

            Proc.Refresh();
            if (Proc.MainWindowHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            else
                return Proc.MainWindowHandle;
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
                if((task.IsCompleted == false
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

        public void SendMessage(string msg)
        {
            //throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void SetVolume(int volume)
        {
            
        }
    }
}
