using Lively.Common;
using Lively.Common.Helpers.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Helpers
{
    public static class DesktopBridgeUtil
    {
        /// <summary>
        /// ApplicationData is unreliable, may throw error.<br>
        /// Issue: https://github.com/microsoft/WindowsAppSDK/issues/101
        /// </br>
        /// </summary>
        /// <param name="path">Desktop-bridge virtualized path if found.</param>
        /// <returns></returns>
        public static string GetVirtualizedPath(string path)
        {
            if (!Constants.ApplicationType.IsMSIX)
            {
                return path;
            }

            var packagePath = path;
            var localFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
            var packageAppData = Path.Combine(localFolder, "Local", "Lively Wallpaper");
            if (path.Length > Constants.CommonPaths.AppDataDir.Count() + 1)
            {
                var tmp = Path.Combine(packageAppData, path.Remove(0, Constants.CommonPaths.AppDataDir.Count() + 1));
                if (File.Exists(tmp) || Directory.Exists(tmp))
                {
                    packagePath = tmp;
                }
            }
            return packagePath;
        }
    }
}