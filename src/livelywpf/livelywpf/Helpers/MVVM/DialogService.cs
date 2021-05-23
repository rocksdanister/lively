using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace livelywpf.Helpers
{
    /// <summary>
    /// UWP ContentDialog
    /// </summary>
    public static class DialogService
    {
        //todo: Find a way to avoid passing XamlRoot.
        public static async Task<ContentDialogResult> ShowConfirmationDialog(string title, string message, XamlRoot xamlRoot,
            string primaryBtnText, string secondaryBtnText = null, ContentDialogButton defaultBtn = ContentDialogButton.Primary)
        {
            var tb = new Windows.UI.Xaml.Controls.TextBlock{ Text = message };
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = tb,
                PrimaryButtonText = primaryBtnText,
            };

            ContentDialogResult result = ContentDialogResult.Primary;
            if (!string.IsNullOrEmpty(secondaryBtnText))
            {
                dialog.SecondaryButtonText = secondaryBtnText;
            }
            dialog.DefaultButton = defaultBtn;

            // Use this code to associate the dialog to the appropriate AppWindow by setting
            // the dialog's XamlRoot to the same XamlRoot as an element that is already present in the AppWindow.
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                dialog.XamlRoot = xamlRoot;
            }

            try
            {
                //If another dialog already open.
                result = await dialog.ShowAsync();
            }
            catch { }
            return result;
        }

        public static async Task<string> ShowTextInputDialog(string title, XamlRoot xamlRoot, string primaryBtnText)
        {
            var tb = new Windows.UI.Xaml.Controls.TextBox();
            string result = null;
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = tb,
                PrimaryButtonText = primaryBtnText,
            };

            // Use this code to associate the dialog to the appropriate AppWindow by setting
            // the dialog's XamlRoot to the same XamlRoot as an element that is already present in the AppWindow.
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                dialog.XamlRoot = xamlRoot;
            }

            try
            {
                //If another dialog already open.
                ContentDialogResult dResult = await dialog.ShowAsync();
                result = tb.Text;
            }
            catch { }
            return result;
        }

        public static async Task<ContentDialogResult> ShowConfirmationDialog(string title, object body, XamlRoot xamlRoot,
         string primaryBtnText, string secondaryBtnText = null, ContentDialogButton defaultBtn = ContentDialogButton.Primary)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = body,
                PrimaryButtonText = primaryBtnText,
            };

            ContentDialogResult result = ContentDialogResult.Primary;
            if (!string.IsNullOrEmpty(secondaryBtnText))
            {
                dialog.SecondaryButtonText = secondaryBtnText;
            }
            dialog.DefaultButton = defaultBtn;

            // Use this code to associate the dialog to the appropriate AppWindow by setting
            // the dialog's XamlRoot to the same XamlRoot as an element that is already present in the AppWindow.
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                dialog.XamlRoot = xamlRoot;
            }

            try
            {
                //If another dialog already open.
                result = await dialog.ShowAsync();
            }
            catch { }
            return result;
        }
    }
}
