using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyFolderDropdown : IpcMessage
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public LivelyFolderDropdown() : base(MessageType.lp_fdropdown)
        {
        }
    }
}
