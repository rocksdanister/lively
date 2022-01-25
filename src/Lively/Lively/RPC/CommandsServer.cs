using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Automation;
using Lively.Grpc.Common.Proto.Commands;
using Lively.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Lively.RPC
{
    internal class CommandsServer : CommandsService.CommandsServiceBase
    {
        private readonly IRunnerService runner;
        private readonly IScreensaverService screensaver;
        private readonly ICommandHandler commandHandler;

        public CommandsServer(IRunnerService runner, IScreensaverService screensaver, ICommandHandler commandHandler)
        {
            this.runner = runner;
            this.screensaver = screensaver;
            this.commandHandler = commandHandler;
        }

        public override Task<Empty> ShowUI(Empty _, ServerCallContext context)
        {
            runner.ShowUI();
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> RestartUI(Empty _, ServerCallContext context)
        {
            runner.RestartUI();
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
                    _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                    {
                        screensaver.CreatePreview(new IntPtr(request.PreviewHwnd));
                    }));
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

        public override Task<Empty> AutomationCommand(AutomationCommandRequest request, ServerCallContext context)
        {
            commandHandler.ParseArgs(request.Args.ToArray());
            return Task.FromResult(new Empty());
        }
    }
}
