namespace Lively.Services.Taskbar
{
    internal sealed class UnavailableTaskbarThemeBackend : ITaskbarThemeBackend
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string reason;

        public UnavailableTaskbarThemeBackend(string reason)
        {
            this.reason = reason;
        }

        public bool IsRunning => false;

        public string Name => nameof(UnavailableTaskbarThemeBackend);

        public void Refresh()
        {
        }

        public void Start(TaskbarThemeState state)
        {
            Logger.Warn("Taskbar theme is unavailable: {0}", reason);
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}
