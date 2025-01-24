using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyScreenshotCmd : IpcMessage
    {
        public ScreenshotFormat Format { get; set; }
        public string FilePath { get; set; }
        public uint Delay { get; set; }
        public LivelyScreenshotCmd() : base(MessageType.cmd_screenshot)
        {
        }
    }
}
