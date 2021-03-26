using System;

namespace livelywpf
{
    public class DownloadEventArgs : EventArgs
    {
        /// <summary>
        /// Total size of file in megabytes.
        /// </summary>
        public double TotalSize { get; set; }
        /// <summary>
        /// Currently downloaded file size in megabytes.
        /// </summary>
        public double DownloadedSize { get; set; }
        /// <summary>
        /// Download progress.
        /// </summary>
        public double Percentage { get; set; }
    }

    interface IDownloadHelper
    {
        event EventHandler<bool> DownloadFileCompleted;
        event EventHandler<DownloadEventArgs> DownloadProgressChanged;

        void DownloadFile(Uri url, string filePath);
        void Cancel();
    }
}