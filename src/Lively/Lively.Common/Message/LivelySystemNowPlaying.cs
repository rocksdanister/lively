using Lively.Common.Models;
using System;

namespace Lively.Common.Message
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
