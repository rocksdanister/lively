using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace livelywpf.Helpers
{
    public class AppUpdaterEventArgs : EventArgs
    {
        public AppUpdaterEventArgs(AppUpdateStatus updateStatus, Version updateVersion, DateTime updateDate, Uri updateUri)
        {
            UpdateStatus = updateStatus;
            UpdateVersion = updateVersion;
            UpdateUri = updateUri;
            UpdateDate = updateDate;
        }

        public AppUpdateStatus UpdateStatus { get; }
        public Version UpdateVersion { get; }
        public Uri UpdateUri { get; }
        public DateTime UpdateDate { get; }
    }

    public sealed class AppUpdaterService
    {
        //singleton
        private static readonly AppUpdaterService instance = new AppUpdaterService();

        //in milliseconds
        private readonly int fetchDelayError = 30 * 60 * 1000; //30min
        private readonly int fetchDelayRepeat = 12 * 60 * 60 * 1000; //12hr

        //public
        public AppUpdateStatus Status { get; private set; } = AppUpdateStatus.notchecked;
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;
        public Version LastCheckVersion { get; private set; } = new Version(0, 0, 0, 0);
        public event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        private readonly IAppUpdater updater;
        private readonly Timer retryTimer = new Timer();

        public static AppUpdaterService Instance
        {
            get
            {
                return instance;
            }
        }

        private AppUpdaterService()
        {
            updater = new GithubUpdater();
            retryTimer.Elapsed += RetryTimer_Elapsed;
            //giving the retry delay is not reliable since it will reset if system sleeps/suspends.
            retryTimer.Interval = 5 * 60 * 1000;
        }

        /// <summary>
        /// Check for updates periodically.
        /// </summary>
        public void Start()
        {
            retryTimer.Start();
        }

        /// <summary>
        /// Stops periodic updates check.
        /// </summary>
        public void Stop()
        {
            if (retryTimer.Enabled)
            {
                retryTimer.Stop();
            }
        }

        private void RetryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((DateTime.Now - LastCheckTime).TotalMilliseconds > (Status != AppUpdateStatus.error ? fetchDelayRepeat : fetchDelayError))
            {
                _ = CheckUpdate(0);
            }
        }

        public async Task<AppUpdateStatus> CheckUpdate(int fetchDelay = 45 * 1000)
        {
            if (Program.IsMSIX)
            {
                return AppUpdateStatus.notchecked;
            }

            await Task.Delay(fetchDelay);
            Status = await updater.CheckUpdate();
            LastCheckTime = DateTime.Now;
            LastCheckVersion = updater.GetVersion();
            UpdateChecked?.Invoke(this, new AppUpdaterEventArgs(Status, LastCheckVersion, LastCheckTime, GetUri()));
            return Status;
        }

        public string GetChangelog()
        {
            return updater.GetChangelog();
        }

        public Uri GetUri()
        {
            return updater.GetUri();
        }
    }
}
