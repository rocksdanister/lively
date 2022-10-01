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
using Lively.Helpers;
using Lively.Views;
using static Lively.Common.Errors;
using System.Reflection;
using NLog;

namespace Lively.RPC
{
    internal class WinDesktopCoreServer : DesktopService.DesktopServiceBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;
        private readonly IUserSettingsService userSettings;

        public WinDesktopCoreServer(IDesktopCore desktopCore, IDisplayManager displayManager, IUserSettingsService userSettings)
        {
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.userSettings = userSettings;
        }

        public override Task<GetCoreStatsResponse> GetCoreStats(Empty _, ServerCallContext context)
        {
            return Task.FromResult(new GetCoreStatsResponse()
            {
                BaseDirectory = AppDomain.CurrentDomain.BaseDirectory,
                AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
            });
        }

        public override Task<Empty> SetWallpaper(SetWallpaperRequest request, ServerCallContext context)
        {
            try
            {
                var lm = WallpaperUtil.ScanWallpaperFolder(request.LivelyInfoPath);
                lm.DataType = (LibraryItemType)(int)request.Type;
                var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
                desktopCore.SetWallpaper(lm, display ?? displayManager.PrimaryDisplayMonitor);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            return Task.FromResult(new Empty());
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
                        Screen = new ScreenData()
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
                        Category = (WallpaperCategory)(int)wallpaper.Category
                    };
                    await responseStream.WriteAsync(item);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public override Task<Empty> PreviewWallpaper(PreviewWallpaperRequest request, ServerCallContext context)
        {
            try
            {
                var lm = WallpaperUtil.ScanWallpaperFolder(request.LivelyInfoPath);
                _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
                  {
                      new WallpaperPreview(lm)
                      {
                          WindowStartupLocation = WindowStartupLocation.CenterScreen,
                          Topmost = true,
                      }.Show();
                  }));
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
            return Task.FromResult(new Empty());
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
            try
            {
                var lm = WallpaperUtil.ScanWallpaperFolder(request.LivelyInfoPath);
                desktopCore.CloseWallpaper(lm, request.Terminate);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
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
                        tcs.TrySetResult(true);
                    }
                    using var item = context.CancellationToken.Register(() => { tcs.TrySetResult(false); });
                    await tcs.Task;

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        desktopCore.WallpaperChanged -= WallpaperChanged;
                        break;
                    }

                    await responseStream.WriteAsync(new Empty());
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public override async Task SubscribeUpdateWallpaper(Empty _, IServerStreamWriter<UpdateWallpaperResponse> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    UpdateWallpaperResponse resp = null;
                    var tcs = new TaskCompletionSource<bool>();
                    desktopCore.WallpaperUpdated += WallpaperUpdated;
                    void WallpaperUpdated(object s, WallpaperUpdateArgs e)
                    {
                        resp = new UpdateWallpaperResponse()
                        {
                            Title = e.Info.Title ?? string.Empty,
                            Description = e.Info.Desc ?? string.Empty,
                            Website = e.Info.Contact ?? string.Empty,
                            Author = e.Info.Author ?? string.Empty,
                            ThumbnailPath = e.Info.Thumbnail ?? string.Empty,
                            PreviewPath = e.Info.Preview ?? string.Empty,
                            LivelyInfoPath = e.InfoPath ?? string.Empty,
                            Type = (UpdateWallpaperCategory)(int)e.Category,
                            IsAbsolutePath = e.Info.IsAbsolutePath,
                        };
                        desktopCore.WallpaperUpdated -= WallpaperUpdated;
                        tcs.TrySetResult(true);
                    }
                    using var item = context.CancellationToken.Register(() => { tcs.TrySetResult(false); });
                    await tcs.Task;

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        desktopCore.WallpaperUpdated -= WallpaperUpdated;
                        break;
                    }

                    await responseStream.WriteAsync(resp);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public override async Task SubscribeWallpaperError(Empty _, IServerStreamWriter<WallpaperErrorResponse> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var resp = new WallpaperErrorResponse();
                    var tcs = new TaskCompletionSource<bool>();
                    desktopCore.WallpaperError += WallpaperError;
                    void WallpaperError(object s, Exception e)
                    {
                        desktopCore.WallpaperError -= WallpaperError;

                        resp.ErrorMsg = e.Message ?? string.Empty;
                        resp.Error = e switch
                        {
                            WorkerWException _ => ErrorCategory.Workerw,
                            WallpaperNotAllowedException _ => ErrorCategory.WallpaperNotAllowed,
                            WallpaperNotFoundException _ => ErrorCategory.WallpaperNotFound,
                            WallpaperPluginException _ => ErrorCategory.WallpaperPluginFail,
                            WallpaperPluginNotFoundException _ => ErrorCategory.WallpaperPluginNotFound,
                            WallpaperPluginMediaCodecException _ => ErrorCategory.WallpaperPluginMediaCodecMissing,
                            ScreenNotFoundException _ => ErrorCategory.ScreenNotFound,
                            _ => ErrorCategory.General,
                        };
                        tcs.TrySetResult(true);
                    }
                    using var item = context.CancellationToken.Register(() => { tcs.TrySetResult(false); });
                    await tcs.Task;

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        desktopCore.WallpaperError -= WallpaperError;
                        break;
                    }

                    await responseStream.WriteAsync(resp);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
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

        public override async Task<Empty> TakeScreenshot(WallpaperScreenshotRequest request, ServerCallContext context)
        {
            try
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        {
                            var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => request.MonitorId == x.Screen.DeviceId);
                            if (wallpaper is not null)
                            {
                                await wallpaper.ScreenCapture(request.SavePath);
                            }
                        }
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        if (desktopCore.Wallpapers.Any())
                        {
                            await desktopCore.Wallpapers[0].ScreenCapture(request.SavePath);
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                Logger.Error(e);
            }
            return await Task.FromResult(new Empty());
        }
    }
}
