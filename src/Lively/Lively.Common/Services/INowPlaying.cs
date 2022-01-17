using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services
{
    public interface INowPlaying
    {
        event EventHandler<NowPlayingEventArgs> NowPlayingTrackChanged;
    }

    public class NowPlayingEventArgs : EventArgs
    {
        /// <summary>
        /// Song title.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Song artist.
        /// </summary>
        public string Artist { get; set; }
    }
}
