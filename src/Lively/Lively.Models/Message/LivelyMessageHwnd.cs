using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyMessageHwnd : IpcMessage
    {
        public long Hwnd { get; set; }
        public LivelyMessageHwnd() : base(MessageType.msg_hwnd)
        {
        }
    }
}
