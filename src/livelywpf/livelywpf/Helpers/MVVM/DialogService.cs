using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace livelywpf.Helpers
{
    /// <summary>
    /// UWP ContentDialog
    /// </summary>
    public static class DialogService
    {
        //todo: Find a way to avoid passing XamlRoot.
        public static async Task<ContentDialogResult> ShowConfirmationDialog(string title, string message,
            string primaryBtnText, string secondaryBtnText = null, ContentDialogButton defaultBtn = ContentDialogButton.Primary)
        {
            var tb = new TextBlock{ Text = message };
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

            try
            {
                //If another dialog already open.
                result = await dialog.ShowAsync();
            }
            catch { }
            return result;
        }

        public static async Task<ContentDialogResult> ShowConfirmationDialog(string title, object body,
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

            try
            {
                //If another dialog already open.
                result = await dialog.ShowAsync();
            }
            catch { }
            return result;
        }


        public static async Task<string> ShowTextInputDialog(string title, string primaryBtnText)
        {
            var tb = new TextBox();
            string result = null;
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = tb,
                PrimaryButtonText = primaryBtnText,
            };

            try
            {
                //If another dialog already open.
                ContentDialogResult dResult = await dialog.ShowAsync();
                result = tb.Text;
            }
            catch { }
            return result;
        }
    }
}
