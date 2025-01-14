using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public interface IDownloadService
    {
        /// <summary>
        /// Downloads a file from the specified URL to the given file path.
        /// </summary>
        /// <param name="url">The file download uri.</param>
        /// <param name="filePath">The destination file path.</param>
        /// <param name="progress">Reports progress. 
        /// The first parameter is the downloaded size in MB, and the second is the total size in MB.</param>
        /// <param name="cancellationToken">A token to cancel the download operation.</param>
        /// <returns>A Task that completes when the download is finished, or throws an exception on failure.</returns>
        Task DownloadFile(Uri url, string filePath, IProgress<(double downloaded, double total)> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads a file from the specified URL to the given file path.
        /// </summary>
        /// <param name="url">The file download uri.</param>
        /// <param name="filePath">The destination file path.</param>
        Task DownloadFile(Uri url, string filePath);
    }
}