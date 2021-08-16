using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.Helpers
{
    interface IAppUpdater
    {
        Task<(Uri, Version, string)> GetLatestRelease(bool isBeta);
    }
}
