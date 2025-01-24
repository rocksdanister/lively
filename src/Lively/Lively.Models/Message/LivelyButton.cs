using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyButton : IpcMessage
    {
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public LivelyButton() : base(MessageType.lp_button)
        {
        }
    }
}
