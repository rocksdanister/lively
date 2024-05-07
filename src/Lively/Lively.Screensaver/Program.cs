using Lively.Common.Helpers;
using Lively.Grpc.Client;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static Lively.Common.Constants;

namespace Lively.Screensaver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (SingleInstanceUtil.IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                // Application is running
                var commandsClient = new CommandsClient();
                var (option, hwnd) = ParseScreensaverArgs(args);
                switch (option)
                {
                    case ScreensaverOptions.show:
                        await commandsClient.ScreensaverShow(true);
                        break;
                    case ScreensaverOptions.preview:
                        await commandsClient.ScreensaverPreview(hwnd);
                        break;
                    case ScreensaverOptions.configure:
                        await commandsClient.ScreensaverConfigure();
                        break;
                    case ScreensaverOptions.undefined:
                        // Incorrect argument, ignore.
                        break;
                }
            }
            else
            {
                // Application is not running, always launch in screensaver only mode.
                if (TryGetInstalledAppPath("{E3E43E1B-DEC8-44BF-84A6-243DBA3F2CB1}", out string installedPath))
                {
                    Process.Start(Path.Combine(installedPath, "Lively.exe"), "screensaver --showExclusive true");
                }
                else
                {
                    // TODO: MSIX.
                }
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

        private static bool TryGetInstalledAppPath(string appId, out string installPath)
        {
            var uninstallKeyPath32Bit = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            var uninstallKeyPath64Bit = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

            // Try accessing the 32-bit registry first
            using var baseKey32Bit = Registry.LocalMachine.OpenSubKey(uninstallKeyPath32Bit);
            installPath = GerInnoInstallPathInRegistry(baseKey32Bit, appId);
            if (installPath is null)
            {
                // If not found in the 32-bit registry, try the 64-bit registry
                using var baseKey64Bit = Registry.LocalMachine.OpenSubKey(uninstallKeyPath64Bit);
                installPath = GerInnoInstallPathInRegistry(baseKey64Bit, appId);
            }
            return installPath is not null;
        }

        //The path is stored to registry to HKLM (administrative install mode) or HKCU (non administrative install mode) to a subkey named after the AppId with _is1 suffix,
        //stored under a key SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall (as you alraedy know). The value name is Inno Setup: App Path.
        //The path is also stored to InstallLocation with additional trailing slash, as that's where Windows reads it from. But Inno Setup reads the first value.
        //ref: https://stackoverflow.com/questions/68990713/how-to-access-the-path-of-inno-setup-installed-program-from-outside-of-inno-setu
        private static string GerInnoInstallPathInRegistry(RegistryKey baseKey, string appId)
        {
            var subKeySuffix = "_is1";
            var appPathValueName = "Inno Setup: App Path";

            if (baseKey != null)
            {
                foreach (var subKeyName in baseKey.GetSubKeyNames())
                {
                    if (subKeyName.EndsWith(subKeySuffix, StringComparison.OrdinalIgnoreCase) && subKeyName.StartsWith(appId, StringComparison.OrdinalIgnoreCase))
                    {
                        using var subKey = baseKey.OpenSubKey(subKeyName);
                        if (subKey != null)
                        {
                            var installPath = subKey.GetValue(appPathValueName) as string;
                            if (!string.IsNullOrEmpty(installPath))
                            {
                                return installPath;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private enum ScreensaverOptions
        {
            show,
            preview,
            configure,
            undefined
        }
    }
}