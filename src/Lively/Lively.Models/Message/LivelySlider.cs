using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelySlider : IpcMessage
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double Step { get; set; }
        public LivelySlider() : base(MessageType.lp_slider)
        {
        }
    }
}
