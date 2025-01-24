using Newtonsoft.Json;
using System;

namespace Lively.Models.Message
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
