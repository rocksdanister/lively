using System;

namespace Lively.Common.Message
{
    [Serializable]
    public class LivelyResumeCmd : IpcMessage
    {
        public LivelyResumeCmd() : base(MessageType.cmd_resume)
        {
        }
    }
}
