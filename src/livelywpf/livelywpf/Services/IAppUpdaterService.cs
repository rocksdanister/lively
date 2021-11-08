using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace livelywpf.Services
{
    public interface IAppUpdaterService
    {
        string LastCheckChangelog { get; }
        DateTime LastCheckTime { get; }
        Uri LastCheckUri { get; }
        Version LastCheckVersion { get; }
        AppUpdateStatus Status { get; }

        event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        Task<AppUpdateStatus> CheckUpdate(int fetchDelay = 45000);
        Task<(Uri, Version, string)> GetLatestRelease(bool isBeta);
        void Start();
        void Stop();
    }

    public enum AppUpdateStatus
    {
        [Description("Software is up-to-date.")]
        uptodate,
        [Description("Update available.")]
        available,
        [Description("Installed software version higher than whats available online.")]
        invalid,
        [Description("Update not checked yet.")]
        notchecked,
        [Description("Update check failed.")]
        error,
    }

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
}