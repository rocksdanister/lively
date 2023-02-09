using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Common.Models;
using Lively.Common.Services;
using Lively.Grpc.Common.Proto.Update;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Lively.RPC
{
    internal class AppUpdateServer : UpdateService.UpdateServiceBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IAppUpdaterService updater;

        public AppUpdateServer(IAppUpdaterService updater)
        {
            this.updater = updater;
        }

        public override async Task<Empty> CheckUpdate(Empty _, ServerCallContext context)
        {
            await updater.CheckUpdate(0);
            return await Task.FromResult(new Empty());
        }

        public override Task<Empty> StartUpdate(Empty _, ServerCallContext context)
        {
            if (updater.Status == AppUpdateStatus.available)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    App.AppUpdateDialog(updater.LastCheckUri, updater.LastCheckChangelog);
                }));
            }
            return Task.FromResult(new Empty());
        }

        public override Task<UpdateResponse> GetUpdateStatus(Empty _, ServerCallContext context)
        {
            return Task.FromResult(new UpdateResponse()
            {
                Status = (UpdateStatus)((int)updater.Status),
                Changelog = updater.LastCheckChangelog ?? string.Empty,
                Url = updater.LastCheckUri?.OriginalString ?? string.Empty,
                Version = updater.LastCheckVersion?.ToString() ?? string.Empty,
                Time = Timestamp.FromDateTime(updater.LastCheckTime.ToUniversalTime()),
            });
        }

        public override async Task SubscribeUpdateChecked(Empty _, IServerStreamWriter<Empty> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    updater.UpdateChecked += Updater_UpdateChecked;
                    void Updater_UpdateChecked(object sender, AppUpdaterEventArgs e)
                    {
                        updater.UpdateChecked -= Updater_UpdateChecked;
                        tcs.TrySetResult(true);
                    }
                    using var item = context.CancellationToken.Register(() => { tcs.TrySetResult(false); });
                    await tcs.Task;

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        updater.UpdateChecked -= Updater_UpdateChecked;
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
