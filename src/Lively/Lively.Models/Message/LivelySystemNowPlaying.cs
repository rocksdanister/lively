using Lively.Models.Services;
using System;

namespace Lively.Models.Message
{
    [Serializable]
    public class LivelySystemNowPlaying : IpcMessage
    {
        public NowPlayingEventArgs Info { get; set; }
        public LivelySystemNowPlaying() : base(MessageType.lsp_nowplaying)
        {
        }
    }
}
