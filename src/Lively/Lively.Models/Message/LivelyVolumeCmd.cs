using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyVolumeCmd : IpcMessage
    {
        public int Volume { get; set; }
        public LivelyVolumeCmd() : base(MessageType.cmd_volume)
        {
        }
    }
}
