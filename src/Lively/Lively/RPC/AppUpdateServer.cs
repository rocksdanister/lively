using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Common;
using Lively.Common.Services;
using Lively.Grpc.Common.Proto.Update;
using Lively.Models.Services;
using System;
using System.Diagnostics;
using System.IO;
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
#if !DEBUG
            await updater.CheckUpdate(0);
#endif
            Debug.WriteLine("App Update checking disabled in DEBUG mode.");
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
                        var fileName = updater.LastCheckFileName;
                        var filePath = Path.Combine(Constants.CommonPaths.TempDir, fileName);
                        if (!File.Exists(filePath))
                            throw new FileNotFoundException(filePath);

                        // Run setup in silent mode.
                        Process.Start(filePath, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS");
                        // Inno installer will auto retry, waiting for application exit.
                        App.QuitApp();
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
                        {
                            MessageBox.Show($"{Properties.Resources.LivelyExceptionAppUpdateFail}\n\nException:\n{ex}", Properties.Resources.TextError, MessageBoxButton.OK, MessageBoxImage.Error);
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
                FileName = updater.LastCheckFileName ?? string.Empty,
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
