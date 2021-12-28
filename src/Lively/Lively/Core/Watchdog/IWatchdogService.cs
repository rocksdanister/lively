namespace Lively.Core.Watchdog
{
    /// <summary>
    /// External service to monitor and close wallpaper plugins in the event of failure.
    /// </summary>
    public interface IWatchdogService
    {
        /// <summary>
        /// Add program to monitor.
        /// </summary>
        /// <param name="pid">processid of program.</param>
        void Add(int pid);
        /// <summary>
        /// Clear programs currently being monitored.
        /// </summary>
        void Clear();
        /// <summary>
        /// Remove the given program from being monitored.
        /// </summary>
        /// <param name="pid">processid of program.</param>
        void Remove(int pid);
        /// <summary>
        /// Start watchdog service.
        /// </summary>
        void Start();
    }
}