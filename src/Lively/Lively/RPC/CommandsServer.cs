using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Grpc.Common.Proto.Commands;
using Lively.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lively.RPC
{
    internal class CommandsServer : CommandsService.CommandsServiceBase
    {
        private readonly IRunnerService runner;
        private readonly IScreensaverService screensaver;

        public CommandsServer(IRunnerService runner, IScreensaverService screensaver)
        {
            this.runner = runner;
            this.screensaver = screensaver;
        }

        public override Task<Empty> ShowUI(Empty _, ServerCallContext context)
        {
            runner.ShowUI();
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> Screensaver(ScreensaverRequest request, ServerCallContext context)
        {
            switch (request.State)
            {
                case ScreensaverState.Start:
                    screensaver.Start();
                    break;
                case ScreensaverState.Stop:
                    screensaver.Stop();
                    break;
                case ScreensaverState.Preview:
                    //TODO
                    break;
            }
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
    }
}
