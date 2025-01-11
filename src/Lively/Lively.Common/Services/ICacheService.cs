using System;
using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public interface ICacheService
    {
        Task<string> GetFileFromCacheAsync(Uri uri, bool throwException = false);
        void RemoveExpired();
    }
}