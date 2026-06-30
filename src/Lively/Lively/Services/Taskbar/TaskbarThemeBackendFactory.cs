using System;

namespace Lively.Services.Taskbar
{
    internal static class TaskbarThemeBackendFactory
    {
        public static ITaskbarThemeBackend Create()
        {
            var osVersion = TaskbarWindowsVersion.GetCurrent();
            if (TaskbarThemeBackendSelector.Select(osVersion, false) == TaskbarThemeBackendKind.Legacy)
            {
                return new LegacyTaskbarThemeBackend();
            }

            var utilityClient = new TaskbarUtilityProcessClient();
            if (TaskbarThemeBackendSelector.Select(osVersion, utilityClient.Exists) == TaskbarThemeBackendKind.Modern)
            {
                return new TaskbarUtilityProcessBackend(utilityClient);
            }

            utilityClient.Dispose();
            return new UnavailableTaskbarThemeBackend("Modern Windows 11 taskbar utility is unavailable.");
        }
    }
}
