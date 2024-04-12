using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Extensions
{
    // Ref: https://github.com/WinUICommunity/WinUICommunity/blob/main/dev/Core/Extensions/ContentDialogEx.cs
    // MIT License
    // Copyright(c) 2023 Mahdi Hosseini
    public static class ContentDialogExtensions
    {
        private static TaskCompletionSource<ContentDialog> _contentDialogShowRequest;

        public static async Task<ContentDialogResult> ShowAsyncQueue(this ContentDialog dialog)
        {
            while (_contentDialogShowRequest != null)
            {
                await _contentDialogShowRequest.Task;
            }

            var request = _contentDialogShowRequest = new TaskCompletionSource<ContentDialog>();
            var result = await dialog.ShowAsync();
            _contentDialogShowRequest = null;
            request.SetResult(dialog);

            return result;
        }

        public static async Task<ContentDialogResult> ShowAsyncDraggable(this ContentDialog dialog)
        {
            dialog.ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
            {
                if (!e.IsInertial)
                    dialog.Margin = new Thickness(
                        dialog.Margin.Left + e.Delta.Translation.X,
                        dialog.Margin.Top + e.Delta.Translation.Y,
                        dialog.Margin.Left - e.Delta.Translation.X,
                        dialog.Margin.Top - e.Delta.Translation.Y);
            };
            var result = await dialog.ShowAsync();
            return result;
        }

        public static async Task<ContentDialogResult> ShowAsyncQueueDraggable(this ContentDialog dialog)
        {
            while (_contentDialogShowRequest != null)
            {
                await _contentDialogShowRequest.Task;
            }
            dialog.ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
            {
                if (!e.IsInertial)
                    dialog.Margin = new Thickness(
                        dialog.Margin.Left + e.Delta.Translation.X,
                        dialog.Margin.Top + e.Delta.Translation.Y,
                        dialog.Margin.Left - e.Delta.Translation.X,
                        dialog.Margin.Top - e.Delta.Translation.Y);
            };
            var request = _contentDialogShowRequest = new TaskCompletionSource<ContentDialog>();
            var result = await dialog.ShowAsync();
            _contentDialogShowRequest = null;
            request.SetResult(dialog);

            return result;
        }
    }
}
