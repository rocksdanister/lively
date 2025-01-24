using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelyMessageWallpaperLoaded : IpcMessage
    {
        public bool Success { get; set; }
        public LivelyMessageWallpaperLoaded() : base(MessageType.msg_wploaded)
        {
        }
    }
}
