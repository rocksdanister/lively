using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace livelywpf
{
    public static class WindowsStartup
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Adds startup entry in registry under application name "livelywpf", current user ONLY. (Does not require admin rights).
        /// </summary>
        /// <param name="setStartup">Add or delete entry.</param>
        public static void SetStartupRegistry(bool setStartup = false)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            if (setStartup)
            {
                try
                {
                    key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }
            }
            else
            {
                try
                {
                    key.DeleteValue(curAssembly.GetName().Name, false);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }
            }
            key.Close();
        }


        /// <summary>
        /// Get the windows startup key value.
        /// </summary>
        /// <returns>null/empty if not found, string otherwise.</returns>
        public static string GetStartupRegistry()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            string result = null;
            try
            {
                result = (string)key.GetValue(curAssembly.GetName().Name);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            finally
            {
                key.Close();
            }

            return result;
        }

        /// <summary>
        /// Verify if windows startup registry value is valid.
        /// </summary>
        /// <returns>true: valid, false: not found/invalid.</returns>
        public static bool CheckStartupRegistry()
        {
            var startupKey = GetStartupRegistry();
            if (String.IsNullOrEmpty(startupKey))
            {
                //no key value.
                return false;
            }
            else if (String.Equals(startupKey, Assembly.GetExecutingAssembly().Location, StringComparison.Ordinal))
            {
                //everything is ok.
                return true;
            }
            else
            {
                //key values do not match, wrong location in registry.
                return false;
            }
        }
    }
}
