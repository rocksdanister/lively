using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.UI.WinUI.Views.Pages;
using Lively.UI.WinUI.Views.Pages.Gallery;
using Lively.UI.WinUI.Views.Pages.Settings;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace Lively.UI.WinUI.Services
{
    public class Navigator : INavigator
    {
        /// <inheritdoc/>
        public event EventHandler<ContentPageType>? ContentPageChanged;

        public object? RootFrame { get; set; }

        /// <inheritdoc/>
        public object? Frame { get; set; }

        public ContentPageType? CurrentPage { get; private set; } = null;

        public void NavigateTo(ContentPageType contentPage, object navArgs = null)
        {
            if (CurrentPage == contentPage)
                return;

            InternalNavigateTo(contentPage, new DrillInNavigationTransitionInfo(), navArgs);
        }

        public void Reload()
        {
            if (CurrentPage == null)
                return;

            InternalNavigateTo(CurrentPage.Value, new EntranceNavigationTransitionInfo());
        }

        private void InternalNavigateTo(ContentPageType contentPage, NavigationTransitionInfo transition, object navArgs = null)
        {
            Type pageType = contentPage switch
            {
                ContentPageType.library => typeof(LibraryView),
                ContentPageType.gallery => typeof(GalleryView),
                ContentPageType.appupdate => typeof(AppUpdateView),
                ContentPageType.settingsGeneral => typeof(SettingsGeneralView),
                ContentPageType.settingsPerformance => typeof(SettingsPerformanceView),
                ContentPageType.settingsWallpaper => typeof(SettingsWallpaperView),
                ContentPageType.settingsSystem => typeof(SettingsSystemView),
                _ => throw new NotImplementedException(),
            };

            if (Frame is Frame f)
            {
                f.Navigate(pageType, navArgs, transition);

                CurrentPage = contentPage;
                ContentPageChanged?.Invoke(this, contentPage);
            }
        }
    }
}
