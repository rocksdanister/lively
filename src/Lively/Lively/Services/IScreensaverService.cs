using System;

namespace Lively.Services
{
    public interface IScreensaverService
    {
        bool IsRunning { get; }

        void CreatePreview(IntPtr hwnd);
        void Start();
        void StartIdleTimer(uint idleTime);
        void Stop();
        void StopIdleTimer();
    }
}