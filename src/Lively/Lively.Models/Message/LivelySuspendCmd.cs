using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelySuspendCmd : IpcMessage
    {
        public LivelySuspendCmd() : base(MessageType.cmd_suspend)
        {
        }
    }
}
