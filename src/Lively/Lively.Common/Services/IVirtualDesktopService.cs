using System;

namespace Lively.Common.Services
{
    /// <summary>
    /// Tracks the currently active Windows virtual desktop.
    /// </summary>
    public interface IVirtualDesktopService : IDisposable
    {
        /// <summary>
        /// Id of the currently active virtual desktop, Guid.Empty when unknown
        /// (single desktop or unsupported Windows version.)
        /// </summary>
        Guid CurrentDesktopId { get; }

        /// <summary>
        /// Fired when the user switches virtual desktop.
        /// </summary>
        event EventHandler<Guid> CurrentDesktopChanged;

        void Start();
        void Stop();
    }
}
