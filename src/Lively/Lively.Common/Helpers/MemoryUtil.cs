using System;
using System.Diagnostics;
using Lively.Common.Helpers.Pinvoke;

namespace Lively.Common.Helpers
{
    /// <summary>
    /// Utility for RAM and working set memory optimization.
    /// </summary>
    public static class MemoryUtil
    {
        /// <summary>
        /// Forces Garbage Collection and trims working set memory for the current process to reduce RAM footprint.
        /// </summary>
        public static void OptimizeMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                using (var currentProcess = Process.GetCurrentProcess())
                {
                    NativeMethods.SetProcessWorkingSetSize(currentProcess.Handle, -1, -1);
                }
            }
            catch
            {
                // Ignore memory optimization exceptions
            }
        }
    }
}
