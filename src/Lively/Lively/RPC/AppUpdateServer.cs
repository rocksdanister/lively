using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Common;
using Lively.Common.Models;
using Lively.Common.Services.Update;
using Lively.Grpc.Common.Proto.Update;
using Lively.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private readonly IDialogService dialogService;

        public AppUpdateServer(IAppUpdaterService updater, IDialogService dialogService)
        {
            this.updater = updater;
            this.dialogService = dialogService;
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
                try
                {
                    try
                    {
                        // Main user interface downloads the setup.
                        var filePath = Path.Combine(Constants.CommonPaths.TempDir, updater.LastCheckUri.Segments.Last());
                        if (File.Exists(filePath))
                            throw new FileNotFoundException(filePath);

                        // Run setup in silent mode.
                        Process.Start(filePath, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS");
                        // Inno installer will auto retry, waiting for application exit.
                        App.ShutDown();
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                        {
                            dialogService.ShowErrorDialog(Properties.Resources.TextError, $"{Properties.Resources.LivelyExceptionAppUpdateFail}\nException:\n{ex}");
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            return Task.FromResult(new Empty());
        }

        public override Task<UpdateResponse> GetUpdateStatus(Empty _, ServerCallContext context)
        {
            return Task.FromResult(new UpdateResponse()
            {
                Status = (UpdateStatus)((int)updater.Status),
                Changelog = string.Empty,
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
