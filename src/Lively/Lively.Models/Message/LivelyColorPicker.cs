using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyColorPicker : IpcMessage
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public LivelyColorPicker() : base(MessageType.lp_cpicker)
        {
        }
    }
}
