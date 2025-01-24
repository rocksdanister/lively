using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyDropdown : IpcMessage
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public LivelyDropdown() : base(MessageType.lp_dropdown)
        {
        }
    }
}
