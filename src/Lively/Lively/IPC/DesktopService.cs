using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Desktop;
using System.Threading.Tasks;
using Lively.Models;
using System.IO;
using Lively.Common.Helpers.Storage;
using Lively.Common;
using Microsoft.Extensions.DependencyInjection;
using Lively.Core;
using Lively.Core.Display;

namespace Lively.IPC
{
    internal class DesktopService : Desktop.DesktopService.DesktopServiceBase
    {
        public override Task<WallpaperResponse> SetWallpaper(WallpaperRequest request, ServerCallContext context)
        {
            //TEST
            var core = App.Services.GetRequiredService<IDesktopCore>();
            var display = App.Services.GetRequiredService<IDisplayManager>();
            var lm = ScanWallpaperFolder(request.LivelyInfoPath);//(@"C:\Users\rocks\AppData\Local\Lively Wallpaper_v2\Library\wallpapers\iqdvd4pt.jyo");
            core.SetWallpaper(lm, display.PrimaryDisplayMonitor);

            return Task.FromResult(new WallpaperResponse
            {
                Status = true,
            });
        }

        //TEST
        private ILibraryModel ScanWallpaperFolder(string folderPath)
        {
            if (File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
            {
                LivelyInfoModel info = null;
                try
                {
                    info = JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(folderPath, "LivelyInfo.json"));
                }
                catch (Exception e)
                {
                    //Logger.Error(e.ToString());
                }

                if (info != null)
                {
                    if (info.Type == WallpaperType.videostream || info.Type == WallpaperType.url)
                    {
                        //online content, no file.
                        //Logger.Info("Loading Wallpaper (no-file):- " + info.FileName + " " + info.Type);
                        return new LibraryModel(info, folderPath, LibraryItemType.ready, false);
                    }
                    else
                    {
                        if (info.IsAbsolutePath)
                        {
                            //Logger.Info("Loading Wallpaper(absolute):- " + info.FileName + " " + info.Type);
                        }
                        else
                        {
                            //Logger.Info("Loading Wallpaper(relative):- " + Path.Combine(folderPath, info.FileName) + " " + info.Type);
                        }
                        return new LibraryModel(info, folderPath, LibraryItemType.ready, false);
                    }
                }
            }
            else
            {
                //Logger.Info("Not a lively wallpaper folder, skipping:- " + folderPath);
            }
            return null;
        }
    }
}
