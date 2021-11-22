using System;
using Downloader;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace livelywpf.Helpers.NetWork
{
    class MultiDownloadHelper : IDownloadHelper
    {
        private readonly DownloadService downloader;
        public event EventHandler<bool> DownloadFileCompleted;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<DownloadEventArgs> DownloadStarted;

        public MultiDownloadHelper()
        {
            //CPU can get toasty.. should rate limit to 100MB/s ?
            var downloadOpt = new DownloadConfiguration()
            {
                BufferBlockSize = 8000, // usually, hosts support max to 8000 bytes, default values is 8000
                ChunkCount = 1, // file parts to download, default value is 1
                //MaximumBytesPerSecond = 1024 * 1024 * 1, // download speed limit
                MaxTryAgainOnFailover = 100, // the maximum number of times to fail
                OnTheFlyDownload = false, // caching in-memory or not? default values is true
                ParallelDownload = false, // download parts of file as parallel or not. Default value is false
                //TempDirectory = "", // Set the temp path for buffering chunk files, the default path is Path.GetTempPath()
                Timeout = 1000, // timeout (millisecond) per stream block reader, default values is 1000
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

        public async void DownloadFile(Uri url, string filePath)
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
            DownloadProgressEventArgs args = new DownloadProgressEventArgs()
            {
                TotalSize = Math.Truncate(ByteToMegabyte(e.TotalBytesToReceive)),
                DownloadedSize = Math.Truncate(ByteToMegabyte(e.ReceivedBytesSize)),
                Percentage = e.ProgressPercentage,
            };
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
