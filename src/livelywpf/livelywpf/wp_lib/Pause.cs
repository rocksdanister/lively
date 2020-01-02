using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//using CefSharp;

namespace livelywpf
{  
    //todo:- change confusing function names(wtf was I thinking) & beheavior for suspendwallpaper() & resumewallpaper()
    public static class Pause
    {
        /// <summary>
        /// PAUSE, UNPAUSE/MUTES-AUIDIO of all currently running wp's based input parameters.
        /// If isFullScreen = true, pause everything; otherwise Resume playback.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isFullScreen">is the running app(s) window fullscreen</param>
        /// <param name="displayDeviceName">if null, ignore displayDevice check:- applies to all screen.</param>
        public static void SuspendWallpaper(bool isFullScreen, string displayDeviceName = null ) 
        {
            try
            {
                if (isFullScreen)  //fullscreen app, pauses all wp's & mute audio.
                {
                    MainWindow.SwitchTrayIcon(true);
                    foreach (var item in SetupDesktop.mediakitPlayers)
                    {
                        //item.mp.StopPlayer();
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                            item.MP.PausePlayer();
                    }

                    foreach (var item in SetupDesktop.wmPlayers)
                    {

                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                            item.MP.PausePlayer();
                    }

                    foreach (var item in SetupDesktop.gifWallpapers)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                            item.Gif.PausePlayer();
                    }

                    foreach (var item in SetupDesktop.webProcesses)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                        {
                            NativeMethods.ShowWindow(item.Handle, 6); //minimize
                                                                      //pausing audio thread causes some audio to remain playing?! todo: find a more elegant soln.
                            if (SaveData.config.MuteCefAudioIn)
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            else
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                        }
                    }

                    foreach (var item in SetupDesktop.extPrograms)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                        {
                            Pause.SuspendAllThreads(item);
                            //pausing audio thread causes some audio to remain playing?!
                            VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                        }
                    }

