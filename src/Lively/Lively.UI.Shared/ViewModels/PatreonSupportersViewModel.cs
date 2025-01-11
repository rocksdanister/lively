using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Grpc.Client;
using Lively.UI.WinUI.Helpers;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class PatreonSupportersViewModel : ObservableObject
    {
        private readonly ICommandsClient commandsClient;

        public PatreonSupportersViewModel(ICommandsClient commandsClient)
        {
            this.commandsClient = commandsClient;
        }

        public bool IsBetaBuild => Constants.ApplicationType.IsTestBuild;

        public bool IsWebView2Available => WebViewUtil.IsWebView2Available();

        [ObservableProperty]
        private string supportersFetchError;

        [ObservableProperty]
        private bool isWebView2Installing;

        [RelayCommand]
        private async Task InstallWebView2()
        {
            try
            {
                IsWebView2Installing = true;

                if (await WebViewUtil.InstallWebView2())
                    _ = commandsClient.RestartUI();
                else
                    LinkUtil.OpenBrowser(WebViewUtil.DownloadUrl);
            }
            finally
            {
                IsWebView2Installing = false;
            }
        }
    }
}
