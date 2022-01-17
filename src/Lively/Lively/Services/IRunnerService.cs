using System;

namespace Lively.Services
{
    public interface IRunnerService : IDisposable
    {
        void ShowUI();
        bool IsVisibleUI { get; }
    }
}