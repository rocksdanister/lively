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

namespace Lively.Grpc.Client
{
    public class DisplayManagerClient : IDisplayManagerClient
    {
        public event EventHandler DisplayChanged;

        private readonly List<IDisplayMonitor> displayMonitors = new List<IDisplayMonitor>(2);
        public ReadOnlyCollection<IDisplayMonitor> DisplayMonitors => displayMonitors.AsReadOnly();

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
                displayMonitors.AddRange(await GetScreens());
            }).Wait();

            cancellationTokeneDisplayChanged = new CancellationTokenSource();
            displayChangedTask = Task.Run(() => SubscribeDisplayChangedStream(cancellationTokeneDisplayChanged.Token));
        }

        private async Task<List<IDisplayMonitor>> GetScreens()
        {
            var screens = new List<GetScreensResponse>();
            using var call = client.GetScreens(new Empty());
            while (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                screens.Add(response);
            }
            var displayMonitors = new List<IDisplayMonitor>();
            foreach (var screen in screens)
            {
                displayMonitors.Add(new DisplayMonitor()
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
            return displayMonitors;
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

        #region  dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokeneDisplayChanged?.Cancel();
                    displayChangedTask?.Wait(100);
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
