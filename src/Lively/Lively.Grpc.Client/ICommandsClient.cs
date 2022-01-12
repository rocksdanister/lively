using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface ICommandsClient
    {
        Task ScreensaverConfigure();
        Task ScreensaverPreview(int previewHandle);
        Task ScreensaverShow(bool show);
        Task ShowUI();
        Task ShutDown();
    }
}