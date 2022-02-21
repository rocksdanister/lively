using System;

namespace Lively.Services
{
    public interface IRunnerService : IDisposable
    {
        IntPtr HwndUI { get; }
        void ShowUI();
        void CloseUI();
        void RestartUI();
        void ShowControlPanel();
        void ShowCustomisWallpaperePanel();
        bool IsVisibleUI { get; }
    }
}