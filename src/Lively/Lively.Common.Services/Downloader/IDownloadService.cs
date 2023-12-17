﻿using System;
using System.Threading.Tasks;

namespace Lively.Common.Services.Downloader
{
    public class DownloadProgressEventArgs : EventArgs
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

    public class DownloadEventArgs : EventArgs
    {
        /// <summary>
        /// Total size of file in megabytes.
        /// </summary>
        public double TotalSize { get; set; }
        /// <summary>
        /// Name of the file.
        /// </summary>
        public string FileName { get; set; }
    }

    public interface IDownloadService
    {
        event EventHandler<bool> DownloadFileCompleted;
        event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        event EventHandler<DownloadEventArgs> DownloadStarted;

        Task DownloadFile(Uri url, string filePath);
        void Cancel();
    }
}