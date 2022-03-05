using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel;
using Lively.Common;

namespace Lively.Helpers
{
    public static class WindowsStartup
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public async static Task SetStartup(bool setStartup)
        {
            if (Constants.ApplicationType.IsMSIX)
            {
                await SetStartupTask(setStartup);
            }
            else
            {
                SetStartupRegistry(setStartup);
            }
        }

        /// <summary>
        /// Adds startup entry in registry under application name "livelywpf", current user ONLY. (Does not require admin rights).
        /// </summary>
        /// <param name="setStartup">Add or delete entry.</param>
        private static void SetStartupRegistry(bool setStartup = false)
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

        //ref: https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.startuptask?view=winrt-19041
        private async static Task SetStartupTask(bool setStartup = false)
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
                            "Lively Wallpaper",
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
                    default:
                    if (setStartup)
                    {
                        Logger.Info("Startup state default, possibly different value.");
                        StartupTaskState newState = await startupTask.RequestEnableAsync();
                        Logger.Info("Request to enable startup " + newState);
                    }
                    break;
            }
        }
    }
}
