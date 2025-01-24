using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyCloseCmd : IpcMessage
    {
        public LivelyCloseCmd() : base(MessageType.cmd_close)
        {
        }
    }
}
