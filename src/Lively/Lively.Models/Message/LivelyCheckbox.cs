using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyCheckbox : IpcMessage
    {
        public string Name { get; set; }
        public bool Value { get; set; }
        public LivelyCheckbox() : base(MessageType.lp_chekbox)
        {
        }
    }
}
