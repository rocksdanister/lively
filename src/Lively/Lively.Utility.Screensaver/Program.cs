using Lively.Utility.Screensaver.Com;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Lively.Utility.Screensaver
{
    /// <summary>
    /// Lightweight version of screensaver launcher.
    /// We rely on the installed app to communicate to the running instance.
    /// Low dependency libraries and .NET framework ensure final build size to be few kilobytes.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            // Not possible to display screensaver in restricted lockscreen region since this utility and screensaver(s) are different applications.
            if (IsSystemLocked())
                return;

            var mutexName = "LIVELY:DESKTOPWALLPAPERSYSTEM";
            var installerGuid = "{E3E43E1B-DEC8-44BF-84A6-243DBA3F2CB1}";
            var packageFamilyName = "12030rocksdanister.LivelyWallpaper_97hta09mmv6hy";
            var appUserModelId = $"{packageFamilyName}!App";
            var isRunning = IsAppMutexRunning(mutexName);
            var (option, hwnd) = ParseScreensaverArgs(args);
            var startArgs = option switch
            {
                ScreensaverOptions.show => !isRunning ? "screensaver --showExclusive true" : "screensaver --show true",
                ScreensaverOptions.preview => $"screensaver --preview {hwnd}",
                ScreensaverOptions.configure => "screensaver --configure true",
                ScreensaverOptions.undefined => string.Empty,
                _ => string.Empty,
            };

            // Incorrect argument, ignore || Do not launch new instance of app unless exclusive screensaver mode.
            if (option == ScreensaverOptions.undefined || (!isRunning && option != ScreensaverOptions.show))
                return;

            // If app is already running will forward the message to running instance via ipc, otherwise starts in exclusive screensaver mode.
            if (TryGetInnoInstalledAppPath(installerGuid, out string installedPath))
            {
                Process.Start(Path.Combine(installedPath, "Lively.exe"), startArgs);
            }
            else
            {
                // Don't work with DesktopBridge, Windows screensaver is not run on desktop session.
                if (!isRunning)
                    return;

                try
                {
                    _ = new ApplicationActivationManager().ActivateApplication(appUserModelId, startArgs, ActivateOptions.None, out _);
                }
                catch { /*Ignore*/ }
            }
        }

        // Ref: https://sites.harding.edu/fmccown/screensaver/screensaver.html
        // CC BY-SA 2.0
        private static (ScreensaverOptions, int) ParseScreensaverArgs(string[] args)
        {
            if (args.Length > 0)
            {
                string firstArgument = args[0].ToLower().Trim();
                string secondArgument = null;

                // Handle cases where arguments are separated by colon.
                // Examples: /c:1234567 or /P:1234567
                // ref: https://sites.harding.edu/fmccown/screensaver/screensaver.html
                if (firstArgument.Length > 2)
                {
                    secondArgument = firstArgument.Substring(3).Trim();
                    firstArgument = firstArgument.Substring(0, 2);
                }
                else if (args.Length > 1)
                    secondArgument = args[1];

                if (firstArgument == "/c")  // Configuration mode
                {
                    return (ScreensaverOptions.configure, 0);
                }
                else if (firstArgument == "/p") // Preview mode
                {
                    return (ScreensaverOptions.preview, int.Parse(secondArgument));
                }
                else if (firstArgument == "/s") // Full-screen mode
                {
                    return (ScreensaverOptions.show, 0);
                }
                else
                {
                    // Undefined argument
                    return (ScreensaverOptions.undefined, 0);
                }
            }
            else  // No arguments - treat like /c
            {
                return (ScreensaverOptions.configure, 0);
            }
        }

        // The path is stored to registry to HKLM (administrative install mode) or HKCU (non administrative install mode) to a subkey named after the AppId with _is1 suffix,
        // stored under a key SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall (as you alraedy know). The value name is Inno Setup: App Path.
        // The path is also stored to InstallLocation with additional trailing slash, as that's where Windows reads it from. But Inno Setup reads the first value.
        // Ref: https://stackoverflow.com/questions/68990713/how-to-access-the-path-of-inno-setup-installed-program-from-outside-of-inno-setu
        private static bool TryGetInnoInstalledAppPath(string appId, out string installPath)
        {
            var appPathValueName = "Inno Setup: App Path";
            var registryPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{appId}}}_is1";

            // Check x64 registry
            installPath = GetRegistryValue(RegistryHive.CurrentUser, registryPath, appPathValueName, RegistryView.Registry64) ??
                GetRegistryValue(RegistryHive.LocalMachine, registryPath, appPathValueName, RegistryView.Registry64);

            // If not found retry x86 registry
            installPath ??= GetRegistryValue(RegistryHive.CurrentUser, registryPath, appPathValueName, RegistryView.Registry32) ??
                    GetRegistryValue(RegistryHive.LocalMachine, registryPath, appPathValueName, RegistryView.Registry32);

            return installPath != null;
        }

        private static bool IsSystemLocked()
        {
            bool result = false;
            var fHandle = GetForegroundWindow();
            try
            {
                GetWindowThreadProcessId(fHandle, out int processID);
                using Process fProcess = Process.GetProcessById(processID);
                result = fProcess.ProcessName.Equals("LockApp", StringComparison.OrdinalIgnoreCase);
            }
            catch { /* Ignore */ }
            return result;
        }

        private static bool IsAppMutexRunning(string mutexName)
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

        private static string GetRegistryValue(RegistryHive hive, string registryPath, string valueName, RegistryView view)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var subKey = baseKey.OpenSubKey(registryPath);

            return subKey?.GetValue(valueName) as string;
        }

        private enum ScreensaverOptions
        {
            show,
            preview,
            configure,
            undefined
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
