using Google.Protobuf.WellKnownTypes;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Grpc.Common.Proto.Update;
using Lively.Models.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public class AppUpdaterClient : IAppUpdaterClient
    {
        public AppUpdateStatus Status { get; private set; } = AppUpdateStatus.notchecked;
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;
        public Version LastCheckVersion { get; private set; } = new Version(0, 0, 0, 0);
        public string LastCheckChangelog { get; private set; }
        public Uri LastCheckUri { get; private set; }
        public string LastCheckFileName { get; private set; }

        public event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        private readonly UpdateService.UpdateServiceClient client;
        private readonly SemaphoreSlim updateCheckedLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource cancellationTokenUpdateChecked;
        private readonly Task updateCheckedChangedTask;
        private bool disposedValue;

        public AppUpdaterClient()
        {
            client = new UpdateService.UpdateServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));

            Task.Run(async () =>
            {
                await UpdateStatusRefresh().ConfigureAwait(false);
            }).Wait();

            cancellationTokenUpdateChecked = new CancellationTokenSource();
            updateCheckedChangedTask = Task.Run(() => SubscribeUpdateCheckedStream(cancellationTokenUpdateChecked.Token));
        }

        public async Task CheckUpdate()
        {
            await client.CheckUpdateAsync(new Empty());
        }

        public async Task StartUpdate()
        {
            await client.StartUpdateAsync(new Empty());
        }

        private async Task UpdateStatusRefresh()
        {
            var resp = await client.GetUpdateStatusAsync(new Empty());
            Status = (AppUpdateStatus)((int)resp.Status);
            LastCheckTime = resp.Time.ToDateTime().ToLocalTime();
            LastCheckChangelog = resp.Changelog;
            try
            {
                LastCheckVersion = string.IsNullOrEmpty(resp.Version) ? null : new Version(resp.Version);
                LastCheckUri = string.IsNullOrEmpty(resp.Url) ? null : new Uri(resp.Url);
                LastCheckFileName = string.IsNullOrEmpty(resp.FileName) ? null : resp.FileName;
            }
            catch { /* TODO */ }
        }

        private async Task SubscribeUpdateCheckedStream(CancellationToken token)
        {
            try
            {
                using var call = client.SubscribeUpdateChecked(new Empty());
                while (await call.ResponseStream.MoveNext(token))
                {
                    await updateCheckedLock.WaitAsync();
                    try
                    {
                        var resp = call.ResponseStream.Current;
                        await UpdateStatusRefresh();
                        UpdateChecked?.Invoke(this, new AppUpdaterEventArgs(Status, LastCheckVersion, LastCheckTime, LastCheckUri, LastCheckFileName));
                    }
                    finally
                    {
                        updateCheckedLock.Release();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokenUpdateChecked?.Cancel();
                    updateCheckedChangedTask?.Wait();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AppUpdaterClient()
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
    }
}
