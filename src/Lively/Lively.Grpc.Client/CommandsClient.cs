using Google.Protobuf.WellKnownTypes;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Grpc.Common.Proto.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public class CommandsClient : ICommandsClient
    {
        private readonly CommandsService.CommandsServiceClient client;

        public CommandsClient()
        {
            client = new CommandsService.CommandsServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));
        }

        public async Task ShowUI()
        {
            await client.ShowUIAsync(new Empty());
        }

        public async Task RestartUI()
        {
            await client.RestartUIAsync(new Empty());
        }

        public async Task ScreensaverShow(bool show)
        {
            await client.ScreensaverAsync(new ScreensaverRequest()
            {
                State = show ? ScreensaverState.Start : ScreensaverState.Stop,
            });
        }

        public async Task ScreensaverConfigure()
        {
            await client.ScreensaverAsync(new ScreensaverRequest()
            {
                State = ScreensaverState.Configure,
            });
        }

        public async Task ScreensaverPreview(int previewHandle)
        {
            await client.ScreensaverAsync(new ScreensaverRequest()
            {
                State = ScreensaverState.Preview,
                PreviewHwnd = previewHandle,
            });
        }

        public async Task ShutDown()
        {
            await client.ShutDownAsync(new Empty());
        }

        public async Task AutomationCommand(string[] args)
        {
            var request = new AutomationCommandRequest();
            request.Args.AddRange(args);
            await client.AutomationCommandAsync(request);
        }
    }
}
