using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.Helpers
{
    interface IAppUpdater
    {
        Task<AppUpdateStatus> CheckUpdate(bool isBeta);
        string GetChangelog();
        Uri GetUri();
        Version GetVersion();
    }
}
