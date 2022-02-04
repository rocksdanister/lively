using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lively.Common.Helpers
{
    public static class SingleInstanceUtil
    {
        public static bool IsAppMutexRunning(string mutexName)
        {
            Mutex mutex = null;
            try
            {
                return Mutex.TryOpenExisting(mutexName, out mutex);
            }
            finally
            {
                mutex?.Dispose();
            }
        }

        public static bool IsAppProcessRunning(string processName) => 
            Process.GetProcessesByName(processName).Count() != 0;
    }
}
