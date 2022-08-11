using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lively.Gallery.Client
{
    public static class HttpRequestMessageExtensions
    {
        public static HttpRequestMessage WithJsonContent(this HttpRequestMessage message, object data)
        {
            message.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            return message;
        }

        public static HttpRequestMessage Clone(this HttpRequestMessage req)
        {
            HttpRequestMessage clone = new HttpRequestMessage(req.Method, req.RequestUri);

            clone.Content = req.Content;
            clone.Version = req.Version;

            foreach (KeyValuePair<string, object> prop in req.Properties)
            {
                clone.Properties.Add(prop);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }


}
