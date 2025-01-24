using Lively.Models.Services;
using System;
using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public interface IAppUpdaterService
    {
        DateTime LastCheckTime { get; }
        Uri LastCheckUri { get; }
        string LastCheckFileName {get;}
        Version LastCheckVersion { get; }
        AppUpdateStatus Status { get; }

        event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        Task<AppUpdateStatus> CheckUpdate(int fetchDelay);
        void Start();
        void Stop();
    }
}