using Lively.Common.Models;
using System;

namespace Lively.Common.Services
{
    public interface IHardwareUsageService
    {
        event EventHandler<HardwareUsageEventArgs> HWMonitor;

        void Start();
        void Stop();
    }
}