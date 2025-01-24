using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static Lively.Common.Constants;

namespace Lively.Common.Helpers
{
    public static class AppLifeCycleUtil
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

        public static bool IsNamedPipeExists(string pipeName) => 
            Directory.GetFiles("\\\\.\\pipe\\").Any(f => f.Equals("\\\\.\\pipe\\" + pipeName));

        public static LivelyAppVer GetRunningLivelyAppVer()
        {
            if (IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                return IsNamedPipeExists(SingleInstance.GrpcPipeServerName) ? LivelyAppVer.v2 : LivelyAppVer.v1;
            }
            return LivelyAppVer.nil;
        }

        public enum LivelyAppVer
        {
            nil,
            v1,
            v2
        }
    }
}