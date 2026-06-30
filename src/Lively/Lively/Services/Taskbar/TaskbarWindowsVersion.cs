using Microsoft.Win32;
using System;

namespace Lively.Services.Taskbar
{
    internal static class TaskbarWindowsVersion
    {
        public static Version GetCurrent()
        {
            var osVersion = Environment.OSVersion.Version;
            if (TryGetUbr(out var ubr) && osVersion.Build > 0)
            {
                return new Version(osVersion.Major, osVersion.Minor, osVersion.Build, ubr);
            }

            return osVersion;
        }

        private static bool TryGetUbr(out int ubr)
        {
            ubr = 0;
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("UBR") is not int value)
                return false;

            ubr = value;
            return true;
        }
    }
}
