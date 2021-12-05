using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using mwpfc = ModernWpf.Controls;
using swc = System.Windows.Controls;
using wuxc = Windows.UI.Xaml.Controls;

namespace livelywpf.Helpers.MVVM
{
    /// <summary>
    /// UWP ContentDialog
    /// </summary>
    public static class Dialogs
    {
        public static async Task<wuxc.ContentDialogResult> ShowConfirmationDialog(string title, string message, XamlRoot xamlRoot,
            string primaryBtnText, string secondaryBtnText = null, wuxc.ContentDialogButton defaultBtn = wuxc.ContentDialogButton.Primary)
        {
            var tb = new wuxc.TextBlock { Text = message };
            var dialog = new wuxc.ContentDialog
            {
                Title = title,
                Content = tb,
                PrimaryButtonText = primaryBtnText,
            };

            var result = wuxc.ContentDialogResult.Primary;
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

        public static async Task<wuxc.ContentDialogResult> ShowConfirmationDialog(string title, object body, XamlRoot xamlRoot,
            string primaryBtnText, string secondaryBtnText = null, wuxc.ContentDialogButton defaultBtn = wuxc.ContentDialogButton.Primary)
        {
            var dialog = new wuxc.ContentDialog
            {
                Title = title,
                Content = body,
                PrimaryButtonText = primaryBtnText,
            };

            var result = wuxc.ContentDialogResult.Primary;
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

        public static async Task<mwpfc.ContentDialogResult> ShowConfirmationDialog(string title, object body,
            string primaryBtnText, string secondaryBtnText = null, mwpfc.ContentDialogButton defaultBtn = mwpfc.ContentDialogButton.Primary)
        {
            var dialog = new mwpfc.ContentDialog
            {
                Title = title,
                Content = body,
                PrimaryButtonText = primaryBtnText,
            };

            var result = mwpfc.ContentDialogResult.Primary;
            if (!string.IsNullOrEmpty(secondaryBtnText))
            {
                dialog.SecondaryButtonText = secondaryBtnText;
            }
            dialog.DefaultButton = defaultBtn;

            try
            {
                //If another dialog already open.
                result = await dialog.ShowAsync();
            }
            catch { }
            return result;
        }

        public static async Task<mwpfc.ContentDialogResult> ShowConfirmationDialog(string title, string message,
            string primaryBtnText, string secondaryBtnText = null, mwpfc.ContentDialogButton defaultBtn = mwpfc.ContentDialogButton.Primary)
        {
            var tb = new swc.TextBlock { Text = message };
            var dialog = new mwpfc.ContentDialog
            {
                Title = title,
                Content = tb,
                PrimaryButtonText = primaryBtnText,
            };

            var result = mwpfc.ContentDialogResult.Primary;
            if (!string.IsNullOrEmpty(secondaryBtnText))
            {
                dialog.SecondaryButtonText = secondaryBtnText;
            }
            dialog.DefaultButton = defaultBtn;

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
            string result = null;
            var tb = new wuxc.TextBox();
            var dialog = new wuxc.ContentDialog
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
                var dResult = await dialog.ShowAsync();
                result = tb.Text;
            }
            catch { }
            return result;
        }

        public static async Task<string> ShowTextInputDialog(string title, string primaryBtnText)
        {
            string result = null;
            var tb = new swc.TextBox();
            var dialog = new mwpfc.ContentDialog
            {
                Title = title,
                Content = tb,
                PrimaryButtonText = primaryBtnText,
            };

            try
            {
                //If another dialog already open.
                var dResult = await dialog.ShowAsync();
                result = tb.Text;
            }
            catch { }
            return result;
        }
    }
}
