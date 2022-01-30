using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface ICommandsClient
    {
        Task ScreensaverConfigure();
        Task ScreensaverPreview(int previewHandle);
        Task ScreensaverShow(bool show);
        Task ShowUI();
        Task RestartUI();
        Task ShowDebugger();
        Task ShutDown();
        Task AutomationCommandAsync(string[] args);
        void AutomationCommand(string[] args);
        void SetVolume(int volume);
    }
}