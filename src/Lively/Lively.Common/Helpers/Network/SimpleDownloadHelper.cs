using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Common.Helpers.Network
{
    //ref: https://github.com/dotnet/runtime/issues/31479
    public class SimpleDownloadHelper : IDownloadHelper
    {
        public event EventHandler<bool> DownloadFileCompleted;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<DownloadEventArgs> DownloadStarted;

        private readonly IHttpClientFactory httpClientFactory;

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public SimpleDownloadHelper(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task DownloadFile(Uri url, string filePath)
        {
            using var stream = new MemoryStream();
            await DownloadFileAsync(url, stream, cts.Token, (d, t) => DownloadProgressChanged?.Invoke(this,
                new DownloadProgressEventArgs()
                {
                    TotalSize = Math.Truncate(ByteToMegabyte(t)),
                    DownloadedSize = Math.Truncate(ByteToMegabyte(d)),
                    Percentage = (double)d * 100/ t
                }));

            using FileStream fileStream = File.Create(filePath);
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fileStream);

            DownloadFileCompleted?.Invoke(this, true);
        }

        public void Cancel()
        {
            cts?.Cancel();
        }

        /// <summary>
        /// Downloads a file from the specified Uri into the specified stream
        /// </summary>
        /// <param name="cancellationToken">An optional CancellationToken that can be used to cancel the in-progress download.</param>
        /// <param name="progressCallback">If not null, will be called as the download progress. The first parameter will be the number of bytes downloaded so far, and the second the total size of the expected file after download.</param>
        /// <returns>A task that is completed once the download is complete.</returns>
        private async Task DownloadFileAsync(Uri uri, Stream toStream, CancellationToken cancellationToken = default, Action<long, long> progressCallback = null)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            if (toStream == null)
                throw new ArgumentNullException(nameof(toStream));

            using var client = httpClientFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (progressCallback != null)
            {
                long length = response.Content.Headers.ContentLength ?? -1;
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                byte[] buffer = new byte[4096];
                int read;
                int totalRead = 0;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await toStream.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    totalRead += read;
                    progressCallback.Invoke(totalRead, length);
                }
                Debug.Assert(totalRead == length || length == -1);
            }
            else
            {
                await response.Content.CopyToAsync(toStream).ConfigureAwait(false);
            }
        }

        static double ByteToMegabyte(double bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
    }
}
