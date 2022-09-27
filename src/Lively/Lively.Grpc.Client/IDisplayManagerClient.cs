using Lively.Models;
using System;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Lively.Grpc.Client
{
    public interface IDisplayManagerClient : IDisposable
    {
        ReadOnlyCollection<IDisplayMonitor> DisplayMonitors { get; }
        IDisplayMonitor PrimaryMonitor { get; }
        Rectangle VirtulScreenBounds { get; }

        event EventHandler DisplayChanged;
    }
}