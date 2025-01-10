using Lively.Common.Models;
using System;

namespace Lively.Common.Message
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
