using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Models;
using Lively.Grpc.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

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
