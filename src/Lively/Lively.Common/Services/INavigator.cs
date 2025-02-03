using Lively.Models.Enums;
using System;

namespace Lively.Common.Services
{
    public interface INavigator
    {
        /// <summary>
        /// Raised when the content page was changed.
        /// </summary>
        event EventHandler<ContentPageType>? ContentPageChanged;

        /// <summary>
        /// The root frame of the app.
        /// </summary>
        object? RootFrame { get; set; }

        /// <summary>
        /// The inner frame that can navigate. This must be set before
        /// any method is called.
        /// </summary>
        object? Frame { get; set; }

        /// <summary>
        /// Returns the current page type.
        /// </summary>
        ContentPageType? CurrentPage { get; }

        /// <summary>
        /// Navigates to the page corresponding to the given enum.
        /// </summary>
        /// <param name="contentPage">The page to navigate to.</param>
        /// <param name="navArgs">Arguments to be passed to the new page during navigation.</param>
        void NavigateTo(ContentPageType contentPage, object? navArgs = null);

        /// <summary>
        /// Reload the current page.
        /// </summary>
        void Reload();
    }
}
