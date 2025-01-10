using System;

namespace Lively.Common.Message
{
    [Serializable]
    public class LivelySuspendCmd : IpcMessage
    {
        public LivelySuspendCmd() : base(MessageType.cmd_suspend)
        {
        }
    }
}
