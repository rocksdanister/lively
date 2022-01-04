using Desktop;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.ConsoleDemo
{
    public class WinDesktopCoreClient : IDisposable
    {
        public event EventHandler<int> WallpaperChanged;
        //TODO
        public event EventHandler DisplayChanged;

        //private readonly List<WallpaperData> wallpapers = new List<WallpaperData>(2);
        //public ReadOnlyCollection<WallpaperData> Wallpapers => wallpapers.AsReadOnly();

        private readonly DesktopService.DesktopServiceClient client;
        private readonly CancellationTokenSource cancellationTokeneWallpaperChanged;
        private readonly Task wallpaperChangedTask;
        private bool disposedValue;

        public WinDesktopCoreClient()
        {
            client = new DesktopService.DesktopServiceClient(GetChannel());

            cancellationTokeneWallpaperChanged = new CancellationTokenSource();
            wallpaperChangedTask = Task.Run(() => SubscribeWallpaperChangedServer(cancellationTokeneWallpaperChanged.Token));
        }

        public async Task<bool> SetWallpaper(string livelyInfoPath, string monitorId)
        {
            bool status = false;
            try
            {
                var request = new SetWallpaperRequest
                {
                    LivelyInfoPath = livelyInfoPath,
                    MonitorId = monitorId,
                };

                var response = await client.SetWallpaperAsync(request);
                status = response.Status;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return status;
        }

        public async Task<bool> SetWallpaper(ILibraryModel item, IDisplayMonitor display) => 
            await SetWallpaper(item.LivelyInfoFolderPath, display.DeviceId);

        public async Task<List<WallpaperModel>> GetWallpapers()
        {
            var wallpapers = new List<WallpaperModel>();
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
                await client.CloseWallpaperCategoryAsync(new CloseWallpaperCategoryRequest() { 
                    Category = (WallpaperCategory)((int)type), 
                    Terminate = terminate }
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
                await client.CloseWallpaperLibraryAsync(new CloseWallpaperLibraryRequest() {
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
                await client.CloseWallpaperMonitorAsync(new CloseWallpaperMonitorRequest() { 
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
        public async Task<List<IDisplayMonitor>> GetScreens()
        {
            var screens = new List<ScreenModel>();
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

            var displayMonitors = new List<IDisplayMonitor>();
            foreach (var screen in screens)
            {
                displayMonitors.Add(new DisplayMonitor() { DeviceId = screen.DeviceId });
            }
            return displayMonitors;
        }

        private async Task SubscribeWallpaperChangedServer(CancellationToken token)
        {
            try
            {
                using var call = client.SubscribeWallpaperChanged(new Empty());
                while (await call.ResponseStream.MoveNext(token))
                {
                    var response = call.ResponseStream.Current;
                    WallpaperChanged?.Invoke(this, response.Count);
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
                    wallpaperChangedTask?.Wait(100);
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

        #region helpers

        private static NamedPipeChannel GetChannel() =>
            new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName);

        #endregion //helpers
    }
}
