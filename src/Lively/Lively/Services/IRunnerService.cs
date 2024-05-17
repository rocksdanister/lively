using System;

namespace Lively.Services
{
    public interface IRunnerService : IDisposable
    {
        IntPtr HwndUI { get; }
        void ShowUI();
        void CloseUI();
        void RestartUI(string startArgs = null);
        void SetBusyUI(bool isBusy);
        void ShowCustomisWallpaperePanel();
        void ShowAppUpdatePage();
        bool IsVisibleUI { get; }
        void SaveRectUI();
    }
}