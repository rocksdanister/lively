using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyTextBox : IpcMessage
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public LivelyTextBox() : base(MessageType.lp_textbox)
        {
        }
    }
}
