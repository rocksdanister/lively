using Lively.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Lively.Grpc.Common.Proto.Display;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Threading;
using System.Linq;

namespace Lively.Grpc.Client
{
    public class DisplayManagerClient : IDisplayManagerClient
    {
        public event EventHandler DisplayChanged;

        private readonly List<DisplayMonitor> displayMonitors = new List<DisplayMonitor>(2);
        public ReadOnlyCollection<DisplayMonitor> DisplayMonitors => displayMonitors.AsReadOnly();
        public DisplayMonitor PrimaryMonitor { get; private set; }
        public System.Drawing.Rectangle VirtulScreenBounds { get; private set; }

        private readonly DisplayService.DisplayServiceClient client;
        private readonly SemaphoreSlim displayChangedLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource cancellationTokeneDisplayChanged;
        private readonly Task displayChangedTask;
        private bool disposedValue;

        public DisplayManagerClient()
        {
            client = new DisplayService.DisplayServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));

            Task.Run(async () =>
            {
                displayMonitors.AddRange(await GetScreens().ConfigureAwait(false));
                VirtulScreenBounds = await GetVirtualScreenBounds().ConfigureAwait(false);
                PrimaryMonitor = displayMonitors.FirstOrDefault(x => x.IsPrimary);
            }).Wait();

            cancellationTokeneDisplayChanged = new CancellationTokenSource();
            displayChangedTask = Task.Run(() => SubscribeDisplayChangedStream(cancellationTokeneDisplayChanged.Token));
        }

        private async Task<List<DisplayMonitor>> GetScreens()
        {
            var resp = await client.GetScreensAsync(new Empty());
            var monitors = new List<DisplayMonitor>();
            foreach (var screen in resp.Screens)
            {
                monitors.Add(new DisplayMonitor()
                {
                    DeviceId = screen.DeviceId,
                    DisplayName = screen.DisplayName,
                    DeviceName = screen.DeviceName,
                    HMonitor = new IntPtr(screen.HMonitor),
                    IsPrimary = screen.IsPrimary,
                    Index = screen.Index,
                    Bounds = new System.Drawing.Rectangle(
                        screen.Bounds.X,
                        screen.Bounds.Y,
                        screen.Bounds.Width,
                        screen.Bounds.Height),
                    WorkingArea = new System.Drawing.Rectangle(
                        screen.WorkingArea.X,
                        screen.WorkingArea.Y,
                        screen.WorkingArea.Width,
                        screen.WorkingArea.Height),

                });
            }
            return monitors;
        }

        private async Task<System.Drawing.Rectangle> GetVirtualScreenBounds()
        {
            var resp = await client.GetVirtualScreenBoundsAsync(new Empty());
            var vsb = new System.Drawing.Rectangle(
                        resp.X,
                        resp.Y,
                        resp.Width,
                        resp.Height);
            return vsb;
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
                        displayMonitors.AddRange(await GetScreens().ConfigureAwait(false));
                        VirtulScreenBounds = await GetVirtualScreenBounds().ConfigureAwait(false);
                        PrimaryMonitor = displayMonitors.FirstOrDefault(x => x.IsPrimary);
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

        #region  dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokeneDisplayChanged?.Cancel();
                    displayChangedTask?.Wait();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DisplayManagerClient()
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
