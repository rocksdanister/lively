using Lively.Common.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface IAppUpdaterClient : IDisposable
    {
        string LastCheckChangelog { get; }
        DateTime LastCheckTime { get; }
        Uri LastCheckUri { get; }
        Version LastCheckVersion { get; }
        AppUpdateStatus Status { get; }

        event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        Task CheckUpdate();
        Task StartUpdate();
    }
}
