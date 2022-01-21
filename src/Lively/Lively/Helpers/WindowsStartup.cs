using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.Helpers
{
    public static class WindowsStartup
    {
        /// <summary>
        /// Adds startup entry in registry under application name "livelywpf", current user ONLY. (Does not require admin rights).
        /// </summary>
        /// <param name="setStartup">Add or delete entry.</param>
        public static void SetStartupRegistry(bool setStartup = false)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            try
            {
                if (setStartup)
                {
                    key.SetValue(curAssembly.GetName().Name, "\"" + Path.ChangeExtension(curAssembly.Location, ".exe") + "\"");
                }
                else
                {
                    key.DeleteValue(curAssembly.GetName().Name, false);
                }
            }
            finally
            {
                key.Close();
            }
        }

        /// <summary>
        /// Checks application key status in windows startup registry.
        /// </summary>
        public static StartupStatus CheckStartupRegistry()
        {
            StartupStatus status;
            var startupKey = GetStartupRegistry();
            if (string.IsNullOrEmpty(startupKey))
            {
                //no key value.
                status = StartupStatus.missing;
            }
            else if (string.Equals(startupKey, "\"" + Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe") + "\"", StringComparison.Ordinal))
            {
                //everything is ok.
                status = StartupStatus.ok;
            }
            else
            {
                //key values do not match, wrong location in registry.
                status = StartupStatus.invalid;
            }
            return status;
        }

        /// <summary>
        /// Get the windows startup key value.
        /// </summary>
        /// <returns>null/empty if not found, string otherwise.</returns>
        private static string GetStartupRegistry()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            try
            {
                return (string)key.GetValue(curAssembly.GetName().Name);
            }
            finally
            {
                key.Close();
            }
        }

        public enum StartupStatus
        {
            ok,
            invalid,
            missing
        }
    }
}
