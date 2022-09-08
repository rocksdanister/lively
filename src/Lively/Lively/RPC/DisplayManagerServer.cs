using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Core.Display;
using Lively.Grpc.Common.Proto.Display;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;


namespace Lively.RPC
{
    internal class DisplayManagerServer : DisplayService.DisplayServiceBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IDisplayManager displayManager;

        public DisplayManagerServer(IDisplayManager displayManager)
        {
            this.displayManager = displayManager;
        }

        public override Task<GetScreensResponse> GetScreens(Empty _, ServerCallContext context)
        {
            var resp = new GetScreensResponse();
            foreach (var display in displayManager.DisplayMonitors)
            {
                var item = new ScreenData()
                {
                    DeviceId = display.DeviceId ?? string.Empty,
                    DeviceName = display.DeviceName ?? string.Empty,
                    DisplayName = display.DisplayName ?? string.Empty,
                    HMonitor = display.HMonitor.ToInt32(),
                    IsPrimary = display.IsPrimary,
                    Index = display.Index,
                    WorkingArea = new Rectangle()
                    {
                        X = display.WorkingArea.X,
                        Y = display.WorkingArea.Y,
                        Width = display.WorkingArea.Width,
                        Height = display.WorkingArea.Height
                    },
                    Bounds = new Rectangle()
                    {
                        X = display.Bounds.X,
                        Y = display.Bounds.Y,
                        Width = display.Bounds.Width,
                        Height = display.Bounds.Height
                    }
                };
                resp.Screens.Add(item);
            }
            return Task.FromResult(resp);
        }

        public override Task<Rectangle> GetVirtualScreenBounds(Empty _, ServerCallContext context)
        {
            var resp = new Rectangle()
            {
                X = displayManager.VirtualScreenBounds.X,
                Y = displayManager.VirtualScreenBounds.Y,
                Width = displayManager.VirtualScreenBounds.Width,
                Height = displayManager.VirtualScreenBounds.Height,
            };
            return Task.FromResult(resp);
        }

        public override async Task SubscribeDisplayChanged(Empty _, IServerStreamWriter<Empty> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    displayManager.DisplayUpdated += DisplayChanged;
                    void DisplayChanged(object s, EventArgs e)
                    {
                        displayManager.DisplayUpdated -= DisplayChanged;
                        tcs.TrySetResult(true);
                    }
                    using var item = context.CancellationToken.Register(() => { tcs.TrySetResult(false); });
                    await tcs.Task;

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        displayManager.DisplayUpdated -= DisplayChanged;
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
    }
}
