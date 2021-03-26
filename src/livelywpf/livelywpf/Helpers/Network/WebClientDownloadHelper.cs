using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.ServiceModel.Description;
using System.Text;
using System.Windows;
using Windows.Web.UI.Interop;

namespace livelywpf
{
    [Obsolete("Cannot cancel in-progress download: https://github.com/dotnet/runtime/issues/31479")]
    class WebClientDownloadHelper : IDownloadHelper
    {
        private WebClient webClient;
        public event EventHandler<DownloadEventArgs> DownloadProgressChanged;
        public event EventHandler<bool> DownloadFileCompleted;

        public void DownloadFile(Uri url, string filePath)
        {
            webClient = new WebClient();
            webClient.DownloadProgressChanged += Client_DownloadProgressChanged;
            webClient.DownloadFileCompleted += Client_DownloadFileCompleted;
            webClient.DownloadFileAsync(url, filePath);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = e.BytesReceived;
            double totalBytes = e.TotalBytesToReceive;
            double percentage = bytesIn / totalBytes * 100;

            DownloadEventArgs args = new DownloadEventArgs()
            {
                TotalSize = Math.Truncate(ByteToMegabyte(totalBytes)),
                DownloadedSize = Math.Truncate(ByteToMegabyte(bytesIn)),
                Percentage = Math.Truncate(percentage),
            };
            DownloadProgressChanged?.Invoke(this, args);
        }

        void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                DownloadFileCompleted?.Invoke(this, true);
            }
            else if (e.Cancelled)
            {
                webClient.Dispose();
            }
            else
            {
                DownloadFileCompleted?.Invoke(this, false);
            }
        }

        static double ByteToMegabyte(double bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        public void Cancel()
        {
            if (webClient != null)
            {
                try
                {
                    webClient.DownloadFileCompleted -= Client_DownloadFileCompleted;
                    webClient.DownloadProgressChanged -= Client_DownloadProgressChanged;
                    //Does not Work!: https://github.com/dotnet/runtime/issues/31479
                    webClient.CancelAsync();
                    webClient.Dispose();
                }
                catch { }
            }
        }
    }
}
