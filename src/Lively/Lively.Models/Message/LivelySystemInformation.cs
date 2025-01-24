using Lively.Models.Services;
using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelySystemInformation : IpcMessage
    {
        public HardwareUsageEventArgs Info { get; set; }
        public LivelySystemInformation() : base(MessageType.cmd_reload)
        {
        }
    }
}
