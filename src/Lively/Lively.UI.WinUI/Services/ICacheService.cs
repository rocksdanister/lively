using System;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Services
{
    public interface ICacheService
    {
        Task<string> GetFileFromCacheAsync(Uri uri, bool throwException = false);
        void RemoveExpired();
    }
}