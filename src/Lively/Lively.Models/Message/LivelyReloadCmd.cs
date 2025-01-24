using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyReloadCmd : IpcMessage
    {
        public LivelyReloadCmd() : base(MessageType.cmd_reload)
        {
        }
    }
}
