using Desktop;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcDotNetNamedPipes;
using Lively.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public class WinDesktopCoreClient : IDisposable
    {
        public event EventHandler<int> WallpaperChanged;

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

        public async Task<List<ScreenModel>> GetScreens()
        {
            var displays = new List<ScreenModel>();
            try
            {
                using var call = client.GetScreens(new Empty());
                while (await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current;
                    displays.Add(response);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return displays;
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
