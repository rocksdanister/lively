using System;

namespace Lively.Common.Message
{
    [Serializable]
    public class LivelyCloseCmd : IpcMessage
    {
        public LivelyCloseCmd() : base(MessageType.cmd_close)
        {
        }
    }
}
