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
            // Application is running
            if (SingleInstanceUtil.IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                var commandsClient = new CommandsClient();
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
                        await commandsClient.ScreensaverConfigure();
                    }
                    else if (firstArgument == "/p") // Preview mode
                    {
                        await commandsClient.ScreensaverPreview(Int32.Parse(secondArgument));
                    }
                    else if (firstArgument == "/s") // Full-screen mode
                    {
                        await commandsClient.ScreensaverShow(true);
                    }
                    else { }  // Undefined argument
                }
                else  // No arguments - treat like /c
                {
                    await commandsClient.ScreensaverConfigure();
                }
            }
            else
            {
                // Application is not running
                if (TryGetInstalledAppPath("{E3E43E1B-DEC8-44BF-84A6-243DBA3F2CB1}", out string installPath))
                {
                    Process.Start(Path.Combine(installPath, "Lively.exe"));
                }
            }
        }

        static bool TryGetInstalledAppPath(string appId, out string installPath)
        {
            string uninstallKeyPath32Bit = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            string uninstallKeyPath64Bit = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

            // Try accessing the 32-bit registry first
            RegistryKey baseKey32Bit = Registry.LocalMachine.OpenSubKey(uninstallKeyPath32Bit);
            installPath = GerInnoInstallPathInRegistry(baseKey32Bit, appId);
            if (installPath is null)
            {
                // If not found in the 32-bit registry, try the 64-bit registry
                RegistryKey baseKey64Bit = Registry.LocalMachine.OpenSubKey(uninstallKeyPath64Bit);
                installPath = GerInnoInstallPathInRegistry(baseKey64Bit, appId);
            }
            return installPath is not null;
        }

        //The path is stored to registry to HKLM (administrative install mode) or HKCU (non administrative install mode) to a subkey named after the AppId with _is1 suffix,
        //stored under a key SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall (as you alraedy know). The value name is Inno Setup: App Path.
        //The path is also stored to InstallLocation with additional trailing slash, as that's where Windows reads it from. But Inno Setup reads the first value.
        //ref: https://stackoverflow.com/questions/68990713/how-to-access-the-path-of-inno-setup-installed-program-from-outside-of-inno-setu
        static string GerInnoInstallPathInRegistry(RegistryKey baseKey, string appId)
        {
            string subKeySuffix = "_is1";
            string appPathValueName = "Inno Setup: App Path";

            if (baseKey != null)
            {
                foreach (string subKeyName in baseKey.GetSubKeyNames())
                {
                    if (subKeyName.EndsWith(subKeySuffix, StringComparison.OrdinalIgnoreCase) && subKeyName.StartsWith(appId, StringComparison.OrdinalIgnoreCase))
                    {
                        RegistryKey subKey = baseKey.OpenSubKey(subKeyName);
                        if (subKey != null)
                        {
                            string installPath = subKey.GetValue(appPathValueName) as string;
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
    }
}
