using Lively.Common;
using Lively.Models;
using Lively.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lively.Factories
{
    public class LivelyPropertyFactory : ILivelyPropertyFactory
    {
        public string CreateLivelyPropertyFolder(ILibraryModel model, IDisplayMonitor display, WallpaperArrangement arrangement, IUserSettingsService userSettings)
        {
            string propertyPath = null;
            if (model.LivelyPropertyPath != null)
            {
                //customisable wallpaper, livelyproperty.json is present.
                var dataFolder = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir);
                try
                {
                    //extract last digits of the Screen class DeviceName, eg: \\.\DISPLAY4 -> 4
                    var screenNumber = display.Index.ToString();
                    if (screenNumber != null)
                    {
                        //Create a directory with the wp foldername in SaveData/wpdata/, copy livelyproperties.json into this.
                        //Further modifications are done to the copy file.
                        string wpdataFolder = null;
                        switch (arrangement)
                        {
                            case WallpaperArrangement.per:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, screenNumber);
                                break;
                            case WallpaperArrangement.span:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "span");
                                break;
                            case WallpaperArrangement.duplicate:
                                wpdataFolder = Path.Combine(dataFolder, new DirectoryInfo(model.LivelyInfoFolderPath).Name, "duplicate");
                                break;
                        }
                        Directory.CreateDirectory(wpdataFolder);
                        //copy the original file if not found..
                        propertyPath = Path.Combine(wpdataFolder, "LivelyProperties.json");
                        if (!File.Exists(propertyPath))
                        {
                            File.Copy(model.LivelyPropertyPath, propertyPath);
                        }
                    }
                    else
                    {
                        //todo: fallback, use the original file (restore feature disabled.)
                    }
                }
                catch
                {
                    //todo: fallback, use the original file (restore feature disabled.)
                }
            }
            return propertyPath;
        }
    }
}
