using Lively.Models.Gallery.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Gallery.Client.Interfaces
{
    public interface ITokenStore
    {
        public TokensModel Get();
        public void Set(string accessToken, string refreshToken, string provider, DateTime expiration);
        public void Clear();
    }
}
