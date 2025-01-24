using Lively.Common;
using Lively.Common.Helpers.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.UI.Shared.Helpers
{
    public static class DesktopBridgeUtil
    {
        public static async Task OpenFolder(string path)
        {
            if (!Constants.ApplicationType.IsMSIX)
            {
                FileUtil.OpenFolder(path);
                return;
            }

            try
            {
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

                var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(packagePath));
                await Windows.System.Launcher.LaunchFolderAsync(folder);
            }
            catch
            {
                //ApplicationData is unreliable.
                //Issue: https://github.com/microsoft/WindowsAppSDK/issues/101
            }
        }
    }
}
