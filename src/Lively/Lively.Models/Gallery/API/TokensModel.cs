using System;

namespace Lively.Models.Gallery.API
{
    public class TokensModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Provider { get; set; }
        public DateTime Expiration { get; set; }
    }
}