                    foreach (var item in SetupDesktop.extVidPlayers)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                        {
                            Pause.SuspendAllThreads(item);
                            //pausing audio thread causes some audio to remain playing?!
                            VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                        }
                    }

                }
                else //non fullscreen application infocus, mostly mutes audio & resume playback of all wp's.
                {
                    MainWindow.SwitchTrayIcon(false);
                    foreach (var item in SetupDesktop.mediakitPlayers)
                    {
                        if (!SaveData.config.AlwaysAudio || SaveData.config.MuteVideo || displayDeviceName != null) // (user setting || multimonitor scenario)
                            item.MP.MutePlayer(true);
                        else
                            item.MP.MutePlayer(false);

                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                            item.MP.PlayMedia();
                    }

                    foreach (var item in SetupDesktop.wmPlayers)
                    {
                        if (!SaveData.config.AlwaysAudio || SaveData.config.MuteVideo || displayDeviceName != null ) // (user setting || multimonitor scenario)
                            item.MP.MutePlayer(true);
                        else
                            item.MP.MutePlayer(false);

                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                            item.MP.PlayMedia();
                    }

                    foreach (var item in SetupDesktop.gifWallpapers)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                            item.Gif.ResumePlayer();
                    }

                    foreach (var item in SetupDesktop.webProcesses)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                        {
                            NativeMethods.ShowWindow(item.Handle, 1); //normal
                            NativeMethods.ShowWindow(item.Handle, 5); //show

                            if ( (!SaveData.config.AlwaysAudio || SaveData.config.MuteCefAudioIn || displayDeviceName != null) 
                                                                && item.Type != SetupDesktop.WallpaperType.web_audio)
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            else
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                        }
                    }

                    foreach (var item in SetupDesktop.extPrograms)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                        {
                            Pause.ResumeAllThreads(item);
                            //pausing audio thread causes some audio to remain playing?!
                            //(SaveData.config.MuteAppWP || display != null) && item.Type != SetupDesktop.WallpaperType.unity_audio
                            if ( (!SaveData.config.AlwaysAudio || SaveData.config.MuteAppWP || displayDeviceName != null) 
                                                               && item.Type != SetupDesktop.WallpaperType.unity_audio)
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            }
                            else
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                            }
                        }
                    }

                    foreach (var item in SetupDesktop.extVidPlayers)
                    {
                        if (item.DisplayID == displayDeviceName || displayDeviceName == null)
                        {
                            Pause.ResumeAllThreads(item);

                            if (!SaveData.config.AlwaysAudio || SaveData.config.MuteVideo || displayDeviceName != null)
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            else
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                        }
                    }
                }
            }
            catch(InvalidOperationException)
            {
                //loop running on list when modification being done(rare), ignore since this fn is run continously every 500msec... everythings fine *nervous laughter*
                //todo: could make a copy of it usin ToList(), will try later.
            }
  
        }

        /// <summary>
        /// UNPAUSE all currently running wp's based given conditions.
        /// If isFullScreen = true, Resume everything; otherwise Resume playback.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isFullScreen">is the window fullscreen</param>
        /// <param name="display">if null, ignore displayDevice check.</param>
        public static void ResumeWallpaper(bool isFullScreen, string display = null)
        {
            try
            {
                if (isFullScreen) //desktop(why else would u call resume for fullscreen app) etc, unmutes audio & resume wp's playback.
                {
                    MainWindow.SwitchTrayIcon(false);
                    foreach (var item in SetupDesktop.mediakitPlayers)
                    {
                        if (SaveData.config.MuteVideo || display != null) // (user setting || multimonitor scenario)
                            item.MP.MutePlayer(true);
                        else
                            item.MP.MutePlayer(false);

                        if (item.DisplayID == display || display == null)
                        {
                            item.MP.PlayMedia();
                        }
                    }

                    foreach (var item in SetupDesktop.wmPlayers)
                    {
                        if (SaveData.config.MuteVideo || display != null) // (user setting || multimonitor scenario)
                            item.MP.MutePlayer(true);
                        else
                            item.MP.MutePlayer(false);

                        if (item.DisplayID == display || display == null)
                        {
                            item.MP.PlayMedia();
                        }
                    }

                    foreach (var item in SetupDesktop.gifWallpapers)
                    {
                        if (item.DisplayID == display || display == null)
                            item.Gif.ResumePlayer();
                    }

                    foreach (var item in SetupDesktop.webProcesses)
                    {
                        if (item.DisplayID == display || display == null)
                        {
                            NativeMethods.ShowWindow(item.Handle, 1); //normal
                            NativeMethods.ShowWindow(item.Handle, 5); //show

                            if (SaveData.config.MuteCefAudioIn)
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            else
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                        }
                    }

                    foreach (var item in SetupDesktop.extPrograms)
                    {
                        if (item.DisplayID == display || display == null)
                        {
                            Pause.ResumeAllThreads(item);
                            if ( (SaveData.config.MuteAppWP || display != null) && item.Type != SetupDesktop.WallpaperType.unity_audio)
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            }
                            else
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                            }
                        }
                    }

                    foreach (var item in SetupDesktop.extVidPlayers)
                    {
                        if (item.DisplayID == display || display == null)
                        {
                            Pause.ResumeAllThreads(item);
                            if (SaveData.config.MuteVideo || display != null)
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            }
                            else
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                                /*
                                if (System.Windows.Forms.Screen.PrimaryScreen.DeviceName.Equals(display, StringComparison.OrdinalIgnoreCase))
                                    VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                                else
                                    VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                                */
                            }
                        }
                    }
                }
                else //non fullscreen application infocus, unmutes audio & resume wp's playback.
                {
                    MainWindow.SwitchTrayIcon(false);
                    foreach (var item in SetupDesktop.mediakitPlayers)
                    {
                        if (SaveData.config.MuteVideo || display != null) // (user setting || multimonitor scenario)
                            item.MP.MutePlayer(true);
                        else
                            item.MP.MutePlayer(false);
                        if (item.DisplayID == display || display == null)
                        {
                            item.MP.PlayMedia();
                        }
                    }

                    foreach (var item in SetupDesktop.wmPlayers)
                    {
                        if (SaveData.config.MuteVideo || display != null) // (user setting || multimonitor scenario)
                            item.MP.MutePlayer(true);
                        else
                            item.MP.MutePlayer(false);
                        if (item.DisplayID == display || display == null)
                        {
                            item.MP.PlayMedia();
                        }
                    }

                    foreach (var item in SetupDesktop.gifWallpapers)
                    {
                        if (item.DisplayID == display || display == null)
                            item.Gif.ResumePlayer();
                    }

                    foreach (var item in SetupDesktop.webProcesses)
                    {
                        if (item.DisplayID == display || display == null)
                        {
                            NativeMethods.ShowWindow(item.Handle, 1); //normal
                            NativeMethods.ShowWindow(item.Handle, 5); //show

                            if (SaveData.config.MuteCefAudioIn)
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            else
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                        }
                    }

                    foreach (var item in SetupDesktop.extPrograms)
                    {
                        if (item.DisplayID == display || display == null)
                        {
                            Pause.ResumeAllThreads(item);
                            //pausing audio thread causes some audio to remain playing?!                            
                            if ((SaveData.config.MuteAppWP) && item.Type != SetupDesktop.WallpaperType.unity_audio)
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            }
                            else
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false);
                            }
                        }
                    }

                    foreach (var item in SetupDesktop.extVidPlayers)
                    {
                        if (item.DisplayID == display || display == null)
                        {
                            Pause.ResumeAllThreads(item);
                            //pausing audio thread causes some audio to remain playing?!
                            if (SaveData.config.MuteVideo || display != null)
                            {
                                VolumeMixer.SetApplicationMute(item.Proc.Id, true);
                            }
                            else
                                VolumeMixer.SetApplicationMute(item.Proc.Id, false); //prev: true
                        }
                    }
                }
            }
            catch(InvalidOperationException)
            {
                //ignore
            }
        }

        static void SuspendAllThreads(SetupDesktop.ExtProgram item)
        {
            try
            {
                foreach (ProcessThread thread in item.Proc.Threads)
                {
                    var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        break;
                        // continue;
                    }

                    /* https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-suspendthread
                     * Each thread has a suspend count (with a maximum value of MAXIMUM_SUSPEND_COUNT).
                     * If the suspend count is greater than zero, the thread is suspended; otherwise, the thread is not suspended and is eligible for execution. 
                     * Calling SuspendThread causes the target thread's suspend count to be incremented.
                     * Attempting to increment past the maximum suspend count causes an error without incrementing the count.
                     */
                    if (item.SuspendCnt == 0)
                        item.SuspendCnt = NativeMethods.SuspendThread(pOpenThread);

                    NativeMethods.CloseHandle(pOpenThread);
                }
            }
            catch
            {
                //pgm unexpected ended etc, ignore; setupdesktop class will dispose it once ready.
            }
        }

        static void ResumeAllThreads(SetupDesktop.ExtProgram item)
        {
            try
            {
                foreach (ProcessThread thread in item.Proc.Threads)
                {
                    var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        break;
                        //  continue;
                    }

                    /* https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-resumethread
                     * The ResumeThread function checks the suspend count of the subject thread. If the suspend count is zero, the thread is not currently suspended.
                     * Otherwise, the subject thread's suspend count is decremented. If the resulting value is zero, then the execution of the subject thread is resumed.
                     * If the return value is zero, the specified thread was not suspended. If the return value is 1, the specified thread was suspended but was restarted. 
                     * If the return value is greater than 1, the specified thread is still suspended.
                     */
                    do
                    {
                        item.SuspendCnt = (uint)NativeMethods.ResumeThread(pOpenThread);
                    } while (item.SuspendCnt > 0);

                    NativeMethods.CloseHandle(pOpenThread);
                }
            }
            catch
            {
                //pgm unexpected ended etc, ignore; setupdesktop class will dispose it once ready.
            }
        }

        static void SuspendAllThreads(SetupDesktop.ExtVidPlayers item)
        {
            try
            {
                foreach (ProcessThread thread in item.Proc.Threads)
                {
                    var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        break;
                        // continue;
                    }

                    /* https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-suspendthread
                     * Each thread has a suspend count (with a maximum value of MAXIMUM_SUSPEND_COUNT).
                     * If the suspend count is greater than zero, the thread is suspended; otherwise, the thread is not suspended and is eligible for execution. 
                     * Calling SuspendThread causes the target thread's suspend count to be incremented.
                     * Attempting to increment past the maximum suspend count causes an error without incrementing the count.
                     */
                    if (item.SuspendCnt == 0)
                        item.SuspendCnt = NativeMethods.SuspendThread(pOpenThread);

                    NativeMethods.CloseHandle(pOpenThread);
                }
            }
            catch
            {
                //pgm unexpected ended etc, ignore; setupdesktop class will dispose it once ready.
            }
        }

        static void ResumeAllThreads(SetupDesktop.ExtVidPlayers item)
        {
            try
            {
                foreach (ProcessThread thread in item.Proc.Threads)
                {
                    var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        break;
                        //  continue;
                    }

                    /* https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-resumethread
                     * The ResumeThread function checks the suspend count of the subject thread. If the suspend count is zero, the thread is not currently suspended.
                     * Otherwise, the subject thread's suspend count is decremented. If the resulting value is zero, then the execution of the subject thread is resumed.
                     * If the return value is zero, the specified thread was not suspended. If the return value is 1, the specified thread was suspended but was restarted. 
                     * If the return value is greater than 1, the specified thread is still suspended.
                     */
                    do
                    {
                        item.SuspendCnt = (uint)NativeMethods.ResumeThread(pOpenThread);
                    } while (item.SuspendCnt > 0);

                    NativeMethods.CloseHandle(pOpenThread);
                }
            }
            catch
            {
                //pgm unexpected ended etc, ignore; setupdesktop class will dispose it once ready.
            }
        }

        [Obsolete]
        static void SuspendAllThreads(SetupDesktop.CefProcess item)
        {
            Process cefProcess = new Process();
            foreach (ProcessThread thread in cefProcess.Threads)//item.proc.Threads)
            {       
                var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                    // continue;
                }
                //StaticPinvoke.SuspendThread(pOpenThread);

                if (item.SuspendCnt == 0)
                    item.SuspendCnt = NativeMethods.SuspendThread(pOpenThread);

                NativeMethods.CloseHandle(pOpenThread);
            }
        }

        [Obsolete]
        static void ResumeAllThreads(SetupDesktop.CefProcess item)
        {
            foreach (ProcessThread thread in item.Proc.Threads)
            {
                var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                    //  continue;
                }
                //StaticPinvoke.ResumeThread(pOpenThread);

                //var suspendCount = 0;
                do
                {
                    item.SuspendCnt = (uint)NativeMethods.ResumeThread(pOpenThread);
                } while (item.SuspendCnt > 0);

                NativeMethods.CloseHandle(pOpenThread);
            }
        }

        public enum Options
        {
            List,
            Kill,
            Suspend,
            Resume
        }

     
        public class Param
        {
            public int PId { get; set; }
            public string Expression { get; set; }
            public Options Option { get; set; }
        }
    }
}
