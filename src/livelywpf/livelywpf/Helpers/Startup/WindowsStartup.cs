using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf.Helpers.Startup
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
                    key.SetValue(curAssembly.GetName().Name, "\"" + Path.ChangeExtension(curAssembly.Location, ".exe") + "\"");
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
        /// Checks application key status in windows startup registry.
        /// </summary>
        /// <returns>0: not found, 1: correct keypresent, -1: wrong keypresent</returns>
        public static int CheckStartupRegistry()
        {
            int status;
            var startupKey = GetStartupRegistry();
            if (string.IsNullOrEmpty(startupKey))
            {
                //no key value.
                status = 0;
            }
            else if (string.Equals(startupKey, "\"" + Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe") + "\"", StringComparison.Ordinal))
            {
                //everything is ok.
                status = 1;
            }
            else
            {
                //key values do not match, wrong location in registry.
                status = -1;
            }
            return status;
        }

        //ref: https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.startuptask?view=winrt-19041
        public async static Task StartupWin10(bool setStartup = false)
        {
            // Pass the task ID you specified in the appxmanifest file
            StartupTask startupTask = await StartupTask.GetAsync("AppStartup");
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    Logger.Info("Startup is disabled");
                    // Task is disabled but can be enabled.
                    // ensure that you are on a UI thread when you call RequestEnableAsync()
                    if (setStartup)
                    {
                        StartupTaskState newState = await startupTask.RequestEnableAsync();
                        Logger.Info("Request to enable startup " + newState);
                    }
                    break;
                case StartupTaskState.DisabledByUser:
                    // Task is disabled and user must enable it manually.
                    if (setStartup)
                    {
                        await Task.Run(() => MessageBox.Show("You have disabled this app's ability to run " +
                            "as soon as you sign in, but if you change your mind, " +
                            "you can enable this in the Startup tab in Task Manager.", 
                            Properties.Resources.TextError, 
                            MessageBoxButton.OK));
                    }
                    break;
                case StartupTaskState.DisabledByPolicy:
                    Logger.Error("Startup disabled by group policy, or not supported on this device");
                    break;
                case StartupTaskState.Enabled:
                    Logger.Info("Startup is enabled.");
                    if (!setStartup)
                    {
                        startupTask.Disable();
                        Logger.Info("Request to disable startup");
                    }
                    break;
            }
        }

        /// <summary>
        /// Check startup state (desktopbridge.)
        /// </summary>
        /// <returns>0: disabled, -1: disabled by policy/user, 1: enabled</returns>
        public async static Task<int> StartupCheck()
        {
            var result = 0;
            StartupTask startupTask = await StartupTask.GetAsync("AppStartup");
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    result = 0;
                    break;
                case StartupTaskState.DisabledByUser:
                    // Task is disabled and user must enable it manually.
                    result = -1;
                    break;
                case StartupTaskState.DisabledByPolicy:
                    result = -1;
                    break;
                case StartupTaskState.Enabled:
                    result = 1;
                    break;
            }
            return result;
        }
    }
}
