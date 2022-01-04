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
using System.Diagnostics;
using System.Linq;

namespace Lively.IPC
{
    internal class WinDesktopCoreServer : DesktopService.DesktopServiceBase
    {
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;

        public WinDesktopCoreServer(IDesktopCore desktopCore, IDisplayManager displayManager)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
        }

        public override Task<SetWallpaperResponse> SetWallpaper(SetWallpaperRequest request, ServerCallContext context)
        {
            //TEST
            var lm = ScanWallpaperFolder(request.LivelyInfoPath);
            var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
            desktopCore.SetWallpaper(lm, display ?? displayManager.PrimaryDisplayMonitor);

            return Task.FromResult(new SetWallpaperResponse
            {
                //TODO
                Status = true,
            });
        }

        public override async Task GetWallpapers(Empty _, IServerStreamWriter<WallpaperModel> responseStream, ServerCallContext context)
        {
            try
            {
                foreach (var wallpaper in desktopCore.Wallpapers)
                {
                    var item = new WallpaperModel()
                    {
                        LivelyInfoPath = wallpaper.Model.LivelyInfoFolderPath,
                        MonitorId = wallpaper.Screen.DeviceId,
                    };
                    await responseStream.WriteAsync(item);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public override async Task GetScreens(Empty _, IServerStreamWriter<ScreenModel> responseStream, ServerCallContext context)
        {
            try
            {
                foreach (var display in displayManager.DisplayMonitors)
                {
                    var item = new ScreenModel()
                    {
                        DeviceId = display.DeviceId,
                    };
                    await responseStream.WriteAsync(item);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public override Task<Empty> CloseAllWallpapers(CloseAllWallpapersRequest request, ServerCallContext context)
        {
            desktopCore.CloseAllWallpapers(request.Terminate);
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> CloseWallpaperMonitor(CloseWallpaperMonitorRequest request, ServerCallContext context)
        {
            var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
            if (display != null)
            {
                desktopCore.CloseWallpaper(display, request.Terminate);
            }
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> CloseWallpaperLibrary(CloseWallpaperLibraryRequest request, ServerCallContext context)
        {
            var lm = ScanWallpaperFolder(request.LivelyInfoPath);
            if (lm != null)
            {
                desktopCore.CloseWallpaper(lm, request.Terminate);
            }
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> CloseWallpaperCategory(CloseWallpaperCategoryRequest request, ServerCallContext context)
        {
            try
            {
                desktopCore.CloseWallpaper((WallpaperType)((int)request.Category), request.Terminate);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return Task.FromResult(new Empty());
        }

        public override async Task SubscribeWallpaperChanged(Empty _, IServerStreamWriter<WallpaperChangedModel> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    desktopCore.WallpaperChanged += WallpaperChanged;
                    void WallpaperChanged(object s, EventArgs e)
                    {
                        desktopCore.WallpaperChanged -= WallpaperChanged;
                        tcs.SetResult(true);
                    }
                    await tcs.Task;

                    var response = new WallpaperChangedModel
                    {
                        Count = desktopCore.Wallpapers.Count,
                    };
                    await responseStream.WriteAsync(response);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        #region helpers

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

        #endregion //helpers
    }
}
