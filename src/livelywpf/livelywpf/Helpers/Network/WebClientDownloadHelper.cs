using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.ServiceModel.Description;
using System.Text;
using System.Windows;
using Windows.Web.UI.Interop;

namespace livelywpf.Helpers.NetWork
{
    [Obsolete("Cannot cancel in-progress download: https://github.com/dotnet/runtime/issues/31479")]
    class WebClientDownloadHelper : IDownloadHelper
    {
        private WebClient webClient;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<bool> DownloadFileCompleted;
        public event EventHandler<DownloadEventArgs> DownloadStarted;
        private bool _initialized = false;
        private string fileName;

        public void DownloadFile(Uri url, string filePath)
        {
            webClient = new WebClient();
            webClient.DownloadProgressChanged += Client_DownloadProgressChanged;
            webClient.DownloadFileCompleted += Client_DownloadFileCompleted;
            webClient.DownloadFileAsync(url, filePath);
            fileName = System.IO.Path.GetFileName(filePath);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if(!_initialized)
            {
                _initialized = true;
                DownloadStarted?.Invoke(this, 
                    new DownloadEventArgs() { 
                        TotalSize = Math.Truncate(ByteToMegabyte(e.TotalBytesToReceive)), 
                        FileName = fileName
                    }
                );
            }

            DownloadProgressEventArgs args = new DownloadProgressEventArgs()
            {
                TotalSize = Math.Truncate(ByteToMegabyte(e.TotalBytesToReceive)),
                DownloadedSize = Math.Truncate(ByteToMegabyte(e.BytesReceived)),
                Percentage = e.ProgressPercentage,
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
                //user cancelled
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
