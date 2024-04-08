using Lively.Common.Models;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Lively.Common.Services.Update
{
    public interface IAppUpdaterService
    {
        DateTime LastCheckTime { get; }
        Uri LastCheckUri { get; }
        Version LastCheckVersion { get; }
        AppUpdateStatus Status { get; }

        event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        Task<AppUpdateStatus> CheckUpdate(int fetchDelay);
        Task<(Uri, Version)> GetLatestRelease(bool isBeta);
        void Start();
        void Stop();
    }
}