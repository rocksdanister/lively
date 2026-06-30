using System;

namespace Lively.Services.Taskbar
{
    internal interface ITaskbarThemeBackend : IDisposable
    {
        bool IsRunning { get; }
        string Name { get; }

        void Refresh();
        void Start(TaskbarThemeState state);
        void Stop();
    }
}
