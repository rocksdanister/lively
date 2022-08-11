using Lively.Gallery.Client.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Lively.Common.Constants;
using Lively.Common.Helpers.Storage;
using Lively.Models.Gallery.API;
using Lively.Common.Helpers;
using System.Diagnostics;

namespace Lively.Gallery.Client
{
    public class JsonTokenStore : ITokenStore
    {
        private TokensModel _tokens;

        public void Clear()
        {
            _tokens = new();
            try
            {
                EncryptUtil.Store(_tokens, CommonPaths.TokensPath);
            }
            catch { }
        }

        public TokensModel Get()
        {
            if (_tokens == null)
            {
                try
                {
                    _tokens = EncryptUtil.Load<TokensModel>(CommonPaths.TokensPath);
                    //Debug.WriteLine($"Accesstoken:{_tokens?.AccessToken}");
                }
                catch { }
            }
            return _tokens;
        }

        public void Set(string accessToken, string refreshToken, string provider, DateTime expiration)
        {
            _tokens = new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = expiration,
                Provider = provider
            };
     
            try
            {
                EncryptUtil.Store(_tokens, CommonPaths.TokensPath);
            }
            catch { }
        }
    }
}
