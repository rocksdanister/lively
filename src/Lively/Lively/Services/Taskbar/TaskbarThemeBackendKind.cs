using System;

namespace Lively.Services.Taskbar
{
    internal enum TaskbarThemeBackendKind
    {
        Legacy,
        Modern,
        Unavailable,
    }

    internal static class TaskbarThemeBackendSelector
    {
        private static readonly Version ModernWindows11TaskbarBuild = new(10, 0, 22621, 1343);

        public static TaskbarThemeBackendKind Select(Version osVersion, bool modernUtilityAvailable)
        {
            if (IsModernWindows11Taskbar(osVersion))
            {
                return modernUtilityAvailable ? TaskbarThemeBackendKind.Modern : TaskbarThemeBackendKind.Unavailable;
            }

            return TaskbarThemeBackendKind.Legacy;
        }

        private static bool IsModernWindows11Taskbar(Version osVersion)
        {
            if (osVersion.Major != 10 || osVersion.Minor != 0)
                return false;

            if (osVersion.Build > ModernWindows11TaskbarBuild.Build)
                return true;

            return osVersion.Build == ModernWindows11TaskbarBuild.Build &&
                osVersion.Revision >= ModernWindows11TaskbarBuild.Revision;
        }
    }
}
