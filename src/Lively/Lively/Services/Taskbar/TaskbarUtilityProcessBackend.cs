using Lively.Models.Enums;
using Lively.Models.Taskbar;

namespace Lively.Services.Taskbar
{
    internal sealed class TaskbarUtilityProcessBackend : ITaskbarThemeBackend
    {
        private readonly TaskbarUtilityProcessClient client;
        private TaskbarThemeState currentState = new(TaskbarTheme.none, System.Drawing.Color.Black);
        private bool disposedValue;

        public TaskbarUtilityProcessBackend(TaskbarUtilityProcessClient client)
        {
            this.client = client;
        }

        public bool IsRunning { get; private set; }
        public string Name => nameof(TaskbarUtilityProcessBackend);

        public void Start(TaskbarThemeState state)
        {
            if (state.Theme == TaskbarTheme.none)
            {
                Stop();
                return;
            }

            currentState = state;
            IsRunning = client.Apply(ToRequest(state));
        }

        public void Refresh()
        {
            if (currentState.Theme == TaskbarTheme.none)
                return;

            IsRunning = IsRunning && client.IsRunning ? client.Refresh() : client.Apply(ToRequest(currentState));
        }

        public void Stop()
        {
            client.Shutdown();
            IsRunning = false;
            currentState = new TaskbarThemeState(TaskbarTheme.none, System.Drawing.Color.Black);
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                client.Dispose();
                disposedValue = true;
            }
        }

        private static TaskbarIpcRequest ToRequest(TaskbarThemeState state)
        {
            return new TaskbarIpcRequest
            {
                Type = TaskbarIpcCommand.Apply,
                Theme = state.Theme,
                Color = TaskbarThemeColor.ToUtilityColor(state.AccentColor),
                BlurRadius = state.Theme == TaskbarTheme.blur ? 3.0f : 0.0f,
            };
        }
    }
}
