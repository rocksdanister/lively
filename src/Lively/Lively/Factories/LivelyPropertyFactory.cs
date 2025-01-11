using Lively.Common;
using Lively.Common.Services;
using Lively.Models;
using Lively.Models.Enums;
using System.IO;

namespace Lively.Factories
{
    public class LivelyPropertyFactory : ILivelyPropertyFactory
    {
        public string CreateLivelyPropertyFolder(LibraryModel model, DisplayMonitor display, WallpaperArrangement arrangement, IUserSettingsService userSettings)
        {
            // Customisation not supported.
            if (model.LivelyPropertyPath is null)
                return null;

            string propertyCopyPath = null;
            var dataFolder = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir);
            try
            {
                // Create a directory with the wallpaper foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                // Further modifications are done to the copy file.
                string wallpaperDataDirectoryPath = null;
                switch (arrangement)
                {
                    case WallpaperArrangement.per:
                        wallpaperDataDirectoryPath = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, display.Index.ToString());
                        break;
                    case WallpaperArrangement.span:
                        wallpaperDataDirectoryPath = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "span");
                        break;
                    case WallpaperArrangement.duplicate:
                        wallpaperDataDirectoryPath = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "duplicate");
                        break;
                }
                Directory.CreateDirectory(wallpaperDataDirectoryPath);
                // Copy the original file if not found..
                propertyCopyPath = Path.Combine(wallpaperDataDirectoryPath, "LivelyProperties.json");
                if (!File.Exists(propertyCopyPath))
                    File.Copy(model.LivelyPropertyPath, propertyCopyPath);
            }
            catch { /* Ignore, file related issue so consider wallpaper uncustomisable. */ }

            return propertyCopyPath;
        }
    }
}
