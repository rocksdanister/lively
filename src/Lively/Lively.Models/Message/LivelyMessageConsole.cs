using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyMessageConsole : IpcMessage
    {
        public string Message { get; set; }
        public ConsoleMessageType Category { get; set; }
        public LivelyMessageConsole() : base(MessageType.msg_console)
        {
        }
    }
}
