using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Grpc.Common.Proto.Desktop;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    //TODO: don't catch exceptions, just throw.
    public class WinDesktopCoreClient : IDesktopCoreClient
    {
        public event EventHandler WallpaperChanged;
        public event EventHandler DisplayChanged;

        private readonly List<GetScreensResponse> displayMonitors = new List<GetScreensResponse>(2);
        public ReadOnlyCollection<GetScreensResponse> DisplayMonitors => displayMonitors.AsReadOnly();

        private readonly List<GetWallpapersResponse> wallpapers = new List<GetWallpapersResponse>(2);
        public ReadOnlyCollection<GetWallpapersResponse> Wallpapers => wallpapers.AsReadOnly();

        private readonly SemaphoreSlim displayChangedLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim wallpaperChangedLock = new SemaphoreSlim(1, 1);
        private readonly DesktopService.DesktopServiceClient client;
        private readonly CancellationTokenSource cancellationTokeneWallpaperChanged, cancellationTokeneDisplayChanged;
        private readonly Task wallpaperChangedTask, displayChangedTask;
        private bool disposedValue;

        public WinDesktopCoreClient()
        {
            client = new DesktopService.DesktopServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));

            Task.Run(async () =>
            {
                displayMonitors.AddRange(await GetScreens());
                wallpapers.AddRange(await GetWallpapers());
            }).Wait();

            cancellationTokeneWallpaperChanged = new CancellationTokenSource();
            wallpaperChangedTask = Task.Run(() => SubscribeWallpaperChangedStream(cancellationTokeneWallpaperChanged.Token));

            cancellationTokeneDisplayChanged = new CancellationTokenSource();
            displayChangedTask = Task.Run(() => SubscribeDisplayChangedStream(cancellationTokeneDisplayChanged.Token));
        }

        public async Task SetWallpaper(string livelyInfoPath, string monitorId)
        {
            try
            {
                var request = new SetWallpaperRequest
                {
                    LivelyInfoPath = livelyInfoPath,
                    MonitorId = monitorId,
                };

                var response = await client.SetWallpaperAsync(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task SetWallpaper(ILibraryModel item, IDisplayMonitor display) =>
            await SetWallpaper(item.LivelyInfoFolderPath, display.DeviceId);

        private async Task<List<GetWallpapersResponse>> GetWallpapers()
        {
            var wallpapers = new List<GetWallpapersResponse>();
            try
            {
                using var call = client.GetWallpapers(new Empty());
                while (await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current;
                    wallpapers.Add(response);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return wallpapers;
        }

        public async Task CloseAllWallpapers(bool terminate = false)
        {
            try
            {
                await client.CloseAllWallpapersAsync(new CloseAllWallpapersRequest() { Terminate = terminate });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task CloseWallpaper(WallpaperType type, bool terminate = false)
        {
            try
            {
                await client.CloseWallpaperCategoryAsync(new CloseWallpaperCategoryRequest()
                {
                    Category = (WallpaperCategory)((int)type),
                    Terminate = terminate
                }
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task CloseWallpaper(ILibraryModel item, bool terminate = false)
        {
            try
            {
                await client.CloseWallpaperLibraryAsync(new CloseWallpaperLibraryRequest()
                {
                    LivelyInfoPath = item.LivelyInfoFolderPath,
                    Terminate = terminate
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task CloseWallpaper(IDisplayMonitor monitor, bool terminate = false)
        {
            try
            {
                await client.CloseWallpaperMonitorAsync(new CloseWallpaperMonitorRequest()
                {
                    MonitorId = monitor.DeviceId,
                    Terminate = terminate
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        //TODO: hook it into an event and store it as a list.
        private async Task<List<GetScreensResponse>> GetScreens()
        {
            var screens = new List<GetScreensResponse>();
            try
            {
                using var call = client.GetScreens(new Empty());
                while (await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current;
                    screens.Add(response);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return screens;
            /*
            var displayMonitors = new List<IDisplayMonitor>();
            foreach (var screen in screens)
            {
                displayMonitors.Add(new DisplayMonitor() 
                { 
                    DeviceId = screen.DeviceId,
                    DisplayName = screen.DisplayName,
                    //DeviceName = screen.DeviceName,
                    HMonitor = new IntPtr(screen.HMonitor),
                    IsPrimary = screen.IsPrimary,
                    Index = screen.Index,
                });
            }
            return displayMonitors;
            */
        }

        private async Task SubscribeWallpaperChangedStream(CancellationToken token)
        {
            try
            {
                using var call = client.SubscribeWallpaperChanged(new Empty());
                while (await call.ResponseStream.MoveNext(token))
                {
                    await wallpaperChangedLock.WaitAsync();
                    try
                    {
                        var response = call.ResponseStream.Current;

                        wallpapers.Clear();
                        wallpapers.AddRange(await GetWallpapers());
                        WallpaperChanged?.Invoke(this, EventArgs.Empty);
                    }
                    finally
                    {
                        wallpaperChangedLock.Release();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private async Task SubscribeDisplayChangedStream(CancellationToken token)
        {
            try
            {
                using var call = client.SubscribeDisplayChanged(new Empty());
                while (await call.ResponseStream.MoveNext(token))
                {
                    await displayChangedLock.WaitAsync();
                    try
                    {
                        var response = call.ResponseStream.Current;

                        displayMonitors.Clear();
                        displayMonitors.AddRange(await GetScreens());
                        DisplayChanged?.Invoke(this, EventArgs.Empty);
                    }
                    finally
                    {
                        displayChangedLock.Release();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task ShutDown()
        {
            await client.ShutDownAsync(new Empty());
        }

        #region dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    cancellationTokeneWallpaperChanged?.Cancel();
                    cancellationTokeneDisplayChanged?.Cancel();
                    wallpaperChangedTask?.Wait(100);
                    displayChangedTask?.Wait(100);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DesktopServiceClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion //dispose
    }
}
