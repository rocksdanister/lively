using Desktop;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    class WallpaperChangedEvent
    {
        private DesktopService.DesktopServiceClient mClient;
        private CancellationTokenSource mCancellationTokenSource;
        private Task mMonitorTask = null;

        public event EventHandler<int> WallpaperChanged;

        public WallpaperChangedEvent(DesktopService.DesktopServiceClient client)
        {
            mClient = client;
        }

        public void Start()
        {
            mCancellationTokenSource = new CancellationTokenSource();
            mMonitorTask = Task.Run(() => SubscribeWallpaperChangedServer(mCancellationTokenSource.Token));
        }

        public void Stop()
        {
            mCancellationTokenSource.Cancel();
            mMonitorTask.Wait(1000);
        }

        private async Task SubscribeWallpaperChangedServer(CancellationToken token)
        {
            try
            {
                using var call = mClient.SubscribeWallpaperChanged(new Empty());
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
    }
}
