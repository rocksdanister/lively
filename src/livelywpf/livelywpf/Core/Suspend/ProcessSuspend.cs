using livelywpf.Core.Wallpapers;
using livelywpf.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace livelywpf.Core.Suspend
{
    public class ProcessSuspend
    {
        public static void SuspendAllThreads(ExtPrograms obj)
        {
            try
            {
                foreach (ProcessThread thread in obj.Proc.Threads)
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
                    if (obj.SuspendCnt == 0)
                        obj.SuspendCnt = NativeMethods.SuspendThread(pOpenThread);

                    NativeMethods.CloseHandle(pOpenThread);
                }
            }
            catch
            {
                //pgm unexpected ended etc, ignore; setupdesktop class will dispose it once ready.
            }
        }

        public static void ResumeAllThreads(ExtPrograms obj)
        {
            try
            {
                foreach (ProcessThread thread in obj.Proc.Threads)
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
                        obj.SuspendCnt = (uint)NativeMethods.ResumeThread(pOpenThread);
                    } while (obj.SuspendCnt > 0);

                    NativeMethods.CloseHandle(pOpenThread);
                }
            }
            catch
            {
                //pgm unexpected ended etc, ignore; setupdesktop class will dispose it once ready.
            }
        }

    }
}
