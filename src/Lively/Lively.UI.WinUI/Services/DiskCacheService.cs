using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Lively.Common.Services;

namespace Lively.UI.WinUI.Services
{
    public class DiskCacheService : ICacheService
    {
        private readonly TimeSpan duration = TimeSpan.FromHours(12);
        private readonly HttpClient httpClient;
        private readonly string cacheDir;

        public DiskCacheService(IHttpClientFactory httpClientFactory, string cacheDir)
        {
            this.cacheDir = cacheDir;
            this.httpClient = httpClientFactory.CreateClient();
            InitializeInternal();
        }

        private void InitializeInternal()
        {
            Directory.CreateDirectory(cacheDir);
            InternalRemoveExpired();
        }

        public async Task<string> GetFileFromCacheAsync(Uri uri, bool throwException = false)
        {
            try
            {
                var fileName = GetCacheFileName(uri);
                var filePath = Path.Combine(cacheDir, fileName);
                if (IsFileOutOfDate(filePath, duration))
                {
                    var buffer = await httpClient.GetByteArrayAsync(uri);
                    await File.WriteAllBytesAsync(filePath, buffer);
                }
                return filePath;
            }
            catch 
            {
                if (throwException)
                    throw;

                return null;
            }
        }

        public void RemoveExpired()
        {
            InternalRemoveExpired();
        }

        private void InternalRemoveExpired()
        {
            DirectoryInfo dir = new(cacheDir);
            foreach (FileInfo file in dir.GetFiles())
            {
                if (IsFileOutOfDate(file.FullName, duration))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { }
                }
            }
        }

        private static bool IsFileOutOfDate(string file, TimeSpan duration)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return true;
            }

            return !File.Exists(file) || DateTime.Now.Subtract(File.GetLastAccessTime(file)) > duration;
        }

        //Attribution: https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/main/Microsoft.Toolkit.Uwp.UI/Cache/CacheBase.cs
        //MIT license.
        private static string GetCacheFileName(Uri uri)
        {
            return CreateHash64(uri.ToString()).ToString();
        }

        private static ulong CreateHash64(string str)
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(str);

            ulong value = (ulong)utf8.Length;
            for (int n = 0; n < utf8.Length; n++)
            {
                value += (ulong)utf8[n] << ((n * 5) % 56);
            }

            return value;
        }
    }
}
