using Lively.Models;
using System;
using System.Collections.ObjectModel;

namespace Lively.Grpc.Client
{
    public interface IDisplayManagerClient : IDisposable
    {
        ReadOnlyCollection<IDisplayMonitor> DisplayMonitors { get; }

        event EventHandler DisplayChanged;
    }
}