using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
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
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using Lively.Services;
using Lively.Grpc.Common.Proto.Desktop;
using Newtonsoft.Json;
using Lively.Common.API;
using Lively.Common.Helpers;

namespace Lively.RPC
{
    internal class WinDesktopCoreServer : DesktopService.DesktopServiceBase
    {
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;
        private readonly IRunnerService runner;

        public WinDesktopCoreServer(IDesktopCore desktopCore, IDisplayManager displayManager, IRunnerService runner)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.runner = runner;
        }

        public override Task<Empty> SetWallpaper(SetWallpaperRequest request, ServerCallContext context)
        {
            //TEST
            var lm = ScanWallpaperFolder(request.LivelyInfoPath);
            var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
            desktopCore.SetWallpaper(lm, display ?? displayManager.PrimaryDisplayMonitor);

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> ShutDown(Empty _, ServerCallContext context)
        {
            try
            {
                return Task.FromResult(new Empty());
            }
            finally
            {
                App.ShutDown();
            }
        }

        public override async Task GetWallpapers(Empty _, IServerStreamWriter<GetWallpapersResponse> responseStream, ServerCallContext context)
        {
            try
            {
                foreach (var wallpaper in desktopCore.Wallpapers)
                {
                    var item = new GetWallpapersResponse()
                    {
                        LivelyInfoPath = wallpaper.Model.LivelyInfoFolderPath,
                        Screen = new GetScreensResponse()
                        {
                            DeviceId = wallpaper.Screen.DeviceId,
                            DeviceName = wallpaper.Screen.DeviceName,
                            DisplayName = wallpaper.Screen.DisplayName,
                            HMonitor = wallpaper.Screen.HMonitor.ToInt32(),
                            IsPrimary = wallpaper.Screen.IsPrimary,
                            Index = wallpaper.Screen.Index,
                            WorkingArea = new Rectangle()
                            {
                                X = wallpaper.Screen.WorkingArea.X,
                                Y = wallpaper.Screen.WorkingArea.Y,
                                Width = wallpaper.Screen.WorkingArea.Width,
                                Height = wallpaper.Screen.WorkingArea.Height
                            },
                            Bounds = new Rectangle()
                            {
                                X = wallpaper.Screen.Bounds.X,
                                Y = wallpaper.Screen.Bounds.Y,
                                Width = wallpaper.Screen.Bounds.Width,
                                Height = wallpaper.Screen.Bounds.Height
                            }
                        },
                        ThumbnailPath = wallpaper.Model.ThumbnailPath ?? string.Empty,
                        PreviewPath = wallpaper.Model.PreviewClipPath ?? string.Empty,
                        PropertyCopyPath = wallpaper.LivelyPropertyCopyPath ?? string.Empty,
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

        public override async Task SubscribeWallpaperChanged(Empty _, IServerStreamWriter<Empty> responseStream, ServerCallContext context)
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

                    await responseStream.WriteAsync(new Empty());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public override Task<Empty> SendMessageWallpaper(WallpaperMessageRequest request, ServerCallContext context)
        {
            var obj = JsonConvert.DeserializeObject<IpcMessage>(request.Msg, new JsonSerializerSettings()
            {
                Converters = {
                    new IpcMessageConverter()
                }}
            );

            if (string.IsNullOrEmpty(request.MonitorId))
            {
                desktopCore.SendMessageWallpaper(request.LivelyInfoPath, obj);
            }
            else
            {
                var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
                desktopCore.SendMessageWallpaper(display, request.LivelyInfoPath, obj);
            }
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> ShowUI(Empty _, ServerCallContext context)
        {
            runner.ShowUI();
            return Task.FromResult(new Empty());
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
