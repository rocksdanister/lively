using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyMessageScreenshot : IpcMessage
    {
        public string FileName { get; set; }
        public bool Success { get; set; }
        public LivelyMessageScreenshot() : base(MessageType.msg_screenshot)
        {
        }
    }
}
