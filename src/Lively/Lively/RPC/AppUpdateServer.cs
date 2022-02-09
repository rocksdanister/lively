using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
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
        private readonly IAppUpdaterService updater;

        public AppUpdateServer(IAppUpdaterService updater)
        {
            this.updater = updater;
        }

        public override Task<Empty> CheckUpdate(Empty _, ServerCallContext context)
        {
            updater.CheckUpdate(0);
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> StartUpdate(Empty _, ServerCallContext context)
        {

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                //updater.ShowUpdateDialog();
            }));
            return Task.FromResult(new Empty());
        }

        public override async Task SubscribeUpdateChecked(Empty _, IServerStreamWriter<UpdateCheckedResponse> responseStream, ServerCallContext context)
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
                        tcs.SetResult(true);
                    }
                    await tcs.Task;

                    await responseStream.WriteAsync(new UpdateCheckedResponse()
                    {
                        Status = (UpdateStatus)((int)updater.Status),
                        Changelog = updater.LastCheckChangelog ?? string.Empty,
                        Url = updater.LastCheckUri.OriginalString ?? string.Empty,
                        Version = updater.LastCheckVersion?.ToString() ?? string.Empty,
                        Time = Timestamp.FromDateTime(DateTime.Now),
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
    }
}
