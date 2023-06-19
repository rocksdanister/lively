using System;
using Downloader;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Lively.Common.Helpers.Network
{
    public class MultiDownloadHelper : IDownloadHelper
    {
        public event EventHandler<bool> DownloadFileCompleted;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<DownloadEventArgs> DownloadStarted;

        private double previousDownloadedSize = -1;
        private readonly DownloadService downloader;

        public MultiDownloadHelper()
        {
            //CPU can get toasty.. should rate limit to 100MB/s ?
            var downloadOpt = new DownloadConfiguration()
            {
                BufferBlockSize = 8000, // usually, hosts support max to 8000 bytes, default values is 8000
                ChunkCount = 1, // file parts to download, default value is 1
                //MaximumBytesPerSecond = 1024 * 1024 * 1, // download speed limit
                MaxTryAgainOnFailover = 5, // the maximum number of times to fail
                ParallelDownload = false, // download parts of file as parallel or not. Default value is false
                Timeout = 3000, // timeout (millisecond) per stream block reader, default values is 1000
                // clear package chunks data when download completed with failure, default value is false
                ClearPackageOnCompletionWithFailure = false,
                // Before starting the download, reserve the storage space of the file as file size, default value is false
                ReserveStorageSpaceBeforeStartingDownload = false,
            };

            downloader = new DownloadService(downloadOpt);
            downloader.DownloadStarted += Downloader_DownloadStarted;
            // Provide any information about chunker downloads, like progress percentage per chunk, speed, total received bytes and received bytes array to live streaming.
            // downloader.ChunkDownloadProgressChanged += OnChunkDownloadProgressChanged;
            // Provide any information about download progress, like progress percentage of sum of chunks, total speed, average speed, total received bytes and received bytes array to live streaming.
            downloader.DownloadProgressChanged += OnDownloadProgressChanged;
            // Download completed event that can include occurred errors or cancelled or download completed successfully.
            downloader.DownloadFileCompleted += OnDownloadFileCompleted;
        }

        public async Task DownloadFile(Uri url, string filePath)
        {
            await downloader.DownloadFileTaskAsync(url.AbsoluteUri, filePath);
        }

        private void Downloader_DownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            DownloadStarted?.Invoke(this,
                    new DownloadEventArgs()
                    {
                        TotalSize = Math.Truncate(ByteToMegabyte(e.TotalBytesToReceive)),
                        FileName = e.FileName
                    }
                );
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var downloadedSize = Math.Truncate(ByteToMegabyte(e.ReceivedBytesSize));
            if (downloadedSize == previousDownloadedSize)
                return;

            DownloadProgressEventArgs args = new DownloadProgressEventArgs()
            {
                TotalSize = Math.Truncate(ByteToMegabyte(e.TotalBytesToReceive)),
                DownloadedSize = downloadedSize,
                Percentage = e.ProgressPercentage,
            };
            previousDownloadedSize = downloadedSize;

            DownloadProgressChanged?.Invoke(this, args);
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                //user cancelled
            }
            else if (e.Error != null)
            {
                DownloadFileCompleted?.Invoke(this, false);
            }
            else
            {
                DownloadFileCompleted?.Invoke(this, true);
            }
        }

        static double ByteToMegabyte(double bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        public void Cancel()
        {
            downloader?.CancelAsync();
            downloader?.Dispose();
        }
    }
}
