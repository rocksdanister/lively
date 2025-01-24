using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;

namespace Lively.UI.Shared.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [RelayCommand]
        private void OpenPersonalWebsite()
        {
            LinkUtil.OpenBrowser("https://rocksdanister.com");
        }

        [RelayCommand]
        private void OpenGithub()
        {
            LinkUtil.OpenBrowser("https://github.com/rocksdanister");
        }

        [RelayCommand]
        private void OpenTwitter()
        {
            LinkUtil.OpenBrowser("https://twitter.com/rocksdanister");
        }

        [RelayCommand]
        private void OpenYoutube()
        {
            LinkUtil.OpenBrowser("https://www.youtube.com/channel/UClep84ofxC41H8-R9UfNPSQ");
        }

        [RelayCommand]
        private void OpenReddit()
        {
            LinkUtil.OpenBrowser("https://reddit.com/u/rocksdanister");
        }

        [RelayCommand]
        private void OpenEmail()
        {
            LinkUtil.OpenBrowser("mailto:awoo.git@gmail.com");
        }
    }
}
