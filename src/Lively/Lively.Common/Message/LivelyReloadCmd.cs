using System;

namespace Lively.Common.Message
{
    [Serializable]
    public class LivelyReloadCmd : IpcMessage
    {
        public LivelyReloadCmd() : base(MessageType.cmd_reload)
        {
        }
    }
}
