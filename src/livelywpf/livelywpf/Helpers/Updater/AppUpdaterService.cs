using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace livelywpf.Helpers.Updater
{
    public class AppUpdaterEventArgs : EventArgs
    {
        public AppUpdaterEventArgs(AppUpdateStatus updateStatus, Version updateVersion, DateTime updateDate, Uri updateUri, string changeLog)
        {
            UpdateStatus = updateStatus;
            UpdateVersion = updateVersion;
            UpdateUri = updateUri;
            UpdateDate = updateDate;
            ChangeLog = changeLog;
        }

        public AppUpdateStatus UpdateStatus { get; }
        public Version UpdateVersion { get; }
        public Uri UpdateUri { get; }
        public DateTime UpdateDate { get; }
        public string ChangeLog { get; }
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
        public string LastCheckChangelog { get; private set; }
        public Uri LastCheckUri { get; private set; }
        public event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        private readonly IAppUpdater updater;
        private readonly Timer retryTimer = new Timer();
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
            if (!Program.IsMSIX)
            {
                retryTimer.Start();
            }
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
                //msix already has built-in updater.
                return AppUpdateStatus.notchecked;
            }

            try
            {
                await Task.Delay(fetchDelay);
                (Uri, Version, string) data = await GetLatestRelease(Program.IsTestBuild);
                int verCompare = CompareAssemblyVersion(data.Item2);
                if (verCompare > 0)
                {
                    //update available.
                    Status = AppUpdateStatus.available;
                }
                else if (verCompare < 0)
                {
                    //beta release.
                    Status = AppUpdateStatus.invalid;
                }
                else
                {
                    //up-to-date.
                    Status = AppUpdateStatus.uptodate;
                }
                LastCheckUri = data.Item1;
                LastCheckVersion = data.Item2;
                LastCheckChangelog = data.Item3;
            }
            catch (Exception e)
            {
                Logger.Error("Update fetch error:" + e.ToString());
                Status = AppUpdateStatus.error;
            }
            LastCheckTime = DateTime.Now;

            UpdateChecked?.Invoke(this, new AppUpdaterEventArgs(Status, LastCheckVersion, LastCheckTime, LastCheckUri, LastCheckChangelog));
            return Status;
        }

        public async Task<(Uri, Version, string)> GetLatestRelease(bool isBeta)
        {
            return await updater.GetLatestRelease(isBeta);
        }

        #region helpers

        public static int CompareAssemblyVersion(Version version)
        {
            var appVersion = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            return version.CompareTo(appVersion);
        }

        #endregion //helpers
    }
}
