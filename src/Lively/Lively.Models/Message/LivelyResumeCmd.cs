using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyResumeCmd : IpcMessage
    {
        public LivelyResumeCmd() : base(MessageType.cmd_resume)
        {
        }
    }
}
