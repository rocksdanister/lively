using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface ICommandsClient
    {
        Task ScreensaverConfigure();
        Task ScreensaverPreview(int previewHandle);
        Task ScreensaverShow(bool show);
        Task ShowUI();
        Task CloseUI();
        Task RestartUI();
        Task RestartUI(string startArgs);
        Task ShowDebugger();
        Task ShutDown();
        Task AutomationCommandAsync(string[] args);
        void AutomationCommand(string[] args);
        void SaveRectUI();
        Task SaveRectUIAsync();
    }
}