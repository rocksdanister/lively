using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Message
{

    [Serializable]
    public abstract class IpcMessage
    {
        [JsonProperty(Order = -2)]
        public MessageType Type { get; }
        public IpcMessage(MessageType type)
        {
            Type = type;
        }
    }
}
